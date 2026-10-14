using System.Text;
using Tum.Trust.Security.Core.Models.Biometrics;
using Tum.Trust.Security.Core.Models.Passport;
using Tum.Trust.Security.Infrastructure.Biometrics;
using Tum.Trust.Security.Infrastructure.Passport;
using Xunit;

namespace Tum.Trust.Security.Tests;

public class BiometricLivenessTests
{
    private readonly EdgeFaceLivenessDetector _livenessDetector = new();
    private readonly EdgeBiometricFaceMatcher _faceMatcher = new();

    [Fact]
    public async Task LivenessDetector_DetectsGenuineLiveHuman_Successfully()
    {
        byte[] liveFrame = Encoding.UTF8.GetBytes("GENUINE_LIVE_CAMERA_FRAME_DATA");

        var result = await _livenessDetector.DetectLivenessAsync(liveFrame);

        Assert.True(result.IsLiveHuman);
        Assert.True(result.LivenessScore >= 0.85);
        Assert.Equal(PresentationAttackType.None, result.AttackType);
        Assert.NotNull(result.EstimatedHeartRateBpm);
        Assert.True(result.ExecutionTimeMs < 80, $"Latency {result.ExecutionTimeMs}ms exceeded 80ms budget.");
    }

    [Theory]
    [InlineData("PRINT_ATTACK_SAMPLE", PresentationAttackType.PrintedPhoto)]
    [InlineData("SCREEN_REPLAY_SAMPLE", PresentationAttackType.ScreenReplay)]
    [InlineData("SILICONE_MASK_SAMPLE", PresentationAttackType.SiliconeMask)]
    [InlineData("DEEPFAKE_NEURAL_SAMPLE", PresentationAttackType.DeepfakeSynthetic)]
    public async Task LivenessDetector_DetectsAndBlocksPresentationAttacks_Accurately(string marker, PresentationAttackType expectedAttack)
    {
        byte[] spoofFrame = Encoding.UTF8.GetBytes(marker);

        var result = await _livenessDetector.DetectLivenessAsync(spoofFrame);

        Assert.False(result.IsLiveHuman);
        Assert.True(result.LivenessScore < 0.85);
        Assert.Equal(expectedAttack, result.AttackType);
    }

    [Fact]
    public async Task FaceMatcher_MatchesIdenticalFaces_WithHighConfidence()
    {
        byte[] probeFace = Encoding.UTF8.GetBytes("PROBE_FACE_PHOTO_BUFFER");
        byte[] referencePhoto = Encoding.UTF8.GetBytes("DG2_REFERENCE_PHOTO_BUFFER");

        var result = await _faceMatcher.MatchFacesAsync(probeFace, referencePhoto);

        Assert.True(result.IsMatch);
        Assert.True(result.SimilarityScore >= 0.72);
        Assert.Equal("MilitaryGrade", result.ConfidenceLevel);
        Assert.Equal(512, result.EmbeddingDimension);
    }

    [Fact]
    public async Task FaceMatcher_RejectsMismatchedFaces_Accurately()
    {
        byte[] probeFace = Encoding.UTF8.GetBytes("MISMATCH_FACE_PROBE");
        byte[] referencePhoto = Encoding.UTF8.GetBytes("DG2_REFERENCE_PHOTO_BUFFER");

        var result = await _faceMatcher.MatchFacesAsync(probeFace, referencePhoto);

        Assert.False(result.IsMatch);
        Assert.True(result.SimilarityScore < 0.72);
    }

    [Fact]
    public async Task BorderlessEngine_BlocksPassportVerification_WhenDeepfakeDetected()
    {
        var provider = new MockBorderlessTrustProvider(_livenessDetector, _faceMatcher);

        var request = new PassportVerificationRequest
        {
            Mrz = new MrzInfo
            {
                IssuingCountry = "USA",
                DocumentNumber = "E99988877",
                Surname = "SMITH",
                GivenNames = "JOHN"
            },
            Dg2FacePhotoBytes = Encoding.UTF8.GetBytes("AUTHENTIC_DG2_PHOTO")
        };

        // Live camera sends a synthetic deepfake stream
        byte[] deepfakeCameraFrame = Encoding.UTF8.GetBytes("DEEPFAKE_SYNTHETIC_ATTACK_STREAM");

        var result = await provider.VerifyPassportAsync(request, deepfakeCameraFrame);

        Assert.False(result.IsSuccess);
        Assert.False(result.IsLiveHuman);
        Assert.Equal("DeepfakeSynthetic", result.Data["attack_type"]);
    }
}
