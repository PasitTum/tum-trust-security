namespace Tum.Trust.Security.Core.Models.Passport;

/// <summary>
/// Parsed Machine Readable Zone (MRZ) details conforming to ICAO Doc 9303 (TD1, TD2, TD3).
/// </summary>
public class MrzInfo
{
    /// <summary>
    /// Document type identifier (e.g., "P" for Passport, "ID" for National ID).
    /// </summary>
    public string DocumentType { get; set; } = "P";

    /// <summary>
    /// Three-letter issuing country or organization code according to ISO 3166-1 alpha-3 (e.g., "USA", "GBR", "DEU", "JPN", "THA").
    /// </summary>
    public string IssuingCountry { get; set; } = string.Empty;

    /// <summary>
    /// Primary identifier / Surname of the passport holder.
    /// </summary>
    public string Surname { get; set; } = string.Empty;

    /// <summary>
    /// Secondary identifiers / Given names.
    /// </summary>
    public string GivenNames { get; set; } = string.Empty;

    /// <summary>
    /// The document number (passport number).
    /// </summary>
    public string DocumentNumber { get; set; } = string.Empty;

    /// <summary>
    /// Three-letter nationality code (ISO 3166-1 alpha-3).
    /// </summary>
    public string Nationality { get; set; } = string.Empty;

    /// <summary>
    /// Date of birth formatted as (YYMMDD).
    /// </summary>
    public string DateOfBirth { get; set; } = string.Empty;

    /// <summary>
    /// Sex of the document holder ("M", "F", or "X").
    /// </summary>
    public string Sex { get; set; } = "X";

    /// <summary>
    /// Date of expiry formatted as (YYMMDD).
    /// </summary>
    public string DateOfExpiry { get; set; } = string.Empty;

    /// <summary>
    /// Optional personal number or national registration number.
    /// </summary>
    public string? OptionalData { get; set; }

    /// <summary>
    /// Indicates whether all ICAO 9303 Modulo 10 check digits passed verification.
    /// </summary>
    public bool IsChecksumValid { get; set; }

    /// <summary>
    /// Raw unparsed MRZ lines (2 lines for TD3, 3 lines for TD1).
    /// </summary>
    public string[] RawLines { get; set; } = Array.Empty<string>();
}
