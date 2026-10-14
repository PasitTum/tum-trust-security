// Copyright (c) 2026 TUM Trust Security. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tum.Trust.Security.Core.Interfaces.Logging;
using Tum.Trust.Security.Core.Models.Logging;

namespace Tum.Trust.Security.Infrastructure.Logging
{
    /// <summary>
    /// Sovereign logging and compliance engine coordinating AI PII sanitization,
    /// real-time threat detection, and immutable SHA-256 Merkle chain persistence.
    /// </summary>
    public class TumLogEngine : ITumLogEngine
    {
        private readonly ITumPiiSanitizer _piiSanitizer;
        private readonly ITumThreatDetector _threatDetector;
        private readonly ITamperProofLogStorage _storage;
        private readonly TumLogOptions _options;
        private readonly ILogger<TumLogEngine> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TumLogEngine"/> class.
        /// </summary>
        /// <param name="piiSanitizer">The PII sanitizer component.</param>
        /// <param name="threatDetector">The threat detector component.</param>
        /// <param name="storage">The tamper-proof storage component.</param>
        /// <param name="options">Configuration options.</param>
        /// <param name="logger">Optional logger instance.</param>
        public TumLogEngine(
            ITumPiiSanitizer? piiSanitizer = null,
            ITumThreatDetector? threatDetector = null,
            ITamperProofLogStorage? storage = null,
            TumLogOptions? options = null,
            ILogger<TumLogEngine>? logger = null)
        {
            _options = options ?? new TumLogOptions();
            _piiSanitizer = piiSanitizer ?? new TumPiiSanitizer();
            _threatDetector = threatDetector ?? new TumThreatDetector();
            _storage = storage ?? new TamperProofLogChain(_options);
            _logger = logger ?? NullLogger<TumLogEngine>.Instance;
        }

        /// <inheritdoc />
        public async Task<TumLogEntry> LogAsync(TumLogEntry entry, CancellationToken cancellationToken = default)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            // 1. Auto PII Sanitization
            if (_options.EnableAutoPiiMasking && !string.IsNullOrEmpty(entry.PayloadSanitized))
            {
                var (sanitized, detections) = _piiSanitizer.SanitizeText(entry.PayloadSanitized);
                entry.PayloadSanitized = sanitized;
                entry.PiiRedactedCount = detections.Count;
                entry.RedactedDetails = detections;
            }

            // 2. Real-Time Threat Inspection
            if (_options.EnableThreatDetection)
            {
                var threats = _threatDetector.InspectRequest(
                    entry.RequestPath,
                    entry.HttpMethod,
                    queryString: null,
                    entry.PayloadSanitized,
                    entry.ClientIp);

                if (threats.Count > 0)
                {
                    entry.SecurityThreats.AddRange(threats);
                    entry.Category = LogCategory.SecurityThreat;
                    entry.Severity = "Critical";

                    _logger.LogWarning("Security Threat detected in request from IP {ClientIp}: {ThreatCount} vector(s)",
                        entry.ClientIp, threats.Count);
                }
            }

            // 3. Stamp Application Metadata
            if (string.IsNullOrEmpty(entry.ApplicationName))
            {
                entry.ApplicationName = _options.ApplicationName;
            }

            if (string.IsNullOrEmpty(entry.Environment))
            {
                entry.Environment = _options.Environment;
            }

            // 4. Append to Immutable WORM Merkle Chain
            var receipt = await _storage.AppendEntryAsync(entry, cancellationToken).ConfigureAwait(false);
            entry.TamperProofHash = receipt.EntryHashSha256;
            entry.PreviousEntryHash = receipt.PreviousEntryHashSha256;
            entry.MerkleRoot = receipt.MerkleRootSha256;

            return entry;
        }

        /// <inheritdoc />
        public async Task<TumLogEntry> LogEventAsync(
            string eventName,
            LogCategory category,
            string payload,
            string? userId = null,
            string? clientIp = null,
            string severity = "Information",
            CancellationToken cancellationToken = default)
        {
            var entry = new TumLogEntry
            {
                EventName = eventName,
                Category = category,
                PayloadSanitized = payload ?? string.Empty,
                UserId = userId,
                ClientIp = clientIp ?? "127.0.0.1",
                Severity = severity,
                TimestampUtc = DateTimeOffset.UtcNow
            };

            return await LogAsync(entry, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<bool> VerifyIntegrityAsync(CancellationToken cancellationToken = default)
        {
            return _storage.VerifyChainIntegrityAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<TumLogEntry>> GetLogsAsync(
            int count = 100,
            LogCategory? filterCategory = null,
            CancellationToken cancellationToken = default)
        {
            var list = await _storage.GetRecentEntriesAsync(count, cancellationToken).ConfigureAwait(false);
            if (!filterCategory.HasValue)
            {
                return list;
            }

            var filtered = new List<TumLogEntry>();
            foreach (var item in list)
            {
                if (item.Category == filterCategory.Value)
                {
                    filtered.Add(item);
                }
            }
            return filtered.AsReadOnly();
        }
    }
}
