using Netka.Identity.Core.Models.Passport;

namespace Netka.Identity.Infrastructure.Passport;

/// <summary>
/// Parser and validator for ICAO Doc 9303 Machine Readable Travel Documents (MRTD).
/// Implements standard 7-3-1 weight Modulo 10 check digit algorithms.
/// </summary>
public static class MrzParser
{
    private static readonly int[] Weights = { 7, 3, 1 };

    /// <summary>
    /// Parses a 2-line TD3 (ePassport standard: 2x44 characters) MRZ block.
    /// </summary>
    /// <param name="line1">First 44-character line.</param>
    /// <param name="line2">Second 44-character line.</param>
    /// <returns>A validated <see cref="MrzInfo"/> instance.</returns>
    public static MrzInfo ParseTd3(string line1, string line2)
    {
        if (string.IsNullOrWhiteSpace(line1) || line1.Length < 44)
            throw new ArgumentException("Line 1 of TD3 MRZ must be at least 44 characters.", nameof(line1));

        if (string.IsNullOrWhiteSpace(line2) || line2.Length < 44)
            throw new ArgumentException("Line 2 of TD3 MRZ must be at least 44 characters.", nameof(line2));

        var mrz = new MrzInfo
        {
            RawLines = new[] { line1, line2 },
            DocumentType = line1.Substring(0, 2).TrimEnd('<'),
            IssuingCountry = line1.Substring(2, 3).Replace('<', ' ').Trim()
        };

        // Parse Names (Surname << GivenNames)
        var namePart = line1.Substring(5, 39);
        var separatorIdx = namePart.IndexOf("<<", StringComparison.Ordinal);
        if (separatorIdx >= 0)
        {
            mrz.Surname = namePart.Substring(0, separatorIdx).Replace('<', ' ').Trim();
            mrz.GivenNames = namePart.Substring(separatorIdx + 2).Replace('<', ' ').Trim();
        }
        else
        {
            mrz.Surname = namePart.Replace('<', ' ').Trim();
        }

        // Line 2: Document Number, Check Digit, Nationality, DOB, Sex, Expiry
        mrz.DocumentNumber = line2.Substring(0, 9).TrimEnd('<');
        var docNumCheck = line2[9];

        mrz.Nationality = line2.Substring(10, 3).Replace('<', ' ').Trim();
        mrz.DateOfBirth = line2.Substring(13, 6);
        var dobCheck = line2[19];

        mrz.Sex = line2.Substring(20, 1);
        mrz.DateOfExpiry = line2.Substring(21, 6);
        var expiryCheck = line2[27];

        mrz.OptionalData = line2.Substring(28, 14).TrimEnd('<');
        var compositeCheck = line2[43];

        // Verify Check Digits (7-3-1 weight Modulo 10)
        bool isDocValid = VerifyCheckDigit(mrz.DocumentNumber, docNumCheck);
        bool isDobValid = VerifyCheckDigit(mrz.DateOfBirth, dobCheck);
        bool isExpiryValid = VerifyCheckDigit(mrz.DateOfExpiry, expiryCheck);

        mrz.IsChecksumValid = isDocValid && isDobValid && isExpiryValid;
        return mrz;
    }

    /// <summary>
    /// Computes or verifies the ICAO 9303 check digit for an input character sequence.
    /// </summary>
    public static bool VerifyCheckDigit(string input, char expectedCheckDigit)
    {
        int calculated = CalculateCheckDigit(input);
        int expected = expectedCheckDigit - '0';
        return calculated == expected;
    }

    /// <summary>
    /// Calculates the ICAO 9303 check digit using 7-3-1 weighting modulo 10.
    /// Characters A-Z map to 10-35, '&lt;' maps to 0.
    /// </summary>
    public static int CalculateCheckDigit(string input)
    {
        if (string.IsNullOrEmpty(input)) return 0;

        int sum = 0;
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            int value;

            if (c >= '0' && c <= '9')
                value = c - '0';
            else if (c >= 'A' && c <= 'Z')
                value = c - 'A' + 10;
            else if (c == '<')
                value = 0;
            else
                value = 0;

            int weight = Weights[i % 3];
            sum += value * weight;
        }

        return sum % 10;
    }
}
