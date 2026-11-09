using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tum.Trust.KioskService.Recovery;
using Tum.Trust.Security.Core.Interfaces;
using Tum.Trust.Security.Core.Models.Hardware;
using Tum.Trust.Security.Infrastructure.Biometrics;
using Tum.Trust.Security.Infrastructure.Hardware;
using Tum.Trust.Security.Infrastructure.Passport;

namespace Tum.Trust.KioskService.Controllers;

/// <summary>
/// Embedded high-performance HTTP REST and Server-Sent Events (SSE) server for Kiosk HAL diagnostics.
/// Exposes endpoints on localhost for the Electron Diagnostics App and local kiosk browser frontend.
/// </summary>
public class KioskHalServer : IDisposable
{
    private readonly ILogger<KioskHalServer> _logger;
    private readonly UsbAutoRecoveryEngine _recoveryEngine;
    private readonly PcScSmartcardReaderAdapter _readerAdapter;
    private readonly ThaiIdSmartCardReader _thaiIdReader;
    private readonly EdgeFaceLivenessDetector _livenessDetector;
    private readonly IcaoNfcDataGroupInspector _passportInspector;
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<string, StreamWriter> _sseClients = new();
    private readonly DateTime _startTimeUtc = DateTime.UtcNow;
    private Task? _listenerTask;
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public KioskHalServer(
        ILogger<KioskHalServer> logger,
        UsbAutoRecoveryEngine recoveryEngine,
        PcScSmartcardReaderAdapter readerAdapter)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _recoveryEngine = recoveryEngine ?? throw new ArgumentNullException(nameof(recoveryEngine));
        _readerAdapter = readerAdapter ?? throw new ArgumentNullException(nameof(readerAdapter));
        _thaiIdReader = new ThaiIdSmartCardReader(_readerAdapter);
        _livenessDetector = new EdgeFaceLivenessDetector();
        _passportInspector = new IcaoNfcDataGroupInspector();

        _listener = new HttpListener();
        _listener.Prefixes.Add("http://127.0.0.1:5890/");

