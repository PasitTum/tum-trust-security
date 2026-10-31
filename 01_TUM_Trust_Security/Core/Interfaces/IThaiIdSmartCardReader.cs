using System;
using System.Threading;
using System.Threading.Tasks;
using Tum.Trust.Security.Core.Models.Hardware;

namespace Tum.Trust.Security.Core.Interfaces;

/// <summary>
/// Hardware contract for interacting with physical Thai National Smart Cards via PC/SC Readers.
/// Conforms to Ministry of Interior (MoI) / Department of Provincial Administration (DOPA) smartcard specifications.
/// </summary>
public interface IThaiIdSmartCardReader
{
    /// <summary>
    /// Event raised when a Thai ID card is detected and read.
    /// </summary>
    event EventHandler<ThaiIdCardData>? OnThaiIdCardRead;

    /// <summary>
    /// Reads and parses personal demographics and citizen ID from a Thai ID smartcard.
    /// </summary>
    /// <param name="readerName">Name of the target PC/SC smartcard reader device.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Extracted and verified <see cref="ThaiIdCardData"/>.</returns>
    Task<ThaiIdCardData> ReadThaiIdCardAsync(string readerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads and parses personal demographics, citizen ID, and optionally facial photograph from a Thai ID smartcard.
    /// </summary>
    /// <param name="readerName">Name of the target PC/SC smartcard reader device.</param>
    /// <param name="includePhoto">Whether to read the facial photograph data array.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Extracted and verified <see cref="ThaiIdCardData"/>.</returns>
    Task<ThaiIdCardData> ReadThaiIdCardAsync(string readerName, bool includePhoto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether an authentic Thai National ID smartcard is currently seated in the reader.
    /// </summary>
    /// <param name="readerName">Name of the target PC/SC smartcard reader device.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a card matching the MoI smartcard AID is detected; otherwise, false.</returns>
    Task<bool> IsThaiIdCardPresentAsync(string readerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the raw facial photograph bytes from the Thai ID smart card.
    /// </summary>
    /// <param name="readerName">Name of the target PC/SC smartcard reader device.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JPEG photo byte array if present; otherwise, null.</returns>
    Task<byte[]?> ReadPhotoAsync(string readerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously waits for a Thai ID smart card to be inserted into the reader, reads it, and returns its data.
    /// </summary>
    /// <param name="readerName">Name of the target PC/SC smartcard reader device.</param>
    /// <param name="timeout">Optional timeout duration. If null, waits until cancelled.</param>
    /// <param name="includePhoto">Whether to read the photo byte array.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Extracted and verified <see cref="ThaiIdCardData"/>.</returns>
    Task<ThaiIdCardData> WaitForCardAndReadAsync(string readerName, TimeSpan? timeout = null, bool includePhoto = false, CancellationToken cancellationToken = default);
}
