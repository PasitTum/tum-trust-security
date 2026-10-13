using Microsoft.Extensions.Configuration;

namespace Netka.Identity.Core.Models;

/// <summary>
/// Root configuration options for Netka Identity Verification SDK.
/// Supports loading from <see cref="IConfiguration"/> (appsettings.json) or dynamically from a Database / custom service.
/// </summary>
public class IdentityVerificationOptions
{
    /// <summary>
    /// Base64-encoded 32-byte (256-bit) key used by <see cref="Interfaces.IEncryptionService"/> for AES-256-CBC + HMAC-SHA256 PII encryption.
    /// </summary>
    public string? EncryptionKey { get; set; }

    /// <summary>
    /// ThaID (BORA Digital ID) OAuth 2.0 / OIDC configuration options.
    /// </summary>
    public ThaIdOptions ThaId { get; set; } = new();

    /// <summary>
    /// DOPA Citizen Card (Laser ID &amp; Smart Card Chip) verification configuration options.
    /// </summary>
    public DopaOptions Dopa { get; set; } = new();

    /// <summary>
    /// Binds an <see cref="IdentityVerificationOptions"/> instance from an <see cref="IConfiguration"/> source.
    /// </summary>
    public static IdentityVerificationOptions FromConfiguration(IConfiguration configuration)
    {
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        var thaIdSection = configuration.GetSection("IdentityVerification:ThaId");
        var dopaSection = configuration.GetSection("IdentityVerification:Dopa");

        return new IdentityVerificationOptions
        {
            EncryptionKey = configuration["IdentityVerification:EncryptionKey"],
            ThaId = ThaIdOptions.FromSection(thaIdSection),
            Dopa = DopaOptions.FromSection(dopaSection)
        };
    }
}

/// <summary>
/// Configuration options for <see cref="Interfaces.IThaIdProvider"/>.
/// </summary>
public class ThaIdOptions
{
    private string? _authorizeUrl;

    /// <summary>
    /// OAuth 2.0 Client ID issued by DOPA BORA (ThaID).
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// OAuth 2.0 Client Secret issued by DOPA BORA (ThaID).
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// ThaID OAuth 2.0 Authorization Endpoint URL (defaults to <c>https://imauth.bora.dopa.go.th/api/v2/oauth2/auth/</c>).
    /// </summary>
    public string? AuthorizeUrl
    {
        get => _authorizeUrl;
        set => _authorizeUrl = value;
    }

    /// <summary>
    /// Alias for <see cref="AuthorizeUrl"/> to support both <c>AuthorizeUrl</c> and <c>AuthorizationUrl</c> key names in DB or JSON configs.
    /// </summary>
    public string? AuthorizationUrl
    {
        get => _authorizeUrl;
        set => _authorizeUrl = value;
    }

    /// <summary>
    /// ThaID OAuth 2.0 Token Endpoint URL (defaults to <c>https://imauth.bora.dopa.go.th/api/v2/oauth2/token/</c>).
    /// </summary>
    public string? TokenUrl { get; set; }

    /// <summary>
    /// ThaID OAuth 2.0 Token Introspection Endpoint URL (defaults to <c>https://imauth.bora.dopa.go.th/api/v2/oauth2/introspect/</c>).
    /// </summary>
    public string? InspectUrl { get; set; }

    /// <summary>
    /// ThaID OAuth 2.0 Token Revocation Endpoint URL (defaults to <c>https://imauth.bora.dopa.go.th/api/v2/oauth2/revoke/</c>).
    /// </summary>
    public string? RevokeUrl { get; set; }

    /// <summary>
    /// ThaID OIDC UserInfo Endpoint URL (defaults to <c>https://imauth.bora.dopa.go.th/api/v2/oauth2/userinfo/</c>).
    /// </summary>
    public string? UserInfoUrl { get; set; }

    /// <summary>
    /// Default OAuth 2.0 Callback Redirect URI when not explicitly passed per call.
    /// </summary>
    public string? RedirectUri { get; set; }

    /// <summary>
    /// Space-delimited OAuth 2.0 scopes (defaults to <c>openid pid profile</c>).
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Whether to allow untrusted SSL/TLS certificates (for internal sandbox/testing environments only).
    /// </summary>
    public bool AllowUntrustedCertificates { get; set; }

    /// <summary>
    /// Reads <see cref="ThaIdOptions"/> from an <see cref="IConfigurationSection"/>.
    /// </summary>
    public static ThaIdOptions FromSection(IConfigurationSection section)
    {
        if (section is null) throw new ArgumentNullException(nameof(section));

        return new ThaIdOptions
        {
            ClientId = section["ClientId"],
            ClientSecret = section["ClientSecret"],
            AuthorizeUrl = section["AuthorizeUrl"] ?? section["AuthorizationUrl"],
            TokenUrl = section["TokenUrl"],
            InspectUrl = section["InspectUrl"],
            RevokeUrl = section["RevokeUrl"],
            UserInfoUrl = section["UserInfoUrl"],
            RedirectUri = section["RedirectUri"],
            Scope = section["Scope"],
            AllowUntrustedCertificates = bool.TryParse(section["AllowUntrustedCertificates"], out var allow) && allow
        };
    }
}

/// <summary>
/// Configuration options for <see cref="Interfaces.IDopaProvider"/>.
/// </summary>
public class DopaOptions
{
    /// <summary>
    /// Default DOPA verification URL fallback when <see cref="LaserUrl"/> or <see cref="ChipUrl"/> is not specified.
    /// </summary>
    public string? VerificationUrl { get; set; }

    /// <summary>
    /// DOPA CheckCardByLaser endpoint URL.
    /// </summary>
    public string? LaserUrl { get; set; }

    /// <summary>
    /// DOPA CheckCardByCID (Smart Card Chip / BP1) endpoint URL.
    /// </summary>
    public string? ChipUrl { get; set; }

    /// <summary>
    /// Optional Bearer API Key / Token for authenticating with the DOPA Gateway.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Whether to allow untrusted SSL/TLS certificates (for internal VPN/gateway environments).
    /// </summary>
    public bool AllowUntrustedCertificates { get; set; }

    /// <summary>
    /// Reads <see cref="DopaOptions"/> from an <see cref="IConfigurationSection"/>.
    /// </summary>
    public static DopaOptions FromSection(IConfigurationSection section)
    {
        if (section is null) throw new ArgumentNullException(nameof(section));

        return new DopaOptions
        {
            VerificationUrl = section["VerificationUrl"],
            LaserUrl = section["LaserUrl"] ?? section["LaserVerificationUrl"],
            ChipUrl = section["ChipUrl"] ?? section["ChipVerificationUrl"],
            ApiKey = section["ApiKey"],
            AllowUntrustedCertificates = bool.TryParse(section["AllowUntrustedCertificates"], out var allow) && allow
        };
    }
}
