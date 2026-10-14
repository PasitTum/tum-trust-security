namespace Netka.Identity.Core.Models.Biometrics;

/// <summary>
/// Categories of Presentation Attacks defined by ISO/IEC 30107 (Presentation Attack Detection - PAD).
/// </summary>
public enum PresentationAttackType
{
    /// <summary>
    /// Genuine living human subject with natural micro-vascular and specular responses.
    /// </summary>
    None = 0,

    /// <summary>
    /// Level 1: 2D static printed photograph (paper, photo album, laser print).
    /// </summary>
    PrintedPhoto = 1,

    /// <summary>
    /// Level 1: 2D digital screen replay (iPad, smartphone, high-refresh-rate monitor).
    /// </summary>
    ScreenReplay = 2,

    /// <summary>
    /// Level 2: 3D biometric spoof (latex mask, silicone prosthetic, mannequin head).
    /// </summary>
    SiliconeMask = 3,

    /// <summary>
    /// Level 3: AI-generated synthetic face, diffusion-based deepfake, or real-time neural face swap.
    /// </summary>
    DeepfakeSynthetic = 4
}
