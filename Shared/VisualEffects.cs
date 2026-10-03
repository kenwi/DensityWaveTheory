namespace DensityWaveTheory.Shared;

/// <summary>Toggleable post-style look options for the galaxy render.</summary>
public sealed class VisualEffects
{
    /// <summary>Warm core to cool outer-disk palette mixed over blackbody colors.</summary>
    public bool RadialPalette { get; set; } = true;

    /// <summary>Gaussian soft falloff + wide bloom halo pass.</summary>
    public bool SoftGlow { get; set; } = true;

    public float PaletteStrength { get; set; } = 0.45f;

    /// <summary>Scales bloom size/intensity when soft glow is on (1 = default strong glow).</summary>
    public float GlowStrength { get; set; } = 1.35f;
}
