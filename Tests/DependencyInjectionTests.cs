using System.Security.Cryptography;
using Netka.Identity.Application;
using Netka.Identity.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Netka.Identity.SDK.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddIdentityVerification_RegistersAllServicesAndProviderInterfaces()
    {
        // Arrange
        byte[] keyBytes = new byte[32];
        RandomNumberGenerator.Fill(keyBytes);
        var settings = new Dictionary<string, string?>
        {
            ["IdentityVerification:EncryptionKey"] = Convert.ToBase64String(keyBytes),
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
}
