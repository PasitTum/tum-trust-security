using Netka.Identity.Core.Interfaces;
using Netka.Identity.Core.Models;
using Netka.Identity.Infrastructure.Providers;
using Netka.Identity.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;

namespace Netka.Identity.Application;

/// <summary>
/// Dependency Injection registration extensions for the Identity Verification SDK.
/// Supports <see cref="IConfiguration"/> (appsettings.json), in-memory <see cref="IdentityVerificationOptions"/>,
/// and dynamic Database-backed configuration via <see cref="IServiceProvider"/> or <see cref="IIdentityOptionsProvider"/>.
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

        return services.AddIdentityVerification((sp, _) =>
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
    }

    private static void ConfigureProviderHttpClient<TProvider>(
        IServiceCollection services,
        bool isThaId,
        Func<HttpClient, IServiceProvider, TProvider> factory)
        where TProvider : class
    {
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
            .AddPolicyHandler(CreateRetryPolicy())
            .AddPolicyHandler(CreateCircuitBreakerPolicy());
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy()
    {
        return Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(response => (int)response.StatusCode >= 500 || (int)response.StatusCode == 408)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy()
    {
        return Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(response => (int)response.StatusCode >= 500)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
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