        // Subscribe to recovery events to push live telemetry to SSE clients
        _recoveryEngine.OnDeviceStatusChanged += (s, e) => BroadcastEvent("device_status_changed", e);
        _recoveryEngine.OnRecoveryCompleted += (s, e) => BroadcastEvent("recovery_completed", e);
        _readerAdapter.OnCardInserted += (s, e) => BroadcastEvent("card_inserted", e);
        _readerAdapter.OnCardRemoved += (s, reader) => BroadcastEvent("card_removed", new { readerName = reader });
    }

    public void Start()
    {
        try
        {
            _listener.Start();
            _listenerTask = Task.Run(ListenLoopAsync, _cts.Token);
            _logger.LogInformation("Kiosk HAL Server started on http://127.0.0.1:5890/");
        }
        catch (HttpListenerException ex)
        {
            _logger.LogError(ex, "Failed to start HttpListener on port 5890. Ensure URL ACL or run as service.");
        }
    }

    private async Task ListenLoopAsync()
    {
        while (!_cts.Token.IsCancellationRequested && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = HandleRequestAsync(context);
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in KioskHalServer listener loop");
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var req = context.Request;
        var res = context.Response;

        // Add standard CORS headers for local Electron & Browser access
        res.Headers.Add("Access-Control-Allow-Origin", "*");
        res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        res.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");

        if (req.HttpMethod == "OPTIONS")
        {
            res.StatusCode = 200;
            res.Close();
            return;
        }

        string path = req.Url?.AbsolutePath.ToLowerInvariant() ?? "/";

        try
        {
            switch (path)
            {
                case "/health":
                    await HandleGetHealthAsync(res);
                    break;

                case "/hal/readers":
                    await HandleGetReadersAsync(res);
                    break;

                case "/hal/smartcard/read-thai-id":
                    await HandleReadThaiIdAsync(req, res);
                    break;

                case "/hal/smartcard/transmit":
                    await HandleTransmitApduAsync(req, res);
                    break;

                case "/hal/biometric/liveness":
                    await HandleBiometricLivenessAsync(req, res);
                    break;

                case "/hal/passport/inspect":
                    await HandlePassportInspectAsync(req, res);
                    break;

                case "/hal/simulate/disconnect":
                    await HandleSimulateDisconnectAsync(req, res);
                    break;

                case "/hal/simulate/reconnect":
                    await HandleSimulateReconnectAsync(req, res);
                    break;

                case "/events":
                    await HandleSseEventsAsync(req, res);
                    return; // Keep connection open for SSE

                default:
                    await SendJsonResponseAsync(res, 404, new { error = "Not Found", path });
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error processing HAL request {Path}", path);
            await SendJsonResponseAsync(res, 500, new { error = ex.Message });
        }
        finally
        {
            if (path != "/events")
            {
                res.Close();
            }
        }
    }

    private async Task HandleGetHealthAsync(HttpListenerResponse res)
    {
        var devices = _recoveryEngine.GetHealthSnapshot();
        var readers = await _readerAdapter.EnumerateReadersAsync();

        var uptime = DateTime.UtcNow - _startTimeUtc;

        var healthData = new
        {
            status = "UP",
            service = "TUM.Trust.KioskService",
            version = "1.4.0",
            uptimeSeconds = (long)uptime.TotalSeconds,
            timestampUtc = DateTime.UtcNow,
            peripherals = devices,
            pcscReaders = readers,
            usbAutoRecovery = new
            {
                engineActive = true,
                monitoredDeviceCount = devices.Count,
                totalRecoveries = devices.Sum(d => d.SuccessfulRecoveryCount),
                totalDisconnects = devices.Sum(d => d.DisconnectCount)
            }
        };

        await SendJsonResponseAsync(res, 200, healthData);
    }

    private async Task HandleGetReadersAsync(HttpListenerResponse res)
    {
        var readers = await _readerAdapter.EnumerateReadersAsync();
        var result = readers.Select(r => new
        {
            name = r.DeviceName,
            type = r.Type.ToString(),
            isConnected = r.IsConnected,
            vendorId = r.VendorId,
            productId = r.ProductId,
            firmwareVersion = r.FirmwareVersion,
            isCardInserted = _readerAdapter.IsCardInserted(r.DeviceName)
        });

        await SendJsonResponseAsync(res, 200, result);
    }

    private async Task HandleReadThaiIdAsync(HttpListenerRequest req, HttpListenerResponse res)
    {
        using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
        string body = await reader.ReadToEndAsync();
        string readerName = "ACS ACR1252U Contactless NFC Reader 0";

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("readerName", out var rProp))
                    readerName = rProp.GetString() ?? readerName;
            }
            catch { }
        }

        try
        {
            var data = await _thaiIdReader.ReadThaiIdCardAsync(readerName, includePhoto: true);
            await SendJsonResponseAsync(res, 200, new
            {
                success = true,
                citizenId = data.MaskedCitizenId,
                rawCitizenId = data.CitizenId,
                fullNameTh = data.FullNameTh,
                fullNameEn = data.FullNameEn,
                dateOfBirth = data.DateOfBirth,
                gender = data.Gender,
                address = data.Address,
                issueDate = data.IssueDate,
                expirationDate = data.ExpirationDate,
                issuer = data.Issuer,
                hasPhoto = data.PhotoBytes != null && data.PhotoBytes.Length > 0,
                photoBase64 = data.PhotoBytes != null ? Convert.ToBase64String(data.PhotoBytes) : null
            });
        }
        catch (Exception ex)
        {
            _recoveryEngine.ReportFailure(readerName, ex);
            await SendJsonResponseAsync(res, 502, new { success = false, error = ex.Message });
        }
    }

    private async Task HandleTransmitApduAsync(HttpListenerRequest req, HttpListenerResponse res)
    {
        using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
        string body = await reader.ReadToEndAsync();
        using var doc = JsonDocument.Parse(body);

        string readerName = doc.RootElement.GetProperty("readerName").GetString() ?? "";
        string apduHex = doc.RootElement.GetProperty("apduHex").GetString() ?? "";

        byte[] apduBytes = ConvertHexStringToByteArray(apduHex);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        byte[] response = await _readerAdapter.TransmitApduAsync(readerName, apduBytes);
        sw.Stop();

        string respHex = BitConverter.ToString(response).Replace("-", " ");
        string swHex = response.Length >= 2 
            ? $"{response[^2]:X2} {response[^1]:X2}" 
            : "N/A";

        await SendJsonResponseAsync(res, 200, new
        {
            success = true,
            readerName,
            commandHex = apduHex,
            responseHex = respHex,
            statusWord = swHex,
            isSuccessStatusWord = response.Length >= 2 && response[^2] == 0x90 && response[^1] == 0x00,
            elapsedMs = sw.ElapsedMilliseconds
        });
    }

    private async Task HandleBiometricLivenessAsync(HttpListenerRequest req, HttpListenerResponse res)
    {
        using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
        string body = await reader.ReadToEndAsync();
        using var doc = JsonDocument.Parse(body);

        byte[] imageBytes;
        if (doc.RootElement.TryGetProperty("imageBase64", out var base64Prop))
        {
            string rawB64 = base64Prop.GetString() ?? "";
            if (rawB64.Contains(',')) rawB64 = rawB64.Split(',')[1];
            imageBytes = Convert.FromBase64String(rawB64);
        }
        else
        {
            // Default sample image buffer
            imageBytes = Encoding.UTF8.GetBytes("LIVE_TEST_FRAME_DEFAULT");
        }

        var result = await _livenessDetector.DetectLivenessAsync(imageBytes);

        await SendJsonResponseAsync(res, 200, new
        {
            isLiveHuman = result.IsLiveHuman,
            livenessScore = result.LivenessScore,
            attackType = result.AttackType.ToString(),
            diagnosticMessage = result.DiagnosticMessage,
            estimatedHeartRateBpm = result.EstimatedHeartRateBpm,
            executionTimeMs = result.ExecutionTimeMs
        });
    }

    private async Task HandlePassportInspectAsync(HttpListenerRequest req, HttpListenerResponse res)
    {
        // Sample mock ePassport LDS extraction
        byte[] dg1Mock = Encoding.ASCII.GetBytes("P<UTOERIKSSON<<ANNA<MARIA<<<<<<<<<<<<<<<<<<<L898902C36UTO7408122F1204159ZE184226B<<<<<10");
        byte[] dg2Mock = new byte[] { 0x75, 0x20, 0x7F, 0x61, 0x1C, 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00 };
        byte[] dg14Mock = new byte[] { 0x6E, 0x10, 0x04, 0x00, 0x7F, 0x00, 0x07 };
        byte[] dg15Mock = new byte[] { 0x6F, 0x20, 0x30, 0x1E, 0x02, 0x01, 0x00 };

        var dg2Face = _passportInspector.InspectDg2Face(dg2Mock);
        var chipAuth = _passportInspector.InspectDg14(dg14Mock);
        var activeAuth = _passportInspector.InspectDg15(dg15Mock);

        await SendJsonResponseAsync(res, 200, new
        {
            mrz = "P<UTOERIKSSON<<ANNA<MARIA<<<<<<<<<<<<<<<<<<<L898902C36UTO7408122F1204159ZE184226B<<<<<10",
            dg1Present = true,
            dg2Face = new
            {
                isValid = dg2Face.IsValid,
                format = dg2Face.ImageFormat,
                width = dg2Face.ImageWidth,
                height = dg2Face.ImageHeight
            },
            chipAuth = new
            {
                isPresent = chipAuth.IsPresent,
                protocol = chipAuth.ProtocolName,
                keyLength = chipAuth.KeyLengthBits
            },
            activeAuth = new
            {
                isPresent = activeAuth.IsPresent,
                keyType = activeAuth.KeyType,
                modulusBits = activeAuth.ModulusLengthBits
            },
            passiveAuth = new
            {
                sodSignatureValid = true,
                signer = "CN=Document Signer Utopia, O=ICAO PKD Test, C=UTO",
                status = "PASSED"
            }
        });
    }

    private async Task HandleSimulateDisconnectAsync(HttpListenerRequest req, HttpListenerResponse res)
    {
        using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
        string body = await reader.ReadToEndAsync();
        string devName = "ACS ACR1252U Contactless NFC Reader 0";
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("deviceName", out var dProp))
                    devName = dProp.GetString() ?? devName;
            }
            catch { }
        }

        _recoveryEngine.SimulateUsbDisconnect(devName);
        await SendJsonResponseAsync(res, 200, new { message = $"Simulated USB disconnect for {devName}", status = "UNPLUGGED" });
    }

    private async Task HandleSimulateReconnectAsync(HttpListenerRequest req, HttpListenerResponse res)
    {
        using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
        string body = await reader.ReadToEndAsync();
        string devName = "ACS ACR1252U Contactless NFC Reader 0";
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("deviceName", out var dProp))
                    devName = dProp.GetString() ?? devName;
            }
            catch { }
        }

        _recoveryEngine.SimulateUsbReconnect(devName);
        await SendJsonResponseAsync(res, 200, new { message = $"Simulated USB reconnect for {devName}", status = "ONLINE" });
    }

    private async Task HandleSseEventsAsync(HttpListenerRequest req, HttpListenerResponse res)
    {
        res.ContentType = "text/event-stream";
        res.Headers.Add("Cache-Control", "no-cache");
        res.Headers.Add("Connection", "keep-alive");

        string clientId = Guid.NewGuid().ToString("N");
        var writer = new StreamWriter(res.OutputStream, Encoding.UTF8) { AutoFlush = true };
        _sseClients[clientId] = writer;

        _logger.LogInformation("SSE Client connected: {ClientId}", clientId);

        try
        {
            // Send initial connection event
            await writer.WriteLineAsync($"event: connected\ndata: {{\"clientId\":\"{clientId}\"}}\n\n");

            // Keep connection alive until client disconnects or cancellation
            while (!_cts.Token.IsCancellationRequested)
            {
                await Task.Delay(15000, _cts.Token);
                await writer.WriteLineAsync(":ping\n\n");
            }
        }
        catch
        {
            // Client closed connection
        }
        finally
        {
            _sseClients.TryRemove(clientId, out _);
            _logger.LogInformation("SSE Client disconnected: {ClientId}", clientId);
        }
    }

    private void BroadcastEvent(string eventName, object payload)
    {
        string json = JsonSerializer.Serialize(payload, JsonOpts);
        string message = $"event: {eventName}\ndata: {json}\n\n";

        foreach (var (cid, writer) in _sseClients)
        {
            try
            {
                writer.WriteLine(message);
            }
            catch
            {
                _sseClients.TryRemove(cid, out _);
            }
        }
    }

    private static async Task SendJsonResponseAsync(HttpListenerResponse res, int statusCode, object data)
    {
        res.StatusCode = statusCode;
        res.ContentType = "application/json; charset=utf-8";
        byte[] buffer = JsonSerializer.SerializeToUtf8Bytes(data, JsonOpts);
        res.ContentLength64 = buffer.Length;
        await res.OutputStream.WriteAsync(buffer);
    }

    private static byte[] ConvertHexStringToByteArray(string hex)
    {
        hex = hex.Replace(" ", "").Replace("-", "").Replace("0x", "").Trim();
        if (hex.Length % 2 != 0) hex = "0" + hex;
        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return bytes;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts.Cancel();
        _cts.Dispose();
        try { _listener.Stop(); } catch { }
        _recoveryEngine.Dispose();
    }
}
