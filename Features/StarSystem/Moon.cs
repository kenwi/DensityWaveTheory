using Raylib_cs;

namespace DensityWaveTheory.Features.StarSystem;

public sealed class Moon
{
    public required string Name { get; init; }
    /// <summary>Orbital distance from planet in AU (physical-ish, usually tiny).</summary>
    public float SemiMajorAu { get; init; }
    /// <summary>Draw distance from planet in AU (exaggerated so moons read at system FOV).</summary>
    public float VisualOrbitAu { get; init; }
    public float PeriodYears { get; init; }
    public float RadiusEarth { get; init; }
    public float PhaseRadians { get; init; }
    public Color Color { get; init; }
    public BodySurface? Surface { get; set; }
}
