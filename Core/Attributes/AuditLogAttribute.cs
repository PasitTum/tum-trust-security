namespace Netka.Identity.Core.Attributes;

/// <summary>
/// Marks a method or property as generating audit log entries.
/// Used by frameworks to intercept and log high-level business actions with diff tracking.
/// Part of Tri-Log Strategy: Activity Log (metrics) + Audit Log (business actions) + PII Log (encrypted vault).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class AuditLogAttribute : Attribute
{
    /// <summary>
    /// Initializes a new AuditLogAttribute.
    /// </summary>
    /// <param name="action">The business action being logged (e.g., "IdentityVerification", "TokenExchange").</param>
    /// <param name="target">The target entity or provider (e.g., "ThaID", "Google", "User").</param>
    public AuditLogAttribute(string action, string target)
    {
        Action = action;
        Target = target;
    }

    /// <summary>
    /// The business action being logged.
    /// </summary>
    public string Action { get; set; }

    /// <summary>
    /// The entity or provider this action targets.
    /// </summary>
    public string Target { get; set; }

    /// <summary>
    /// Optional description of what this audit log entry tracks.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether to track data mutations (diff) for this audit entry.
    /// </summary>
    public bool TrackDiff { get; set; } = true;
}
