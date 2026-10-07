namespace DensityWaveTheory.Features.StarSystem;

/// <summary>Planet-centric view: one regenerated planet + moons, no sibling planets.</summary>
public sealed class PlanetFocus
{
    public required Planet Planet { get; init; }
    public required StellarModel HostStar { get; init; }
    public required HabitableZone HabitableZone { get; init; }
    public required int PlanetIndex { get; init; }
    public required int SystemSeed { get; init; }
    public required StarSystemResume Resume { get; init; }
}
