namespace Tum.Trust.Security.Core.Models.Biometrics;

/// <summary>
/// Outcome of a 1:1 facial biometric feature comparison between a live probe image and an official reference photo.
/// </summary>
public class FaceMatchResult
{
    /// <summary>
    /// Indicates whether the two facial images belong to the same identity based on the operational threshold.
    /// </summary>
    public bool IsMatch { get; set; }

    /// <summary>
    /// Metric cosine similarity score between facial embedding vectors (0.00 to 1.00).
    /// </summary>
    public double SimilarityScore { get; set; }

    /// <summary>
    /// Operational confidence level ("Low", "Medium", "High", "MilitaryGrade").
    /// </summary>
    public string ConfidenceLevel { get; set; } = "High";

    /// <summary>
    /// Dimension of the extracted neural feature embedding vector (e.g., 512-D).
    /// </summary>
    public int EmbeddingDimension { get; set; } = 512;

    /// <summary>
    /// Match latency in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; set; }
}
