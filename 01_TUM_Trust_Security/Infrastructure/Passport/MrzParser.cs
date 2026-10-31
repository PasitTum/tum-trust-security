using Tum.Trust.Security.Core.Models.Passport;

namespace Tum.Trust.Security.Infrastructure.Passport;

/// <summary>
/// Parser and validator for ICAO Doc 9303 Machine Readable Travel Documents (MRTD).
/// Implements TD1 (ID Cards: 3x30), TD2 (Visas/Cards: 2x36), and TD3 (ePassports: 2x44)
/// with 7-3-1 weight Modulo 10 check digit algorithms.
/// </summary>
public static class MrzParser
{
    private static readonly int[] Weights = { 7, 3, 1 };

    /// <summary>
    /// Parses an MRZ block by auto-detecting layout format (TD1, TD2, or TD3).
    /// </summary>
    /// <param name="mrzText">Raw multiline MRZ string.</param>
    /// <returns>A validated <see cref="MrzInfo"/> instance.</returns>
    public static MrzInfo Parse(string mrzText)
    {
        if (string.IsNullOrWhiteSpace(mrzText))
            throw new ArgumentException("MRZ text cannot be null or empty.", nameof(mrzText));

        var lines = mrzText
            .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToArray();

        if (lines.Length == 3 && lines.All(l => l.Length >= 30))
        {
            return ParseTd1(lines[0], lines[1], lines[2]);
        }
        else if (lines.Length == 2 && lines.All(l => l.Length == 36))
        {
            return ParseTd2(lines[0], lines[1]);
        }
        else if (lines.Length == 2 && lines.All(l => l.Length >= 44))
        {
            return ParseTd3(lines[0], lines[1]);
        }

        throw new ArgumentException($"Unrecognized MRZ format. Found {lines.Length} lines. Supported: TD1 (3x30), TD2 (2x36), TD3 (2x44).", nameof(mrzText));
    }

    /// <summary>
    /// Parses a 3-line TD1 (ID-1 smart card format: 3x30 characters) MRZ block.
    /// </summary>
    public static MrzInfo ParseTd1(string line1, string line2, string line3)
    {
        if (string.IsNullOrWhiteSpace(line1) || line1.Length < 30)
            throw new ArgumentException("Line 1 of TD1 MRZ must be at least 30 characters.", nameof(line1));
        if (string.IsNullOrWhiteSpace(line2) || line2.Length < 30)
            throw new ArgumentException("Line 2 of TD1 MRZ must be at least 30 characters.", nameof(line2));
        if (string.IsNullOrWhiteSpace(line3) || line3.Length < 30)
            throw new ArgumentException("Line 3 of TD1 MRZ must be at least 30 characters.", nameof(line3));

        var mrz = new MrzInfo
        {
            Format = "TD1",
            RawLines = new[] { line1, line2, line3 },
            DocumentType = line1.Substring(0, 2).TrimEnd('<'),
            IssuingCountry = line1.Substring(2, 3).Replace('<', ' ').Trim(),
            DocumentNumber = line1.Substring(5, 9).TrimEnd('<')
        };

        char docCheck = line1[14];
        string optional1 = line1.Substring(15, 15).TrimEnd('<');

        mrz.DateOfBirth = line2.Substring(0, 6);
        char dobCheck = line2[6];
        mrz.Sex = line2.Substring(7, 1);
        mrz.DateOfExpiry = line2.Substring(8, 6);
        char expiryCheck = line2[14];
        mrz.Nationality = line2.Substring(15, 3).Replace('<', ' ').Trim();
        string optional2 = line2.Substring(18, 11).TrimEnd('<');
        char compositeCheck = line2[29];

        mrz.OptionalData = (optional1 + optional2).Trim();

        // Line 3: Names (Surname << GivenNames)
        ParseNames(line3, mrz);

        // Verification
        mrz.DocumentNumberCheckValid = VerifyCheckDigit(mrz.DocumentNumber, docCheck);
        mrz.DateOfBirthCheckValid = VerifyCheckDigit(mrz.DateOfBirth, dobCheck);
        mrz.DateOfExpiryCheckValid = VerifyCheckDigit(mrz.DateOfExpiry, expiryCheck);

        // TD1 composite check digit over: line1[5..29] + line2[0..6] + line2[8..14] + line2[18..28]
        string compositePayload = line1.Substring(5, 25) + line2.Substring(0, 7) + line2.Substring(8, 7) + line2.Substring(18, 11);
        mrz.CompositeCheckValid = VerifyCheckDigit(compositePayload, compositeCheck);

        mrz.IsChecksumValid = mrz.DocumentNumberCheckValid &&
                              mrz.DateOfBirthCheckValid &&
                              mrz.DateOfExpiryCheckValid &&
                              mrz.CompositeCheckValid;

        CollectValidationErrors(mrz);
        return mrz;
    }

