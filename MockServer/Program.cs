using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Netka.Identity.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")) &&
    !args.Any(a => a.StartsWith("--urls", StringComparison.OrdinalIgnoreCase)))
{
    builder.WebHost.UseUrls("http://localhost:5199");
}

var app = builder.Build();

// In-memory store for PKCE code_challenge per authorization code and revoked tokens
var codeChallenges = new ConcurrentDictionary<string, string?>(StringComparer.Ordinal);
var revokedTokens = new ConcurrentDictionary<string, bool>(StringComparer.Ordinal);

string[] officialThaIdScopes =
[
    "openid", "pid", "address", "gender", "birthdate",
    "given_name", "middle_name", "family_name", "name",
    "given_name_en", "middle_name_en", "family_name_en", "name_en",
    "title", "title_en", "ial", "smartcard_code",
    "date_of_expiry", "date_of_issuance", "house_address"
];

// ============================================================================
// 0. Sandbox Status & Documentation Page (GET /)
// ============================================================================
app.MapGet("/", (HttpContext ctx) =>
{
    string baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
    string html = $$"""
    <!DOCTYPE html>
    <html lang="th">
    <head>
        <meta charset="utf-8" />
        <title>Netka Identity Sandbox Mock Server (ThaID &amp; DOPA)</title>
        <style>
            body { font-family: 'Segoe UI', Tahoma, sans-serif; max-width: 960px; margin: 32px auto; padding: 0 20px; color: #1e293b; background: #f8fafc; }
            h1 { color: #0f172a; border-bottom: 2px solid #cbd5e1; padding-bottom: 10px; }
            .card { background: #ffffff; border: 1px solid #e2e8f0; border-radius: 8px; padding: 20px; margin-bottom: 20px; box-shadow: 0 1px 2px rgba(0,0,0,0.05); }
            code, pre { background: #0f172a; color: #f8fafc; padding: 2px 6px; border-radius: 4px; font-family: Consolas, monospace; font-size: 13px; }
            pre { padding: 14px; overflow-x: auto; }
            table { width: 100%; border-collapse: collapse; margin-top: 10px; font-size: 14px; }
            th, td { border: 1px solid #e2e8f0; padding: 8px 10px; text-align: left; }
            th { background: #f1f5f9; }
            .badge { display: inline-block; padding: 2px 8px; border-radius: 999px; font-size: 12px; font-weight: 600; background: #dcfce7; color: #166534; }
        </style>
    </head>
    <body>
        <h1>Netka Identity Sandbox Mock Server <span class="badge">RUNNING</span></h1>
        <div class="card">
            <h3>การตั้งค่าในโปรเจกต์ปลายทาง (appsettings.Development.json)</h3>
            <p>ชี้ URL มายัง Mock Server นี้โดยไม่ต้องแก้ไขโค้ดใดๆ ในโปรเจกต์ปลายทาง:</p>
            <pre>{
      "IdentityVerification": {
        "EncryptionKey": "GENERATE_YOUR_32_BYTE_BASE64_KEY_HERE",
        "ThaID": {
          "ClientId": "mock-client-id",
          "ClientSecret": "mock-client-secret",
          "AuthorizeUrl": "{{baseUrl}}/api/v2/oauth2/auth/",
          "TokenUrl": "{{baseUrl}}/api/v2/oauth2/token/",
          "InspectUrl": "{{baseUrl}}/api/v2/oauth2/introspect/",
          "RevokeUrl": "{{baseUrl}}/api/v2/oauth2/revoke/",
          "UserInfoUrl": "{{baseUrl}}/api/v2/oauth2/userinfo/",
          "Scope": "openid pid title given_name family_name name birthdate address house_address smartcard_code"
        },
        "Dopa": {
          "LaserUrl": "{{baseUrl}}/CheckCardByLaser",
          "ChipUrl": "{{baseUrl}}/CheckCardByCID",
          "ApiKey": "mock-dopa-api-key"
        }
      }
    }</pre>
        </div>
        <div class="card">
            <h3>Endpoints ที่เปิดให้บริการ</h3>
            <table>
                <tr><th>Method</th><th>Endpoint</th><th>คำอธิบาย</th></tr>
                <tr><td>GET</td><td><code>/api/v2/oauth2/auth/</code></td><td>จำลองหน้า ThaID QR / Consent และทำการ Redirect (<code>HTTP 302 Found</code>) กลับไปยัง <code>redirect_uri</code></td></tr>
                <tr><td>POST</td><td><code>/api/v2/oauth2/token/</code></td><td>ตรวจสอบ PKCE (<code>S256</code>) และคืน Token พร้อมข้อมูลประชาชนครบ 20 Scopes</td></tr>
                <tr><td>POST</td><td><code>/api/v2/oauth2/introspect/</code></td><td>ตรวจสอบสถานะ Token (<code>active: true/false</code>)</td></tr>
                <tr><td>POST</td><td><code>/api/v2/oauth2/revoke/</code></td><td>ยกเลิก Token (<code>{"message": "revoked"}</code>)</td></tr>
                <tr><td>GET</td><td><code>/api/v2/oauth2/userinfo/</code></td><td>คืนข้อมูลประชาชน 20 Scopes จาก Bearer Token</td></tr>
                <tr><td>POST</td><td><code>/CheckCardByLaser</code></td><td>จำลองการตรวจสอบสถานะบัตรประชาชนด้วยเลขหลังบัตร (DOPA Laser ID)</td></tr>
                <tr><td>POST</td><td><code>/CheckCardByCID</code></td><td>จำลองการตรวจสอบสถานะบัตรประชาชนด้วยเลขชิป (DOPA Chip / BP1)</td></tr>
            </table>
        </div>
    </body>
    </html>
    """;
    return Results.Content(html, "text/html; charset=utf-8");
});

