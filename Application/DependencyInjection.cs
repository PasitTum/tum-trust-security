using Netka.Identity.Core.Interfaces;
using Netka.Identity.Infrastructure.Providers;
using Netka.Identity.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;

namespace Netka.Identity.Application;

/// <summary>
/// Dependency Injection registration extensions for the Identity Verification SDK.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers core identity verification services, AES-256 encryption, and verification providers (<see cref="IThaIdProvider"/> &amp; <see cref="IDopaProvider"/>).
    /// </summary>
    public static IServiceCollection AddIdentityVerification(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        services.AddSingleton<IConfiguration>(configuration);

        var encryptionKey = configuration["IdentityVerification:EncryptionKey"];
        if (string.IsNullOrWhiteSpace(encryptionKey))
        {
            throw new InvalidOperationException("IdentityVerification:EncryptionKey missing");
        }

        // 1. Register AES-256 Encryption Service as Singleton
        services.AddSingleton<IEncryptionService>(sp =>
            new AesEncryptionService(
                encryptionKey,
                sp.GetService<ILogger<AesEncryptionService>>()));

        // 2. Register ThaID Provider (IThaIdProvider + IVerificationProvider)
        ConfigureProviderHttpClient<ThaIdProvider>(
            services,
            configuration,
            "IdentityVerification:ThaId:AllowUntrustedCertificates");
        services.AddTransient<IThaIdProvider>(sp => sp.GetRequiredService<ThaIdProvider>());
        services.AddTransient<IVerificationProvider>(sp => sp.GetRequiredService<ThaIdProvider>());

        // 3. Register DOPA Provider (IDopaProvider + IVerificationProvider)
        ConfigureProviderHttpClient<DopaProvider>(
            services,
            configuration,
            "IdentityVerification:Dopa:AllowUntrustedCertificates");
        services.AddTransient<IDopaProvider>(sp => sp.GetRequiredService<DopaProvider>());
        services.AddTransient<IVerificationProvider>(sp => sp.GetRequiredService<DopaProvider>());

        return services;
    }

    private static void ConfigureProviderHttpClient<TProvider>(
        IServiceCollection services,
        IConfiguration configuration,
        string allowUntrustedCertConfigKey)
        where TProvider : class
    {
        var allowUntrustedCerts = bool.TryParse(configuration[allowUntrustedCertConfigKey], out var allow) && allow;

        services.AddHttpClient<TProvider>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();
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
}
