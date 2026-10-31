namespace Tum.Trust.Security.Core.Models.Passport;

/// <summary>
/// Parsed Machine Readable Zone (MRZ) details conforming to ICAO Doc 9303 (TD1, TD2, TD3).
/// </summary>
public class MrzInfo
{
    /// <summary>
    /// Document type identifier (e.g., "P" for Passport, "I" or "ID" for National ID, "V" for Visa).
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
    /// The document number (passport or ID number).
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
    /// Detected ICAO 9303 layout format ("TD1", "TD2", or "TD3").
    /// </summary>
    public string Format { get; set; } = "TD3";

    /// <summary>
    /// Indicates whether the document number check digit passed validation.
    /// </summary>
    public bool DocumentNumberCheckValid { get; set; }

    /// <summary>
    /// Indicates whether the date of birth check digit passed validation.
    /// </summary>
    public bool DateOfBirthCheckValid { get; set; }

    /// <summary>
    /// Indicates whether the date of expiry check digit passed validation.
    /// </summary>
    public bool DateOfExpiryCheckValid { get; set; }

    /// <summary>
    /// Indicates whether the composite overall check digit passed validation.
    /// </summary>
    public bool CompositeCheckValid { get; set; }

    /// <summary>
    /// Indicates whether all ICAO 9303 Modulo 10 check digits passed verification.
    /// </summary>
    public bool IsChecksumValid { get; set; }

    /// <summary>
    /// Validation warnings or errors encountered during MRZ parsing.
    /// </summary>
    public IList<string> ValidationErrors { get; set; } = new List<string>();

    /// <summary>
    /// Raw unparsed MRZ lines (2 lines for TD3/TD2, 3 lines for TD1).
    /// </summary>
    public string[] RawLines { get; set; } = Array.Empty<string>();
}
