using DensityWaveTheory.Shared;

namespace DensityWaveTheory.Features.StarSystem;

public static class MoonFocusGenerator
{
    /// <summary>
    /// Rebuild a single moon for close-up at high surface detail.
    /// Parent planet / sibling moons are not kept in memory.
    /// </summary>
    public static MoonFocus Build(PlanetFocus planetFocus, int moonIndex)
    {
        var parent = planetFocus.Planet;
        if (moonIndex < 0 || moonIndex >= parent.Moons.Count)
            throw new ArgumentOutOfRangeException(nameof(moonIndex));

        var source = parent.Moons[moonIndex];
        var moon = new Moon
        {
            Name = source.Name,
            SemiMajorAu = source.SemiMajorAu,
            VisualOrbitAu = source.VisualOrbitAu,
            PeriodYears = source.PeriodYears,
            RadiusEarth = source.RadiusEarth,
            PhaseRadians = source.PhaseRadians,
            Color = source.Color,
        };

        var seed = ProceduralSeed.Xor(
            ProceduralSeed.Xor(
                ProceduralSeed.Mix(
                    planetFocus.SystemSeed,
                    (planetFocus.PlanetIndex + 3) * ProceduralSeed.IndexPrime),
                (moonIndex + 1) * ProceduralSeed.SiblingPrime),
            ProceduralSeed.MoonTag);
        moon.Surface = SurfaceGenerator.CreateMoon(seed, moon.Color, 224);

        return new MoonFocus
        {
            Moon = moon,
            ParentName = parent.Name,
            ParentType = parent.Type,
            ParentSemiMajorAu = parent.SemiMajorAu,
            ParentRadiusEarth = parent.RadiusEarth,
            HostStar = planetFocus.HostStar,
            MoonIndex = moonIndex,
            SystemSeed = planetFocus.SystemSeed,
            Resume = new PlanetResume(planetFocus.Resume, planetFocus.PlanetIndex),
        };
    }
}
