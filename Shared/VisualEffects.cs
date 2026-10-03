namespace DensityWaveTheory.Shared;

/// <summary>Toggleable look options. Defaults match the beltoforion reference look.</summary>
public sealed class VisualEffects
{
    /// <summary>Optional wash over blackbody colors. Off by default.</summary>
    public bool RadialPalette { get; set; }

    /// <summary>Optional Gaussian dust. Off by default; linear falloff matches reference.</summary>
    public bool SoftGlow { get; set; }

    public float PaletteStrength { get; set; } = 0.25f;

    /// <summary>Scales dust sprite size when soft glow is on.</summary>
    public float GlowStrength { get; set; } = 1.0f;
}
