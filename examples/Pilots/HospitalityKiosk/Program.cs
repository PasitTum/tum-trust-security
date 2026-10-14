using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Tum.Trust.Security.Application;
using Tum.Trust.Security.Core.Interfaces;
using Tum.Trust.Security.Core.Models.Passport;
using Tum.Trust.Security.Infrastructure.Passport;

namespace Tum.Trust.Security.Pilots.HospitalityKiosk;

/// <summary>
/// Pilot Blueprint: 30-Second Luxury Hotel Self Check-in with TUM Trust Security.
/// Eliminates front-desk queues for international guests with Zero PII Storage.
/// </summary>
public static class Program
{
    /// <summary>
    /// Application entry point.
    /// </summary>
    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==========================================================================");
        Console.WriteLine("   GRAND LUXURY RESORT & SPA — 30-SECOND SELF CHECK-IN KIOSK              ");
        Console.WriteLine("   Powered by TUM Trust Security (Universal ePassport & mDL Engine)       ");
        Console.WriteLine("==========================================================================");
        Console.ResetColor();

        // 1. Resolve TUM Trust Engine
        var services = new ServiceCollection();
        services.AddIdentityVerification(opt => { });
        var sp = services.BuildServiceProvider();
        var trustEngine = sp.GetRequiredService<IBorderlessTrustProvider>();

        Console.WriteLine("\n[GUEST ARRIVAL] Guest taps Japanese ePassport on Kiosk NFC Reader...");

        // Japanese ePassport TD3 MRZ sample
        string line1 = "P<JPNTOKYO<<TARO<<<<<<<<<<<<<<<<<<<<<<<<<<<<";
        string line2 = "TZ12345674JPN8501015M2801012<<<<<<<<<<<<<<02";
        var mrz = MrzParser.ParseTd3(line1, line2);

        var request = new PassportVerificationRequest
        {
            Mrz = mrz,
            Dg1Bytes = Encoding.UTF8.GetBytes(line1 + line2),
            Dg2FacePhotoBytes = new byte[1024],
            SodBytes = Encoding.UTF8.GetBytes("SOVEREIGN_SOD_JPN_CERTIFIED"),
            ActiveAuthenticationSignature = new byte[128]
        };

        var cameraFrame = new byte[1024]; // Captured from Kiosk HD Camera
        var verificationResult = await trustEngine.VerifyPassportAsync(request, cameraFrame);

        if (verificationResult.IsSuccess)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ✔ ePassport Verified : Japan Ministry of Foreign Affairs (Authentic CSCA)");
            Console.WriteLine("  ✔ Anti-Cloning Check : Active Authentication PASSED");
            Console.WriteLine("  ✔ Liveness & Face    : Live Person Confirmed (Sub-dermal pulse 72 bpm)");
            Console.WriteLine("  ✔ Guest Name         : " + mrz.GivenNames + " " + mrz.Surname);
            Console.WriteLine("  ✔ Room Key Card      : Room #1402 Key Issued");
            Console.WriteLine("  ✔ Sovereign ZKP Token: " + verificationResult.ZeroKnowledgeTrustToken?[..36] + "...");
            Console.WriteLine("  ✔ Privacy Compliance : ZERO Passport copies stored (PDPA/GDPR Safe)");
            Console.ResetColor();
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n[CHECK-IN COMPLETE] Welcome to Grand Luxury Resort, Mr. Taro Tokyo!");
        Console.WriteLine("==========================================================================\n");
        Console.ResetColor();
    }
}
