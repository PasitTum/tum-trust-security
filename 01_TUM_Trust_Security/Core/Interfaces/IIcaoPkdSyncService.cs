using System.Threading;
using System.Threading.Tasks;
using Tum.Trust.Security.Core.Models.IcaoPkd;

namespace Tum.Trust.Security.Core.Interfaces;

/// <summary>
/// Service contract for synchronizing official ICAO Public Key Directory (PKD) MasterLists,
/// validating Country Signing CA (CSCA) certificates, and distributing updates to air-gapped kiosks
/// via the TUM Sovereign KeyMesh protocol.
/// </summary>
public interface IIcaoPkdSyncService
{
    /// <summary>
    /// Synchronizes and updates the local sovereign trust anchor keystore from an official ICAO PKD MasterList release.
    /// </summary>
    /// <param name="masterListLdifBytes">Optional raw LDIF or PKCS#7 MasterList bytes (e.g. from local update bundle).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PkdSyncResult> SynchronizeAsync(byte[]? masterListLdifBytes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves metadata for the currently active CSCA MasterList cached in local memory.
    /// </summary>
    Task<CscaMasterListInfo> GetActiveMasterListInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// TUM Sovereign KeyMesh: Ingests an authenticated peer-to-peer cryptographic gossip payload
    /// allowing isolated/air-gapped Kiosks to update their sovereign keys without external Internet access.
    /// </summary>
    Task<PkdSyncResult> IngestSovereignKeyMeshPayloadAsync(string gossipPayloadToken, CancellationToken cancellationToken = default);
}
