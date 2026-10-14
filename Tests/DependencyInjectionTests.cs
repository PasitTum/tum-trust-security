using System.Security.Cryptography;
using Tum.Trust.Security.Application;
using Tum.Trust.Security.Core.Interfaces;
using Tum.Trust.Security.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tum.Trust.Security.Tests;

public class DependencyInjectionTests
{
    private static string GenerateBase64Key()
    {
        byte[] keyBytes = new byte[32];
        RandomNumberGenerator.Fill(keyBytes);
        return Convert.ToBase64String(keyBytes);
    }

    [Fact]
    public void AddIdentityVerification_RegistersAllServicesAndProviderInterfaces()
    {
        // Arrange
        var settings = new Dictionary<string, string?>
        {
            ["IdentityVerification:EncryptionKey"] = GenerateBase64Key(),
            ["IdentityVerification:ThaId:ClientId"] = "client-id",
            ["IdentityVerification:ThaId:ClientSecret"] = "client-secret",
            ["IdentityVerification:ThaId:RedirectUri"] = "https://kiosk.local/callback",
            ["IdentityVerification:Dopa:LaserUrl"] = "https://dopa.local/CheckCardByLaser",
            ["IdentityVerification:Dopa:ChipUrl"] = "https://dopa.local/CheckCardByCID"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddIdentityVerification(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        var encryptionService = provider.GetRequiredService<IEncryptionService>();
        var thaIdProvider = provider.GetRequiredService<IThaIdProvider>();
        var dopaProvider = provider.GetRequiredService<IDopaProvider>();
        var allProviders = provider.GetServices<IVerificationProvider>().ToList();

        Assert.NotNull(encryptionService);
        Assert.Equal("ThaID", thaIdProvider.ProviderName);
        Assert.Equal("DOPA", dopaProvider.ProviderName);
        Assert.Equal(2, allProviders.Count);
    }

    [Fact]
    public void AddIdentityVerification_WithDatabaseScopedService_SupportsStartupWithoutConfigAndHotReloadsDbChangesAtRuntime()
    {
        // Arrange: Simulate a Database table + Scoped EF Core / Repository service
        var simulatedDbTable = new SimulatedIdentityDbRecord();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(simulatedDbTable);
        services.AddScoped<ScopedDbConfigRepository>();

        // Act 1: Register SDK before Database has any rows seeded (must NOT throw at startup!)
        services.AddIdentityVerification((sp, options) =>
        {
            var repo = sp.GetRequiredService<ScopedDbConfigRepository>();
            var dbRecord = repo.GetActiveConfig();

            options.EncryptionKey = dbRecord.EncryptionKey;
            options.ThaId.ClientId = dbRecord.ThaIdClientId;
            options.ThaId.ClientSecret = dbRecord.ThaIdClientSecret;
            options.ThaId.AuthorizeUrl = dbRecord.ThaIdAuthorizeUrl;
            options.ThaId.RedirectUri = dbRecord.ThaIdRedirectUri;
            options.Dopa.LaserUrl = dbRecord.DopaLaserUrl;
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        var thaIdProvider = provider.GetRequiredService<IThaIdProvider>();
        var encryptionService = provider.GetRequiredService<IEncryptionService>();

        // Act 2: Seed DB with v1 config and use ThaIdProvider + EncryptionService
        string keyV1 = GenerateBase64Key();
        simulatedDbTable.EncryptionKey = keyV1;
        simulatedDbTable.ThaIdClientId = "db-client-v1";
        simulatedDbTable.ThaIdClientSecret = "db-secret-v1";
        simulatedDbTable.ThaIdAuthorizeUrl = "https://db-v1.bora.local/oauth2/auth/";
        simulatedDbTable.ThaIdRedirectUri = "https://kiosk.local/cb1";

        string urlV1 = thaIdProvider.GetAuthorizationUrl("", "state-1");
        string cipherV1 = encryptionService.Encrypt("1234567890121");

        Assert.StartsWith("https://db-v1.bora.local/oauth2/auth/?", urlV1);
        Assert.Contains("client_id=db-client-v1", urlV1);
        Assert.Contains("redirect_uri=https%3A%2F%2Fkiosk.local%2Fcb1", urlV1);
        Assert.Equal("1234567890121", encryptionService.Decrypt(cipherV1));

        // Act 3: Admin updates config in Database at runtime (Hot Reload without restarting app or re-resolving provider!)
        string keyV2 = GenerateBase64Key();
        simulatedDbTable.EncryptionKey = keyV2;
        simulatedDbTable.ThaIdClientId = "db-client-v2";
        simulatedDbTable.ThaIdAuthorizeUrl = "https://db-v2.bora.local/oauth2/auth/";
        simulatedDbTable.ThaIdRedirectUri = "https://kiosk.local/cb2";

        string urlV2 = thaIdProvider.GetAuthorizationUrl("", "state-2");
        string cipherV2 = encryptionService.Encrypt("1234567890121");

        Assert.StartsWith("https://db-v2.bora.local/oauth2/auth/?", urlV2);
        Assert.Contains("client_id=db-client-v2", urlV2);
        Assert.Contains("redirect_uri=https%3A%2F%2Fkiosk.local%2Fcb2", urlV2);
        Assert.Equal("1234567890121", encryptionService.Decrypt(cipherV2));
    }

    [Fact]
    public async Task AddIdentityVerification_WhenOnlyThaIdConfiguredInDatabase_ResolvingAllProvidersDoesNotCrashAndDopaReturnsMissingConfiguration()
    {
        // Arrange: Database only has ThaID configured; DOPA is left unconfigured
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityVerification(options =>
        {
            options.ThaId.ClientId = "only-thaid-client";
            options.ThaId.ClientSecret = "only-thaid-secret";
            // Dopa.LaserUrl & EncryptionKey intentionally left null/empty
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        // Act: Resolve all IVerificationProvider instances (both ThaID and DOPA)
        var allProviders = provider.GetServices<IVerificationProvider>().ToList();
        var thaId = provider.GetRequiredService<IThaIdProvider>();
        var dopa = provider.GetRequiredService<IDopaProvider>();

        // Assert: Resolving does not throw, ThaID works, and calling unconfigured DOPA returns MISSING_CONFIGURATION cleanly
        Assert.Equal(2, allProviders.Count);
        string authUrl = thaId.GetAuthorizationUrl("https://kiosk.local/callback", "state-ok");
        Assert.Contains("client_id=only-thaid-client", authUrl);

        var dopaResult = await dopa.VerifyByLaserAsync(
            pid: "1234567890121",
            firstName: "สมชาย",
            lastName: "ใจดี",
            birthDate: "25300101",
            laserId: "ME0123456789");

        Assert.False(dopaResult.IsSuccess);
        Assert.Equal("MISSING_CONFIGURATION", dopaResult.ErrorCode);
    }

    [Fact]
    public void AddIdentityVerification_WithCustomOptionsProviderClass_ResolvesOptionsFromCustomClass()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityVerification<CustomDatabaseOptionsProvider>();

        using ServiceProvider provider = services.BuildServiceProvider();
        var thaId = provider.GetRequiredService<IThaIdProvider>();

        // Act
        string authUrl = thaId.GetAuthorizationUrl("https://kiosk.local/callback", "state-custom");

        // Assert
        Assert.StartsWith("https://custom-db.local/oauth2/auth/?", authUrl);
        Assert.Contains("client_id=custom-provider-client-id", authUrl);
    }

    private sealed class SimulatedIdentityDbRecord
    {
        public string? EncryptionKey { get; set; }
        public string? ThaIdClientId { get; set; }
        public string? ThaIdClientSecret { get; set; }
        public string? ThaIdAuthorizeUrl { get; set; }
        public string? ThaIdRedirectUri { get; set; }
        public string? DopaLaserUrl { get; set; }
    }

    private sealed class ScopedDbConfigRepository
    {
        private readonly SimulatedIdentityDbRecord _dbRecord;

        public ScopedDbConfigRepository(SimulatedIdentityDbRecord dbRecord)
        {
            _dbRecord = dbRecord;
        }

        public SimulatedIdentityDbRecord GetActiveConfig() => _dbRecord;
    }

    private sealed class CustomDatabaseOptionsProvider : IIdentityOptionsProvider
    {
        public IdentityVerificationOptions GetOptions()
        {
            return new IdentityVerificationOptions
            {
                ThaId = new ThaIdOptions
                {
                    ClientId = "custom-provider-client-id",
                    ClientSecret = "custom-provider-secret",
                    AuthorizeUrl = "https://custom-db.local/oauth2/auth/"
                }
            };
        }
    }
}
