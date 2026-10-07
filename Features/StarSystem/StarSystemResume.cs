using System.Numerics;

namespace DensityWaveTheory.Features.StarSystem;

/// <summary>Enough identity to regenerate a star system after leaving planet focus.</summary>
public readonly record struct StarSystemResume(
    int Seed,
    int StarIndex,
    float TempKelvin,
    Vector2 StarWorldPos,
    float GalaxyFov,
    Vector2 GalaxyTarget);
