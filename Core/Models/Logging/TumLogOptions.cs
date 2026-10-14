// Copyright (c) 2026 TUM Trust Security. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Tum.Trust.Security.Core.Models.Logging
{
    /// <summary>
    /// Configuration options for the TUMLog Sovereign Application-Layer Logging and Compliance Engine.
    /// </summary>
    public class TumLogOptions
    {
        /// <summary>
        /// Gets or sets the application name stamped on all generated log entries.
        /// </summary>
        public string ApplicationName { get; set; } = "TUM-Application";

        /// <summary>
        /// Gets or sets the deployment environment name (e.g. 'Production', 'Staging', 'Development').
        /// </summary>
        public string Environment { get; set; } = "Production";

        /// <summary>
        /// Gets or sets a value indicating whether automatic PII detection and redaction is enabled.
        /// </summary>
        public bool EnableAutoPiiMasking { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether web application threat detection (SQLi, XSS, Path Traversal) is enabled.
        /// </summary>
        public bool EnableThreatDetection { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether cryptographic SHA-256 Merkle chaining is enabled.
        /// </summary>
        public bool EnableMerkleChaining { get; set; } = true;

        /// <summary>
        /// Gets or sets the WORM (Write Once Read Many) retention policy in days (default: 90 days as mandated by Thai Computer Crime Act Sec 26).
        /// </summary>
        public int WormRetentionDays { get; set; } = 90;

        /// <summary>
        /// Gets or sets the primary Stratum 1 NTP server host for time synchronization (default: 'time.navy.mi.th' / 'time.nist.gov').
        /// </summary>
        public string NtpServerHost { get; set; } = "time.navy.mi.th";

        /// <summary>
        /// Gets or sets the local or mounted directory path where immutable log files are securely appended.
        /// </summary>
        public string StorageDirectory { get; set; } = "./tum_logs";

        /// <summary>
        /// Gets or sets the capacity of the high-speed lock-free ring buffer channel.
        /// </summary>
        public int ChannelBufferCapacity { get; set; } = 50000;
    }
}