// ============================================================================
// 1. OpenID Discovery (/.well-known/openid-configuration)
// ============================================================================
app.MapGet("/.well-known/openid-configuration", (HttpContext ctx) =>
{
    string baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
    return Results.Json(new
    {
        issuer = baseUrl,
        authorization_endpoint = $"{baseUrl}/api/v2/oauth2/auth/",
        token_endpoint = $"{baseUrl}/api/v2/oauth2/token/",
        userinfo_endpoint = $"{baseUrl}/api/v2/oauth2/userinfo/",
        introspection_endpoint = $"{baseUrl}/api/v2/oauth2/introspect/",
        revocation_endpoint = $"{baseUrl}/api/v2/oauth2/revoke/",
        jwks_uri = $"{baseUrl}/.well-known/jwks.json",
        response_types_supported = new[] { "code" },
        grant_types_supported = new[] { "authorization_code", "refresh_token" },
        code_challenge_methods_supported = new[] { "S256", "plain" },
        scopes_supported = officialThaIdScopes
    });
});

// ============================================================================
// 2. ThaID Authorization & Redirect Endpoint (/api/v2/oauth2/auth/)
// ============================================================================
IResult HandleThaIdAuthorize(HttpContext ctx)
{
    var query = ctx.Request.Query;
    string clientId = query["client_id"].ToString();
    string redirectUri = query["redirect_uri"].ToString();
    string scope = query["scope"].ToString();
    string state = query["state"].ToString();
    string codeChallenge = query["code_challenge"].ToString();
    string codeChallengeMethod = query["code_challenge_method"].ToString();
    string nonce = query["nonce"].ToString();
    string scenario = query["scenario"].ToString();
    string autoRedirect = query["auto"].ToString();

    if (string.IsNullOrWhiteSpace(redirectUri))
    {
        return Results.BadRequest(new { error = "invalid_request", error_description = "Missing redirect_uri parameter" });
    }

    bool acceptsHtml = ctx.Request.Headers.Accept.Any(h =>
        h is not null && h.Contains("text/html", StringComparison.OrdinalIgnoreCase));

    // If programmatic HttpClient request (not a browser requesting text/html) or auto=1/true or scenario is explicitly chosen:
    // Perform immediate HTTP 302 Found Redirect back to redirect_uri!
    if (!string.IsNullOrWhiteSpace(scenario) ||
        string.Equals(autoRedirect, "1", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(autoRedirect, "true", StringComparison.OrdinalIgnoreCase) ||
        !acceptsHtml)
    {
        string effectiveScenario = string.IsNullOrWhiteSpace(scenario) ? "success" : scenario.ToLowerInvariant();
        string separator = redirectUri.Contains('?') ? "&" : "?";

        if (effectiveScenario == "deny")
        {
            // UC-THAID-06: Citizen denied consent in ThaID app
            string denyUrl = $"{redirectUri}{separator}error=access_denied&error_description={Uri.EscapeDataString("User denied consent")}&state={Uri.EscapeDataString(state)}";
            return Results.Redirect(denyUrl, permanent: false);
        }

        if (effectiveScenario == "invalid_state")
        {
            // UC-THAID-02: Tampered state for CSRF protection test
            const string code = "mock-auth-code-001";
            codeChallenges[code] = string.IsNullOrWhiteSpace(codeChallenge) ? null : codeChallenge;
            string tamperedUrl = $"{redirectUri}{separator}code={Uri.EscapeDataString(code)}&state=tampered-invalid-state";
            return Results.Redirect(tamperedUrl, permanent: false);
        }

        if (effectiveScenario == "expired_code")
        {
            // UC-THAID-03: Expired authorization code
            const string expiredCode = "mock-expired-code";
            codeChallenges[expiredCode] = string.IsNullOrWhiteSpace(codeChallenge) ? null : codeChallenge;
            string expiredUrl = $"{redirectUri}{separator}code={Uri.EscapeDataString(expiredCode)}&state={Uri.EscapeDataString(state)}";
            return Results.Redirect(expiredUrl, permanent: false);
        }

        if (effectiveScenario == "timeout")
        {
            // UC-THAID-04: Timeout authorization code
            const string timeoutCode = "mock-timeout";
            codeChallenges[timeoutCode] = string.IsNullOrWhiteSpace(codeChallenge) ? null : codeChallenge;
            string timeoutUrl = $"{redirectUri}{separator}code={Uri.EscapeDataString(timeoutCode)}&state={Uri.EscapeDataString(state)}";
            return Results.Redirect(timeoutUrl, permanent: false);
        }

        // UC-THAID-01: Happy Path
        const string validCode = "mock-auth-code-001";
        codeChallenges[validCode] = string.IsNullOrWhiteSpace(codeChallenge) ? null : codeChallenge;
        string callbackUrl = $"{redirectUri}{separator}code={Uri.EscapeDataString(validCode)}&state={Uri.EscapeDataString(state)}";
        return Results.Redirect(callbackUrl, permanent: false);
    }

    // Interactive Browser Consent UI for Kiosk / Web Testing
    string baseQuery =
        $"response_type=code&client_id={Uri.EscapeDataString(clientId)}&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
        $"&scope={Uri.EscapeDataString(scope)}&state={Uri.EscapeDataString(state)}" +
        $"&code_challenge={Uri.EscapeDataString(codeChallenge)}&code_challenge_method={Uri.EscapeDataString(codeChallengeMethod)}" +
        $"&nonce={Uri.EscapeDataString(nonce)}";

    string consentHtml = $$"""
    <!DOCTYPE html>
    <html lang="th">
    <head>
        <meta charset="utf-8" />
        <title>ThaID Sandbox Simulator — ยืนยันตัวตนดิจิทัล</title>
        <style>
            body { font-family: 'Segoe UI', Tahoma, sans-serif; background: #0f172a; color: #f8fafc; display: flex; justify-content: center; align-items: center; min-height: 95vh; margin: 0; }
            .box { background: #1e293b; border: 1px solid #334155; border-radius: 14px; max-width: 540px; width: 100%; padding: 28px; box-shadow: 0 10px 30px rgba(0,0,0,0.4); }
            h2 { margin-top: 0; color: #38bdf8; }
            .meta { background: #0f172a; padding: 12px; border-radius: 8px; font-size: 13px; color: #94a3b8; margin-bottom: 20px; word-break: break-all; }
            .btn { display: block; width: 100%; text-align: left; padding: 12px 16px; margin-bottom: 10px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 14px; box-sizing: border-box; }
            .btn-success { background: #16a34a; color: white; }
            .btn-danger { background: #dc2626; color: white; }
            .btn-warn { background: #d97706; color: white; }
            .btn-secondary { background: #475569; color: white; }
        </style>
    </head>
    <body>
        <div class="box">
            <h2>จำลองแอปพลิเคชัน ThaID (OAuth 2.0 Redirect)</h2>
            <p>เลือกสถานการณ์ที่ต้องการจำลอง ระบบจะส่ง <code>HTTP 302 Redirect</code> กลับไปยัง Callback ของโปรเจกต์ปลายทางทันที:</p>
            <div class="meta">
                <div><strong>Client ID:</strong> {{WebUtility.HtmlEncode(clientId)}}</div>
                <div><strong>Redirect URI:</strong> {{WebUtility.HtmlEncode(redirectUri)}}</div>
                <div><strong>State:</strong> {{WebUtility.HtmlEncode(state)}}</div>
                <div><strong>PKCE Challenge ({{WebUtility.HtmlEncode(codeChallengeMethod)}}):</strong> {{WebUtility.HtmlEncode(codeChallenge)}}</div>
            </div>
            <a class="btn btn-success" href="/api/v2/oauth2/auth/?{{baseQuery}}&amp;scenario=success">
                ✅ UC-THAID-01: ยินยอมสำเร็จ (Redirect 302 พร้อมข้อมูลประชาชนครบ 20 Scopes)
            </a>
            <a class="btn btn-danger" href="/api/v2/oauth2/auth/?{{baseQuery}}&amp;scenario=deny">
                ❌ UC-THAID-06: ประชาชนกดยกเลิกในแอป ThaID (Redirect 302 error=access_denied)
            </a>
            <a class="btn btn-warn" href="/api/v2/oauth2/auth/?{{baseQuery}}&amp;scenario=invalid_state">
                ⚠️ UC-THAID-02: จำลองค่า State ไม่ตรง (ทดสอบการป้องกัน CSRF -> INVALID_STATE)
            </a>
            <a class="btn btn-secondary" href="/api/v2/oauth2/auth/?{{baseQuery}}&amp;scenario=expired_code">
                ⏰ UC-THAID-03: จำลอง Authorization Code หมดอายุ (Token HTTP 400 -> TOKEN_EXCHANGE_FAILED)
            </a>
            <a class="btn btn-secondary" href="/api/v2/oauth2/auth/?{{baseQuery}}&amp;scenario=timeout">
                🔌 UC-THAID-04: จำลองโครงข่ายล่มตอนแลก Token (NETWORK_ERROR)
            </a>
        </div>
    </body>
    </html>
    """;
    return Results.Content(consentHtml, "text/html; charset=utf-8");
}

app.MapGet("/api/v2/oauth2/auth/", (Delegate)HandleThaIdAuthorize);
app.MapGet("/api/v2/oauth2/auth", (Delegate)HandleThaIdAuthorize);

// ============================================================================
// 3. ThaID Token Endpoint (/api/v2/oauth2/token/)
// ============================================================================
async Task<IResult> HandleThaIdToken(HttpContext ctx)
{
    var form = await ctx.Request.ReadFormAsync();
    string code = form["code"].ToString();
    string codeVerifier = form["code_verifier"].ToString();

    if (string.IsNullOrWhiteSpace(code))
    {
        return Results.BadRequest(new { error = "invalid_request", error_description = "Missing code parameter" });
    }

    // UC-THAID-04: Simulate Network Connection Drop / Timeout
    if (string.Equals(code, "mock-timeout", StringComparison.OrdinalIgnoreCase))
    {
        ctx.Abort();
        return Results.Empty;
    }

    // UC-THAID-03: Expired or Invalid Authorization Code
    if (string.Equals(code, "mock-expired-code", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new
        {
            error = "invalid_grant",
            error_description = "Authorization code has expired"
        });
    }

    // Verify PKCE S256 if a code_challenge was registered during /api/v2/oauth2/auth/
    if (codeChallenges.TryGetValue(code, out string? storedChallenge) && !string.IsNullOrWhiteSpace(storedChallenge))
    {
        if (string.IsNullOrWhiteSpace(codeVerifier) || !PkceHelper.VerifyCodeChallenge(codeVerifier, storedChallenge))
        {
            return Results.BadRequest(new
            {
                error = "invalid_grant",
                error_description = "PKCE code_verifier verification failed"
            });
        }
    }

    // UC-THAID-01: Happy Path with all 20 Official ThaID Scopes
    return Results.Json(BuildThaIdCitizenPayload(includeTokens: true));
}

app.MapPost("/api/v2/oauth2/token/", (Delegate)HandleThaIdToken);
app.MapPost("/api/v2/oauth2/token", (Delegate)HandleThaIdToken);

// ============================================================================
// 4. ThaID Token Introspection (/api/v2/oauth2/introspect/)
// ============================================================================
async Task<IResult> HandleThaIdIntrospect(HttpContext ctx)
{
    var form = await ctx.Request.ReadFormAsync();
    string token = form["token"].ToString();

    if (string.IsNullOrWhiteSpace(token) ||
        string.Equals(token, "mock-expired-token", StringComparison.OrdinalIgnoreCase) ||
        revokedTokens.ContainsKey(token))
    {
        return Results.Json(new { active = false });
    }

    return Results.Json(new
    {
        active = true,
        sub = "1234567890121",
        scope = string.Join(' ', officialThaIdScopes)
    });
}

app.MapPost("/api/v2/oauth2/introspect/", (Delegate)HandleThaIdIntrospect);
app.MapPost("/api/v2/oauth2/introspect", (Delegate)HandleThaIdIntrospect);

// ============================================================================
// 5. ThaID Token Revocation (/api/v2/oauth2/revoke/)
// ============================================================================
async Task<IResult> HandleThaIdRevoke(HttpContext ctx)
{
    var form = await ctx.Request.ReadFormAsync();
    string token = form["token"].ToString();
    if (!string.IsNullOrWhiteSpace(token))
    {
        revokedTokens[token] = true;
    }

    return Results.Json(new { message = "revoked" });
}

app.MapPost("/api/v2/oauth2/revoke/", (Delegate)HandleThaIdRevoke);
app.MapPost("/api/v2/oauth2/revoke", (Delegate)HandleThaIdRevoke);

// ============================================================================
// 6. ThaID UserInfo Endpoint (/api/v2/oauth2/userinfo/)
// ============================================================================
IResult HandleThaIdUserInfo(HttpContext ctx)
{
    string authHeader = ctx.Request.Headers.Authorization.ToString();
    if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Unauthorized();
    }

    string token = authHeader["Bearer ".Length..].Trim();
    if (string.Equals(token, "mock-expired-token", StringComparison.OrdinalIgnoreCase) || revokedTokens.ContainsKey(token))
    {
        return Results.Unauthorized();
    }

    return Results.Json(BuildThaIdCitizenPayload(includeTokens: false));
}

