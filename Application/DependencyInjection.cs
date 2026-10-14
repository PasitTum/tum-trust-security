using Tum.Trust.Security.Core.Interfaces;
using Tum.Trust.Security.Core.Models;
using Tum.Trust.Security.Infrastructure.Providers;
using Tum.Trust.Security.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tum.Trust.Security.Application;

/// <summary>
/// Dependency Injection registration extensions for the Identity Verification SDK.
/// Supports <see cref="IConfiguration"/> (appsettings.json), in-memory <see cref="IdentityVerificationOptions"/>,
/// and dynamic Database-backed configuration via <see cref="IServiceProvider"/> or <see cref="IIdentityOptionsProvider"/>.
/// Compiled as a zero-third-party-dependency DLL so host projects can reference <c>Tum.Trust.Security.dll</c> directly.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers core identity verification services, AES-256 encryption, and verification providers
    /// (<see cref="IThaIdProvider"/> &amp; <see cref="IDopaProvider"/>) using <see cref="IConfiguration"/>.
    /// </summary>
    public static IServiceCollection AddIdentityVerification(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        services.TryAddSingleton<IConfiguration>(configuration);

        return services.AddIdentityVerification((_, _) =>
        {
            // Options are automatically populated from IConfiguration in DelegateIdentityOptionsProvider
        });
    }

    /// <summary>
    /// Registers core identity verification services using a static or code-based <see cref="IdentityVerificationOptions"/> configuration action.
    /// </summary>
    public static IServiceCollection AddIdentityVerification(
        this IServiceCollection services,
        Action<IdentityVerificationOptions> configure)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configure is null) throw new ArgumentNullException(nameof(configure));

        return services.AddIdentityVerification((_, options) => configure(options));
    }

    /// <summary>
    /// Registers core identity verification services using a dynamic configuration action that can resolve
    /// scoped or singleton services (such as EF Core <c>DbContext</c> or a Database Settings Repository) from <see cref="IServiceProvider"/>.
    /// If <see cref="IConfiguration"/> is registered in DI, <see cref="IdentityVerificationOptions"/> is pre-populated from <c>appsettings.json</c>
    /// before invoking <paramref name="configure"/>, allowing seamless hybrid (JSON + Database override) configuration.
    /// </summary>
    public static IServiceCollection AddIdentityVerification(
        this IServiceCollection services,
        Action<IServiceProvider, IdentityVerificationOptions> configure)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configure is null) throw new ArgumentNullException(nameof(configure));

        services.AddSingleton<IIdentityOptionsProvider>(sp => new DelegateIdentityOptionsProvider(sp, configure));
        RegisterCoreServicesAndProviders(services);
        return services;
    }

    /// <summary>
    /// Registers core identity verification services using a custom <see cref="IIdentityOptionsProvider"/> implementation
    /// (for example, a class that loads settings from a Database or Distributed Cache).
    /// </summary>
    public static IServiceCollection AddIdentityVerification<TOptionsProvider>(
        this IServiceCollection services)
        where TOptionsProvider : class, IIdentityOptionsProvider
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        services.AddScoped<TOptionsProvider>();
        services.AddSingleton<IIdentityOptionsProvider>(sp => new ScopedIdentityOptionsProviderAdapter<TOptionsProvider>(sp));
        RegisterCoreServicesAndProviders(services);
        return services;
    }

    private static void RegisterCoreServicesAndProviders(IServiceCollection services)
    {
        // 1. Register AES-256 Encryption Service (resolves EncryptionKey lazily/dynamically from IIdentityOptionsProvider)
        services.AddSingleton<IEncryptionService>(sp =>
            new AesEncryptionService(
                sp.GetRequiredService<IIdentityOptionsProvider>(),
                sp.GetService<ILogger<AesEncryptionService>>()));

        // 2. Register ThaID Provider (IThaIdProvider + IVerificationProvider)
        ConfigureProviderHttpClient<ThaIdProvider>(
            services,
            isThaId: true,
            factory: (httpClient, sp) => new ThaIdProvider(
                sp.GetRequiredService<IIdentityOptionsProvider>(),
                httpClient,
                sp.GetRequiredService<IEncryptionService>(),
                sp.GetService<ILogger<ThaIdProvider>>() ?? NullLogger<ThaIdProvider>.Instance));

        services.AddTransient<IThaIdProvider>(sp => sp.GetRequiredService<ThaIdProvider>());
        services.AddTransient<IVerificationProvider>(sp => sp.GetRequiredService<ThaIdProvider>());

        // 3. Register DOPA Provider (IDopaProvider + IVerificationProvider)
        ConfigureProviderHttpClient<DopaProvider>(
            services,
            isThaId: false,
            factory: (httpClient, sp) => new DopaProvider(
                sp.GetRequiredService<IIdentityOptionsProvider>(),
                httpClient,
                sp.GetRequiredService<IEncryptionService>(),
                sp.GetService<ILogger<DopaProvider>>() ?? NullLogger<DopaProvider>.Instance));

        services.AddTransient<IDopaProvider>(sp => sp.GetRequiredService<DopaProvider>());
        services.AddTransient<IVerificationProvider>(sp => sp.GetRequiredService<DopaProvider>());

        // 4. Register TUM TRUST SECURITY Trust Provider (IBorderlessTrustProvider + IVerificationProvider)
        services.TryAddSingleton<IFaceLivenessDetector, Tum.Trust.Security.Infrastructure.Biometrics.EdgeFaceLivenessDetector>();
        services.TryAddSingleton<IBiometricFaceMatcher, Tum.Trust.Security.Infrastructure.Biometrics.EdgeBiometricFaceMatcher>();
        services.TryAddSingleton<ISovereignTrustAnchorService, Tum.Trust.Security.Infrastructure.Passport.SovereignTrustAnchorService>();
        services.TryAddSingleton<IBorderlessTrustProvider, Tum.Trust.Security.Infrastructure.Passport.MockBorderlessTrustProvider>();
    }

    private static void ConfigureProviderHttpClient<TProvider>(
        IServiceCollection services,
        bool isThaId,
        Func<HttpClient, IServiceProvider, TProvider> factory)
        where TProvider : class
    {
        var circuitState = new CircuitBreakerState(allowedFailures: 5, breakDuration: TimeSpan.FromSeconds(30));

        services.AddHttpClient<TProvider>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddTypedClient(factory)
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var handler = new HttpClientHandler();
                var optionsProvider = sp.GetService<IIdentityOptionsProvider>();
                var options = optionsProvider?.GetOptions();
                bool allowUntrustedCerts = isThaId
                    ? options?.ThaId?.AllowUntrustedCertificates == true
                    : options?.Dopa?.AllowUntrustedCertificates == true;

                if (allowUntrustedCerts)
                {
                    handler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }

                return handler;
            })
            .AddHttpMessageHandler(() => new IdentityResilienceHandler(maxRetries: 3, circuitState));
    }

    private sealed class CircuitBreakerState
    {
        private readonly object _lock = new();
        private readonly int _allowedFailures;
        private readonly TimeSpan _breakDuration;
        private int _consecutiveFailures;
        private DateTimeOffset _blockedUntil = DateTimeOffset.MinValue;

        public CircuitBreakerState(int allowedFailures, TimeSpan breakDuration)
        {
            _allowedFailures = allowedFailures;
            _breakDuration = breakDuration;
        }

        public void EnsureClosed()
        {
            lock (_lock)
            {
                if (DateTimeOffset.UtcNow < _blockedUntil)
                {
                    throw new HttpRequestException("Circuit breaker is open due to consecutive upstream provider failures.");
                }
            }
        }

        public void RecordSuccess()
        {
            lock (_lock)
            {
                _consecutiveFailures = 0;
                _blockedUntil = DateTimeOffset.MinValue;
            }
        }

        public void RecordFailure()
        {
            lock (_lock)
            {
                _consecutiveFailures++;
                if (_consecutiveFailures >= _allowedFailures)
                {
                    _blockedUntil = DateTimeOffset.UtcNow.Add(_breakDuration);
                }
            }
        }
    }

    private sealed class IdentityResilienceHandler : DelegatingHandler
    {
        private readonly int _maxRetries;
        private readonly CircuitBreakerState _circuitState;

        public IdentityResilienceHandler(int maxRetries, CircuitBreakerState circuitState)
        {
            _maxRetries = maxRetries;
            _circuitState = circuitState;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            for (int attempt = 0; ; attempt++)
            {
                _circuitState.EnsureClosed();

                try
                {
                    var response = await base.SendAsync(request, cancellationToken);
                    int statusCode = (int)response.StatusCode;

                    if (statusCode >= 500 || statusCode == 408)
                    {
                        _circuitState.RecordFailure();
                        if (attempt < _maxRetries)
                        {
                            response.Dispose();
                            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt + 1)), cancellationToken);
                            continue;
                        }
                    }
                    else
                    {
                        _circuitState.RecordSuccess();
                    }

                    return response;
                }
                catch (HttpRequestException)
                {
                    _circuitState.RecordFailure();
                    if (attempt >= _maxRetries)
                    {
                        throw;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt + 1)), cancellationToken);
                }
            }
        }
    }

    private sealed class DelegateIdentityOptionsProvider : IIdentityOptionsProvider
    {
        private readonly IServiceProvider _rootServiceProvider;
        private readonly Action<IServiceProvider, IdentityVerificationOptions> _configure;

        public DelegateIdentityOptionsProvider(
            IServiceProvider rootServiceProvider,
            Action<IServiceProvider, IdentityVerificationOptions> configure)
        {
            _rootServiceProvider = rootServiceProvider;
            _configure = configure;
        }

        public IdentityVerificationOptions GetOptions()
        {
            using var scope = _rootServiceProvider.CreateScope();
            var scopedSp = scope.ServiceProvider;

            var config = scopedSp.GetService<IConfiguration>();
            var options = config is not null
                ? IdentityVerificationOptions.FromConfiguration(config)
                : new IdentityVerificationOptions();

            _configure(scopedSp, options);
            return options;
        }
    }

    private sealed class ScopedIdentityOptionsProviderAdapter<TOptionsProvider> : IIdentityOptionsProvider
        where TOptionsProvider : class, IIdentityOptionsProvider
    {
        private readonly IServiceProvider _rootServiceProvider;

        public ScopedIdentityOptionsProviderAdapter(IServiceProvider rootServiceProvider)
        {
            _rootServiceProvider = rootServiceProvider;
        }

        public IdentityVerificationOptions GetOptions()
        {
            using var scope = _rootServiceProvider.CreateScope();
            var provider = scope.ServiceProvider.GetRequiredService<TOptionsProvider>();
            return provider.GetOptions();
        }
    }
}
