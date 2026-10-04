using Raylib_cs;

namespace DensityWaveTheory.Features.StarSystem;

public enum PlanetType
{
    HotRocky,
    Rocky,
    Temperate,
    Ice,
    GasGiant,
}

public sealed class Planet
{
    public required string Name { get; init; }
    public required PlanetType Type { get; init; }
    public float SemiMajorAu { get; init; }
    public float Eccentricity { get; init; }
    public float PeriodYears { get; init; }
    public float RadiusEarth { get; init; }
    public float PhaseRadians { get; init; }
    public Color Color { get; init; }
    public bool InHabitableZone { get; init; }
}