app.MapGet("/api/v2/oauth2/userinfo/", (Delegate)HandleThaIdUserInfo);
app.MapGet("/api/v2/oauth2/userinfo", (Delegate)HandleThaIdUserInfo);

// ============================================================================
// 7. DOPA CheckCardByLaser (/CheckCardByLaser)
// ============================================================================
app.MapPost("/CheckCardByLaser", async (HttpContext ctx) =>
{
    using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
    var root = doc.RootElement;
    string laser = root.TryGetProperty("Laser", out var laserProp) ? laserProp.GetString() ?? "" : "";

    // UC-DOPA-06: Network / VPN Timeout
    if (string.Equals(laser, "METIMEOUT000", StringComparison.OrdinalIgnoreCase))
    {
        ctx.Abort();
        return Results.Empty;
    }

    // UC-DOPA-05: DOPA Gateway Unavailable (HTTP 503)
    if (string.Equals(laser, "ME5000000000", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Json(
            new { message = "DOPA Gateway Service Unavailable" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    // UC-DOPA-04: Card Verification Rejected (IsError = true)
    if (string.Equals(laser, "ME9999999999", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Json(new
        {
            IsError = true,
            Code = 1,
            Desc = "สถานะไม่ปกติ",
            ErrorMessage = "ไม่พบเลข Laser จาก PID นี้"
        });
    }

    // UC-DOPA-01: Happy Path
    return Results.Json(new
    {
        IsError = false,
        Code = 0,
        Desc = "สถานะปกติ"
    });
});

// ============================================================================
// 8. DOPA CheckCardByCID (/CheckCardByCID)
// ============================================================================
app.MapPost("/CheckCardByCID", async (HttpContext ctx) =>
{
    using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
    var root = doc.RootElement;
    string chipNo = root.TryGetProperty("ChipNo", out var chipProp) ? chipProp.GetString() ?? "" : "";

    // UC-DOPA-06: Network / VPN Timeout
    if (string.Equals(chipNo, "TIMEOUT000000000", StringComparison.OrdinalIgnoreCase))
    {
        ctx.Abort();
        return Results.Empty;
    }

    // UC-DOPA-05: DOPA Gateway Unavailable (HTTP 503)
    if (string.Equals(chipNo, "5000000000000000", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Json(
            new { message = "DOPA Gateway Service Unavailable" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    // UC-DOPA-04: Card Verification Rejected (IsError = true)
    if (string.Equals(chipNo, "0000000000000000", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Json(new
        {
            IsError = true,
            Code = 1,
            Desc = "สถานะไม่ปกติ",
            ErrorMessage = "ข้อมูล ChipNo หรือ bp1no ไม่ถูกต้อง"
        });
    }

    // UC-DOPA-01: Happy Path
    return Results.Json(new
    {
        IsError = false,
        Code = 0,
        Desc = "สถานะปกติ"
    });
});

app.Run();

static Dictionary<string, object> BuildThaIdCitizenPayload(bool includeTokens)
{
    var payload = new Dictionary<string, object>
    {
        ["sub"] = "1234567890121",
        ["pid"] = "1234567890121",
        ["title"] = "นาย",
        ["title_en"] = "Mr.",
        ["given_name"] = "สมชาย",
        ["middle_name"] = "",
        ["family_name"] = "ใจดี",
        ["name"] = "นาย สมชาย ใจดี",
        ["given_name_en"] = "Somchai",
        ["middle_name_en"] = "",
        ["family_name_en"] = "Jaidee",
        ["name_en"] = "Mr. Somchai Jaidee",
        ["gender"] = "male",
        ["birthdate"] = "1990-01-31",
        ["address"] = "99/9 หมู่ 1 ต.คลองหนึ่ง อ.คลองหลวง จ.ปทุมธานี",
        ["house_address"] = new Dictionary<string, object>
        {
            ["raw"] = "99/9 หมู่ 1 ต.คลองหนึ่ง อ.คลองหลวง จ.ปทุมธานี",
            ["formatted"] = "99/9 หมู่ 1 ต.คลองหนึ่ง อ.คลองหลวง จ.ปทุมธานี"
        },
        ["smartcard_code"] = "ME0123456789",
        ["date_of_issuance"] = "2022-01-31",
        ["date_of_expiry"] = "2030-01-30",
        ["ial"] = 2.3
    };

    if (includeTokens)
    {
        payload["access_token"] = "mock-thaid-access-token";
        payload["refresh_token"] = "mock-thaid-refresh-token";
        payload["id_token"] = "mock-thaid-id-token";
        payload["token_type"] = "Bearer";
        payload["expires_in"] = 3600;
        payload["scope"] = "openid pid address gender birthdate given_name middle_name family_name name given_name_en middle_name_en family_name_en name_en title title_en ial smartcard_code date_of_expiry date_of_issuance house_address";
    }

    return payload;
}

public partial class Program;
