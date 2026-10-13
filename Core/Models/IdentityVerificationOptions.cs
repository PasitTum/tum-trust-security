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
    /// Enables in-memory mock mode for both ThaID and DOPA providers without making external HTTP calls.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool UseMock
    {
        get => ThaId.UseMock && Dopa.UseMock;
        set
        {
            ThaId.UseMock = value;
            Dopa.UseMock = value;
        }
    }

    /// <summary>
    /// Optional mock scenario for ThaID loopback redirect when <see cref="UseMock"/> is <c>true</c>:
    /// <c>"success"</c> (default), <c>"deny"</c>, <c>"invalid_state"</c>, <c>"expired_code"</c>, or <c>"timeout"</c>.
    /// </summary>
    public string MockScenario
    {
        get => ThaId.MockScenario ?? "success";
        set => ThaId.MockScenario = value;
    }

    /// <summary>
    /// Binds an <see cref="IdentityVerificationOptions"/> instance from an <see cref="IConfiguration"/> source.
    /// </summary>
    public static IdentityVerificationOptions FromConfiguration(IConfiguration configuration)
    {
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        var rootUseMock = bool.TryParse(configuration["IdentityVerification:UseMock"], out var globalMock) && globalMock;
        var rootMockScenario = configuration["IdentityVerification:MockScenario"];
        var thaIdSection = configuration.GetSection("IdentityVerification:ThaId");
        var dopaSection = configuration.GetSection("IdentityVerification:Dopa");

        var thaIdOpts = ThaIdOptions.FromSection(thaIdSection);
        var dopaOpts = DopaOptions.FromSection(dopaSection);

        if (rootUseMock)
        {
            if (thaIdSection["UseMock"] is null) thaIdOpts.UseMock = true;
            if (dopaSection["UseMock"] is null) dopaOpts.UseMock = true;
        }

        if (!string.IsNullOrWhiteSpace(rootMockScenario) && thaIdSection["MockScenario"] is null)
        {
            thaIdOpts.MockScenario = rootMockScenario!;
        }

        return new IdentityVerificationOptions
        {
            EncryptionKey = configuration["IdentityVerification:EncryptionKey"],
            ThaId = thaIdOpts,
            Dopa = dopaOpts
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
    /// Enables built-in mock mode (loopback redirect + in-memory 20-scope citizen data without external HTTP requests).
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool UseMock { get; set; }

    /// <summary>
    /// Optional mock redirect scenario when <see cref="UseMock"/> is <c>true</c>:
    /// <c>"success"</c> (default), <c>"deny"</c> (<c>error=access_denied</c>), <c>"invalid_state"</c>, <c>"expired_code"</c>, or <c>"timeout"</c>.
    /// </summary>
    public string? MockScenario { get; set; }

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
    /// Expected OIDC Issuer URI (defaults to <c>https://imauth.bora.dopa.go.th</c>).
    /// </summary>
    public string? Issuer { get; set; }

    /// <summary>
    /// ThaID OpenID Connect Discovery URL (defaults to <c>https://imauth.bora.dopa.go.th/.well-known/openid-configuration</c>).
    /// </summary>
    public string? DiscoveryUrl { get; set; }

    /// <summary>
    /// ThaID JSON Web Key Set (JWKS) URL for verifying ES256/RS256 <c>id_token</c> signatures (defaults to <c>https://imauth.bora.dopa.go.th/jwks/</c>).
    /// </summary>
    public string? JwksUrl { get; set; }

    /// <summary>
    /// Optional API Gateway key sent with ThaID HTTP requests when behind an API gateway.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// HTTP header name used when <see cref="ApiKey"/> is specified (defaults to <c>x-api-key</c>).
    /// </summary>
    public string? ApiKeyHeader { get; set; } = "x-api-key";

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
            UseMock = bool.TryParse(section["UseMock"], out var useMock) && useMock,
            MockScenario = section["MockScenario"],
            ClientId = section["ClientId"],
            ClientSecret = section["ClientSecret"],
            AuthorizeUrl = section["AuthorizeUrl"] ?? section["AuthorizationUrl"],
            TokenUrl = section["TokenUrl"],
            InspectUrl = section["InspectUrl"],
            RevokeUrl = section["RevokeUrl"],
            UserInfoUrl = section["UserInfoUrl"],
            RedirectUri = section["RedirectUri"],
            Scope = section["Scope"],
            Issuer = section["Issuer"],
            DiscoveryUrl = section["DiscoveryUrl"],
            JwksUrl = section["JwksUrl"],
            ApiKey = section["ApiKey"],
            ApiKeyHeader = !string.IsNullOrWhiteSpace(section["ApiKeyHeader"]) ? section["ApiKeyHeader"] : "x-api-key",
            AllowUntrustedCertificates = bool.TryParse(section["AllowUntrustedCertificates"], out var allow) && allow
        };
    }
}

