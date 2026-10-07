namespace DensityWaveTheory.Features.StarSystem;

/// <summary>Moon-centric view: one regenerated moon, parent system/planet not retained.</summary>
public sealed class MoonFocus
{
    public required Moon Moon { get; init; }
    public required string ParentName { get; init; }
    public required PlanetType ParentType { get; init; }
    public required float ParentSemiMajorAu { get; init; }
    public required float ParentRadiusEarth { get; init; }
    public required StellarModel HostStar { get; init; }
    public required int MoonIndex { get; init; }
    public required int SystemSeed { get; init; }
    public required PlanetResume Resume { get; init; }
}
