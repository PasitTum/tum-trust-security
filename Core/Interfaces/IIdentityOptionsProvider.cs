using Netka.Identity.Core.Models;

namespace Netka.Identity.Core.Interfaces;

/// <summary>
/// Provides <see cref="IdentityVerificationOptions"/> dynamically at runtime (e.g., from a Database, Distributed Cache, or Tenant Context).
/// </summary>
public interface IIdentityOptionsProvider
{
    /// <summary>
    /// Retrieves the current <see cref="IdentityVerificationOptions"/> for the active request or scope.
    /// </summary>
    IdentityVerificationOptions GetOptions();
}