    /// <summary>
    /// Parses a 2-line TD2 (ID-2 / Visa format: 2x36 characters) MRZ block.
    /// </summary>
    public static MrzInfo ParseTd2(string line1, string line2)
    {
        if (string.IsNullOrWhiteSpace(line1) || line1.Length < 36)
            throw new ArgumentException("Line 1 of TD2 MRZ must be at least 36 characters.", nameof(line1));
        if (string.IsNullOrWhiteSpace(line2) || line2.Length < 36)
            throw new ArgumentException("Line 2 of TD2 MRZ must be at least 36 characters.", nameof(line2));

        var mrz = new MrzInfo
        {
            Format = "TD2",
            RawLines = new[] { line1, line2 },
            DocumentType = line1.Substring(0, 2).TrimEnd('<'),
            IssuingCountry = line1.Substring(2, 3).Replace('<', ' ').Trim()
        };

        // Line 1: Names
        ParseNames(line1.Substring(5, 31), mrz);

        // Line 2: Doc Number, check, Nationality, DOB, check, Sex, Expiry, check, Optional, composite
        mrz.DocumentNumber = line2.Substring(0, 9).TrimEnd('<');
        char docCheck = line2[9];
        mrz.Nationality = line2.Substring(10, 3).Replace('<', ' ').Trim();
        mrz.DateOfBirth = line2.Substring(13, 6);
        char dobCheck = line2[19];
        mrz.Sex = line2.Substring(20, 1);
        mrz.DateOfExpiry = line2.Substring(21, 6);
        char expiryCheck = line2[27];
        mrz.OptionalData = line2.Substring(28, 7).TrimEnd('<');
        char compositeCheck = line2[35];

        // Verification
        mrz.DocumentNumberCheckValid = VerifyCheckDigit(mrz.DocumentNumber, docCheck);
        mrz.DateOfBirthCheckValid = VerifyCheckDigit(mrz.DateOfBirth, dobCheck);
        mrz.DateOfExpiryCheckValid = VerifyCheckDigit(mrz.DateOfExpiry, expiryCheck);

        // TD2 composite check digit covers line2[0..9] + line2[13..19] + line2[21..34]
        string compositePayload = line2.Substring(0, 10) + line2.Substring(13, 7) + line2.Substring(21, 14);
        mrz.CompositeCheckValid = VerifyCheckDigit(compositePayload, compositeCheck);

        mrz.IsChecksumValid = mrz.DocumentNumberCheckValid &&
                              mrz.DateOfBirthCheckValid &&
                              mrz.DateOfExpiryCheckValid &&
                              mrz.CompositeCheckValid;

        CollectValidationErrors(mrz);
        return mrz;
    }

    /// <summary>
    /// Parses a 2-line TD3 (ePassport standard: 2x44 characters) MRZ block.
    /// </summary>
    public static MrzInfo ParseTd3(string line1, string line2)
    {
        if (string.IsNullOrWhiteSpace(line1) || line1.Length < 44)
            throw new ArgumentException("Line 1 of TD3 MRZ must be at least 44 characters.", nameof(line1));
        if (string.IsNullOrWhiteSpace(line2) || line2.Length < 44)
            throw new ArgumentException("Line 2 of TD3 MRZ must be at least 44 characters.", nameof(line2));

        var mrz = new MrzInfo
        {
            Format = "TD3",
            RawLines = new[] { line1, line2 },
            DocumentType = line1.Substring(0, 2).TrimEnd('<'),
            IssuingCountry = line1.Substring(2, 3).Replace('<', ' ').Trim()
        };

        // Line 1: Names (Surname << GivenNames)
        ParseNames(line1.Substring(5, 39), mrz);

        // Line 2: Document Number, Check Digit, Nationality, DOB, Sex, Expiry, Optional, Composite
        mrz.DocumentNumber = line2.Substring(0, 9).TrimEnd('<');
        char docNumCheck = line2[9];
        mrz.Nationality = line2.Substring(10, 3).Replace('<', ' ').Trim();
        mrz.DateOfBirth = line2.Substring(13, 6);
        char dobCheck = line2[19];
        mrz.Sex = line2.Substring(20, 1);
        mrz.DateOfExpiry = line2.Substring(21, 6);
        char expiryCheck = line2[27];
        mrz.OptionalData = line2.Substring(28, 14).TrimEnd('<');
        char optionalCheck = line2[42];
        char compositeCheck = line2[43];

        // Verification (7-3-1 weight Modulo 10)
        mrz.DocumentNumberCheckValid = VerifyCheckDigit(mrz.DocumentNumber, docNumCheck);
        mrz.DateOfBirthCheckValid = VerifyCheckDigit(mrz.DateOfBirth, dobCheck);
        mrz.DateOfExpiryCheckValid = VerifyCheckDigit(mrz.DateOfExpiry, expiryCheck);

        // TD3 composite check digit covers line2[0..9] + line2[13..19] + line2[21..42]
        string compositePayload = line2.Substring(0, 10) + line2.Substring(13, 7) + line2.Substring(21, 22);
        mrz.CompositeCheckValid = VerifyCheckDigit(compositePayload, compositeCheck);

        mrz.IsChecksumValid = mrz.DocumentNumberCheckValid &&
                              mrz.DateOfBirthCheckValid &&
                              mrz.DateOfExpiryCheckValid &&
                              mrz.CompositeCheckValid;

        CollectValidationErrors(mrz);
        return mrz;
    }

    private static void ParseNames(string namePart, MrzInfo mrz)
    {
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
    }

    private static void CollectValidationErrors(MrzInfo mrz)
    {
        if (!mrz.DocumentNumberCheckValid)
            mrz.ValidationErrors.Add("Document number check digit mismatch.");
        if (!mrz.DateOfBirthCheckValid)
            mrz.ValidationErrors.Add("Date of birth check digit mismatch.");
        if (!mrz.DateOfExpiryCheckValid)
            mrz.ValidationErrors.Add("Date of expiry check digit mismatch.");
        if (!mrz.CompositeCheckValid)
            mrz.ValidationErrors.Add("Composite check digit mismatch.");
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
