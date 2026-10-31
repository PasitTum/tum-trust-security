namespace Tum.Trust.Security.Core.Models.IcaoPkd;

/// <summary>
/// Outcome of an automated ICAO PKD MasterList synchronization and verification cycle.
/// Incorporates TUM Sovereign KeyMesh telemetry for air-gapped terminal operations.
/// </summary>
public class PkdSyncResult
{
    /// <summary>
    /// Indicates whether the MasterList was cryptographically validated and cached.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Number of sovereign nations whose root certificates are actively synchronized.
    /// </summary>
    public int SyncedCountriesCount { get; set; }

    /// <summary>
    /// Number of new or rotated certificates ingested into the local secure keystore.
    /// </summary>
    public int UpdatedCertificatesCount { get; set; }

    /// <summary>
    /// Cryptographic MasterList version identifier.
    /// </summary>
    public string MasterListVersion { get; set; } = string.Empty;

    /// <summary>
    /// Execution duration in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// TUM Sovereign KeyMesh: Indicates whether this update was propagated to an air-gapped terminal
    /// via peer-to-peer zero-trust authenticated gossip payload.
    /// </summary>
    public bool AirGappedMeshSynchronized { get; set; }

    /// <summary>
    /// Status or diagnostic message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
