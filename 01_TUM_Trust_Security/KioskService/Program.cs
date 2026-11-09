using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tum.Trust.KioskService;
using Tum.Trust.KioskService.Controllers;
using Tum.Trust.KioskService.Recovery;
using Tum.Trust.Security.Infrastructure.Hardware;

namespace Tum.Trust.KioskService;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .UseWindowsService(options =>
            {
                options.ServiceName = "TumTrustKioskService";
            })
            .ConfigureServices((hostContext, services) =>
            {
                // Singletons for hardware abstraction and auto-recovery
                services.AddSingleton<PcScSmartcardReaderAdapter>();
                services.AddSingleton<UsbAutoRecoveryEngine>();
                services.AddSingleton<KioskHalServer>();

                // Hosted background service daemon
                services.AddHostedService<KioskBackgroundService>();
            })
            .ConfigureLogging((hostContext, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                if (OperatingSystem.IsWindows())
                {
                    logging.AddEventLog(settings =>
                    {
                        settings.SourceName = "TumTrustSecurity";
                        settings.LogName = "Application";
                    });
                }
                logging.SetMinimumLevel(LogLevel.Information);
            });

        var host = builder.Build();
        await host.RunAsync();
    }
}
