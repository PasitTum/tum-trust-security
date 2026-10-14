using System;
using System.Text;
using System.Threading.Tasks;
using Tum.Trust.Security.Infrastructure.Passport;
using Xunit;

namespace Tum.Trust.Security.Tests;

/// <summary>
/// Unit and integration tests for ICAO PKD MasterList Sync and TUM Sovereign KeyMesh gossip protocol.
/// </summary>
public class IcaoPkdSyncTests
{
    private readonly IcaoPkdSyncService _service = new();

    [Fact]
    public async Task SynchronizeAsync_DefaultMasterList_ReturnsSuccessWith150PlusNations()
    {
        var result = await _service.SynchronizeAsync();

        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.True(result.SyncedCountriesCount >= 150);
        Assert.False(result.AirGappedMeshSynchronized);
        Assert.Contains("ICAO-PKD-ML", result.MasterListVersion);
    }

    [Fact]
    public async Task SynchronizeAsync_CustomLdifBytes_UpdatesVersionAndCalculatesHash()
    {
        byte[] customLdif = Encoding.UTF8.GetBytes("CUSTOM_ICAO_PKD_LDIF_RELEASE_DATA_STREAM");

        var result = await _service.SynchronizeAsync(customLdif);

        Assert.True(result.IsSuccess);
        var activeInfo = await _service.GetActiveMasterListInfoAsync();

        Assert.NotNull(activeInfo);
        Assert.NotEmpty(activeInfo.HashSha256);
        Assert.Contains("CUSTOM", activeInfo.Version);
    }

    [Fact]
    public async Task IngestSovereignKeyMeshPayloadAsync_ValidToken_SuccessfullyUpdatesAirGappedKiosk()
    {
        // Valid authenticated KeyMesh gossip token
        string validToken = "tum_mesh_eyJpc3MiOiJ0dW1fa2V5bWVzaF9hdXRob3JpdHkiLCJzZXNzaW9uIjoiYWlyX2dhcHBlZCJ9";

        var result = await _service.IngestSovereignKeyMeshPayloadAsync(validToken);

        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.True(result.AirGappedMeshSynchronized);
        Assert.Equal("ICAO-PKD-KEYMESH-GOSSIP-VERIFIED", result.MasterListVersion);
    }

    [Fact]
    public async Task IngestSovereignKeyMeshPayloadAsync_TamperedToken_FailsVerification()
    {
        string invalidToken = "invalid_unauthenticated_token_without_prefix";

        var result = await _service.IngestSovereignKeyMeshPayloadAsync(invalidToken);

        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.False(result.AirGappedMeshSynchronized);
    }
}
