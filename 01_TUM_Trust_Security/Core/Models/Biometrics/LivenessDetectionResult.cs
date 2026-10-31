namespace Tum.Trust.Security.Core.Models.Biometrics;

/// <summary>
/// Outcome of an edge AI Presentation Attack Detection (PAD) analysis conforming to ISO/IEC 30107.
/// </summary>
public class LivenessDetectionResult
{
    /// <summary>
    /// Indicates whether the subject is verified as an authentic, living human.
    /// </summary>
    public bool IsLiveHuman { get; set; }

    /// <summary>
    /// Confidence probability of biological life (0.00 to 1.00).
    /// </summary>
    public double LivenessScore { get; set; }

    /// <summary>
    /// The detected presentation attack type (if any).
    /// </summary>
    public PresentationAttackType AttackType { get; set; } = PresentationAttackType.None;

    /// <summary>
    /// Human-readable diagnosis or warning description.
    /// </summary>
    public string DiagnosticMessage { get; set; } = string.Empty;

    /// <summary>
    /// Edge inference execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Sub-dermal micro-vascular pulse frequency estimate (BPM) derived via Remote-PPG, if available.
    /// </summary>
    public double? EstimatedHeartRateBpm { get; set; }
}
