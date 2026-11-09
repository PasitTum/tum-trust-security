using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tum.Trust.KioskService.Controllers;
using Tum.Trust.KioskService.Recovery;

namespace Tum.Trust.KioskService;

/// <summary>
/// Core Windows Background Service / Linux systemd Daemon for Kiosk Hardware Abstraction Layer.
/// Runs continuously as an OS service with auto-recovery and local IPC hosting.
/// </summary>
public class KioskBackgroundService : BackgroundService
{
    private readonly ILogger<KioskBackgroundService> _logger;
    private readonly UsbAutoRecoveryEngine _recoveryEngine;
    private readonly KioskHalServer _halServer;

    public KioskBackgroundService(
        ILogger<KioskBackgroundService> logger,
        UsbAutoRecoveryEngine recoveryEngine,
        KioskHalServer halServer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _recoveryEngine = recoveryEngine ?? throw new ArgumentNullException(nameof(recoveryEngine));
        _halServer = halServer ?? throw new ArgumentNullException(nameof(halServer));
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("=================================================");
        _logger.LogInformation(" TUM Trust Security — Kiosk HAL Daemon v1.4.0");
        _logger.LogInformation(" Commercial Production Background Service");
        _logger.LogInformation("=================================================");

        _recoveryEngine.Start();
        _halServer.Start();

        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("KioskBackgroundService successfully running. Listening for HAL requests & PnP events.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Heartbeat log every 60 seconds
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                var health = _recoveryEngine.GetHealthSnapshot();
                _logger.LogInformation("Kiosk HAL Heartbeat: {Count} devices supervised. System healthy.", health.Count);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("KioskBackgroundService stopping gracefully...");
        _halServer.Dispose();
        _recoveryEngine.Dispose();
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("KioskBackgroundService stopped cleanly.");
    }
}