/// <summary>
/// Gateway mode for DOPA verification: <see cref="EnterpriseGateway"/> (POST JSON to internal gateway / VPN)
/// or <see cref="DgaGdx"/> (GET queries to DGA Government Data Exchange api.egov.go.th using Consumer-Key and Token).
/// </summary>
public enum DopaGatewayMode
{
    /// <summary>
    /// Direct Server-to-Server POST JSON to enterprise internal gateway or VPN endpoint (default).
    /// </summary>
    EnterpriseGateway = 0,

    /// <summary>
    /// Official DGA GDX (Government Data Exchange) REST API (GET https://api.egov.go.th/ws/dopa/verification/personal)
    /// with two-step validation (Consumer-Key + ConsumerSecret -> 60-min Token).
    /// </summary>
    DgaGdx = 1
}

/// <summary>
/// Configuration options for <see cref="Interfaces.IDopaProvider"/>.
/// </summary>
public class DopaOptions
{
    private string? _laserUrl;
    private string? _chipUrl;
    private string? _faceUrl;

    /// <summary>
    /// Enables built-in mock mode (in-memory DOPA Laser, Chip, Face &amp; Token verification without external HTTP requests).
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool UseMock { get; set; }

    /// <summary>
    /// Gateway mode for DOPA verification: <see cref="DopaGatewayMode.EnterpriseGateway"/> (default) or <see cref="DopaGatewayMode.DgaGdx"/>.
    /// Auto-detected as <see cref="DopaGatewayMode.DgaGdx"/> if <see cref="ConsumerKey"/> or <see cref="ConsumerSecret"/> is specified and Mode is not explicitly set.
    /// </summary>
    public DopaGatewayMode Mode { get; set; } = DopaGatewayMode.EnterpriseGateway;

    /// <summary>
    /// DGA GDX Consumer-Key issued by DGA (สพร.) via email upon service registration.
    /// Used for DGA GDX Government API validation (<c>/ws/auth/validate</c>).
    /// </summary>
    public string? ConsumerKey { get; set; }

    /// <summary>
    /// DGA GDX ConsumerSecret issued by DGA (สพร.) upon service registration.
    /// Used for DGA GDX Government API validation (<c>/ws/auth/validate</c>).
    /// </summary>
    public string? ConsumerSecret { get; set; }

    /// <summary>
    /// DGA GDX AgentID: 13-digit Citizen ID of the officer or English system identifier registered with DGA for audit logging.
    /// </summary>
    public string? AgentId { get; set; }

    /// <summary>
    /// DGA GDX Base URL or validation URL. Defaults to <c>https://api.egov.go.th</c> (or <c>https://trial.dga.or.th</c> for testing).
    /// </summary>
    public string? DgaGdxBaseUrl { get; set; } = "https://api.egov.go.th";

    /// <summary>
    /// Default DOPA verification URL fallback when <see cref="LaserUrl"/>, <see cref="ChipUrl"/>, or <see cref="FaceUrl"/> is not specified.
    /// </summary>
    public string? VerificationUrl { get; set; }

    /// <summary>
    /// DOPA CheckCardByLaser endpoint URL (Enterprise Gateway) or DGA GDX verification URL.
    /// Defaults to <c>https://api.egov.go.th/ws/dopa/verification/personal</c> when in <see cref="DopaGatewayMode.DgaGdx"/> mode.
    /// </summary>
    public string? LaserUrl
    {
        get => _laserUrl;
        set => _laserUrl = value;
    }

    /// <summary>
    /// Alias for <see cref="LaserUrl"/> to support <c>LaserVerificationUrl</c> in JSON/DB configuration.
    /// </summary>
    public string? LaserVerificationUrl
    {
        get => _laserUrl;
        set => _laserUrl = value;
    }

