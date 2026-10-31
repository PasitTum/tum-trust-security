using System;
using System.Linq;
using Tum.Trust.Security.Infrastructure.Helpers;

namespace Tum.Trust.Security.Core.Models.Hardware;

/// <summary>
/// Data payload read from a physical Thai National Smart Card (DOPA / MoI applet).
/// Conforms to ISO/IEC 7816-4 and Ministry of Interior specifications.
/// </summary>
public class ThaiIdCardData
{
    /// <summary>
    /// 13-digit Thai Citizen Identification Number (PID).
    /// </summary>
    public string CitizenId { get; set; } = string.Empty;

    /// <summary>
    /// Title in Thai (e.g., นาย, นาง, นางสาว).
    /// </summary>
    public string TitleTh { get; set; } = string.Empty;

    /// <summary>
    /// First name in Thai.
    /// </summary>
    public string FirstNameTh { get; set; } = string.Empty;

    /// <summary>
    /// Middle name in Thai if present.
    /// </summary>
    public string MiddleNameTh { get; set; } = string.Empty;

    /// <summary>
    /// Last name in Thai.
    /// </summary>
    public string LastNameTh { get; set; } = string.Empty;

    /// <summary>
    /// Gets the full formatted Thai name (e.g., "นาย สมชาย ใจดี").
    /// </summary>
    public string FullNameTh => string.Join(" ", new[] { TitleTh, FirstNameTh, MiddleNameTh, LastNameTh }.Where(s => !string.IsNullOrWhiteSpace(s)));

    /// <summary>
    /// Title in English (e.g., Mr., Mrs., Miss).
    /// </summary>
    public string TitleEn { get; set; } = string.Empty;

    /// <summary>
    /// First name in English.
    /// </summary>
    public string FirstNameEn { get; set; } = string.Empty;

    /// <summary>
    /// Middle name in English if present.
    /// </summary>
    public string MiddleNameEn { get; set; } = string.Empty;

    /// <summary>
    /// Last name in English.
    /// </summary>
    public string LastNameEn { get; set; } = string.Empty;

    /// <summary>
    /// Gets the full formatted English name (e.g., "Mr. Somchai Jaidee").
    /// </summary>
    public string FullNameEn => string.Join(" ", new[] { TitleEn, FirstNameEn, MiddleNameEn, LastNameEn }.Where(s => !string.IsNullOrWhiteSpace(s)));

    /// <summary>
    /// Formatted masked citizen ID for display without leaking PII (e.g., "1********2345").
    /// </summary>
    public string MaskedCitizenId => StringMaskingHelper.MaskThaiNationalId(CitizenId);

    /// <summary>
    /// Date of birth in Buddhist Era (YYYYMMDD).
    /// </summary>
    public string DateOfBirth { get; set; } = string.Empty;

    /// <summary>
    /// Gender (1 = Male, 2 = Female).
    /// </summary>
    public string Gender { get; set; } = string.Empty;

    /// <summary>
    /// Full structured residential address.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Card issue date (YYYYMMDD).
    /// </summary>
    public string IssueDate { get; set; } = string.Empty;

    /// <summary>
    /// Card expiration date (YYYYMMDD).
    /// </summary>
    public string ExpirationDate { get; set; } = string.Empty;

    /// <summary>
    /// Issuer office code or location name.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Raw facial photo bytes extracted from the chip.
    /// </summary>
    public byte[]? PhotoBytes { get; set; }

    /// <summary>
    /// ATR (Answer To Reset) bytes returned during card initialization.
    /// </summary>
    public byte[]? AtrBytes { get; set; }
}
