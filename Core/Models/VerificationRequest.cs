namespace Netka.Identity.Core.Models;

/// <summary>
/// Hybrid model representing an identity verification request.
/// Combines standard OAuth 2.0 / OIDC properties with a dynamic dictionary for provider-specific parameters.
/// </summary>
public class VerificationRequest
{
    /// <summary>
    /// The target identity provider name (e.g., "ThaID", "Google", "Microsoft").
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// The authorization code returned by the provider during callback.
    /// </summary>
    public string? AuthorizationCode { get; set; }

    /// <summary>
    /// The callback redirect URI used when requesting authorization.
    /// </summary>
    public string? RedirectUri { get; set; }

    /// <summary>
    /// The OAuth 2.0 state parameter returned by the provider for CSRF validation.
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// The OIDC nonce parameter for replay protection.
    /// </summary>
    public string? Nonce { get; set; }

    /// <summary>
    /// Dynamic fallback dictionary for unique provider properties (e.g., "code_verifier", "expected_state").
    /// Uses <see cref="StringComparer.OrdinalIgnoreCase"/> by default.
    /// </summary>
    public IDictionary<string, object> AdditionalParameters { get; set; } =
        new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
}
