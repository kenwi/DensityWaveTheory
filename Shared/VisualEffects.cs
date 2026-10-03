namespace DensityWaveTheory.Shared;

/// <summary>Toggleable look options. Soft glow follows beltoforion: dust nebula, sharp stars.</summary>
public sealed class VisualEffects
{
    /// <summary>Optional wash over blackbody colors. Off by default so dust temperature drives the look.</summary>
    public bool RadialPalette { get; set; }

    /// <summary>Widen/soften dust (and filaments) into a nebula. Stars stay small and sharp.</summary>
    public bool SoftGlow { get; set; } = true;

    public float PaletteStrength { get; set; } = 0.25f;

    /// <summary>Scales dust sprite size when soft glow is on.</summary>
    public float GlowStrength { get; set; } = 1.15f;
}