    /// <summary>
    /// DOPA CheckCardByCID (Smart Card Chip / BP1) endpoint URL.
    /// </summary>
    public string? ChipUrl
    {
        get => _chipUrl;
        set => _chipUrl = value;
    }

    /// <summary>
    /// Alias for <see cref="ChipUrl"/> to support <c>ChipVerificationUrl</c> in JSON/DB configuration.
    /// </summary>
    public string? ChipVerificationUrl
    {
        get => _chipUrl;
        set => _chipUrl = value;
    }

    /// <summary>
    /// DOPA Face Verification API endpoint URL (1:1 Biometric Face Matching against citizen ID).
    /// </summary>
    public string? FaceUrl
    {
        get => _faceUrl;
        set => _faceUrl = value;
    }

    /// <summary>
    /// Alias for <see cref="FaceUrl"/> to support <c>FaceVerificationUrl</c> in JSON/DB configuration.
    /// </summary>
    public string? FaceVerificationUrl
    {
        get => _faceUrl;
        set => _faceUrl = value;
    }

    /// <summary>
    /// Minimum similarity score threshold (0.0 to 1.0) for <see cref="Interfaces.IDopaProvider.VerifyByFaceAsync"/> when DOPA response does not include an explicit boolean <c>match</c> field. Defaults to <c>0.80f</c> (80%).
    /// </summary>
    public float FaceThreshold { get; set; } = 0.80f;

    /// <summary>
    /// Optional OAuth 2.0 Token Endpoint URL (<c>grant_type=client_credentials</c>) for obtaining a dynamic Bearer token from the DOPA Gateway.
    /// </summary>
    public string? TokenUrl { get; set; }

    /// <summary>
    /// Optional OAuth 2.0 Client ID used with <see cref="TokenUrl"/> for <c>client_credentials</c> authentication.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Optional OAuth 2.0 Client Secret used with <see cref="TokenUrl"/> for <c>client_credentials</c> authentication.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Optional static Bearer API Key / Token for authenticating with the DOPA Gateway.
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

        var threshold = 0.80f;
        if (float.TryParse(section["FaceThreshold"] ?? section["Threshold"], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsedThreshold) &&
            parsedThreshold > 0f && parsedThreshold <= 1.0f)
        {
            threshold = parsedThreshold;
        }

        var modeStr = section["Mode"] ?? section["GatewayMode"];
        var mode = DopaGatewayMode.EnterpriseGateway;
        if (!string.IsNullOrWhiteSpace(modeStr))
        {
            if (string.Equals(modeStr, "DgaGdx", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(modeStr, "GDX", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(modeStr, "DGA", StringComparison.OrdinalIgnoreCase))
            {
                mode = DopaGatewayMode.DgaGdx;
            }
        }
        else if (!string.IsNullOrWhiteSpace(section["ConsumerKey"]) || !string.IsNullOrWhiteSpace(section["Consumer-Key"]))
        {
            mode = DopaGatewayMode.DgaGdx;
        }

        return new DopaOptions
        {
            UseMock = bool.TryParse(section["UseMock"], out var useMock) && useMock,
            Mode = mode,
            ConsumerKey = section["ConsumerKey"] ?? section["Consumer-Key"],
            ConsumerSecret = section["ConsumerSecret"],
            AgentId = section["AgentId"] ?? section["AgentID"],
            DgaGdxBaseUrl = section["DgaGdxBaseUrl"] ?? "https://api.egov.go.th",
            VerificationUrl = section["VerificationUrl"],
            LaserUrl = section["LaserUrl"] ?? section["LaserVerificationUrl"],
            ChipUrl = section["ChipUrl"] ?? section["ChipVerificationUrl"],
            FaceUrl = section["FaceUrl"] ?? section["FaceVerificationUrl"] ?? section["FaceApiUrl"],
            FaceThreshold = threshold,
            TokenUrl = section["TokenUrl"],
            ClientId = section["ClientId"],
            ClientSecret = section["ClientSecret"],
            ApiKey = section["ApiKey"],
            AllowUntrustedCertificates = bool.TryParse(section["AllowUntrustedCertificates"], out var allow) && allow
        };
    }
}
