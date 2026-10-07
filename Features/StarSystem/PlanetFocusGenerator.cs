namespace DensityWaveTheory.Features.StarSystem;

public static class PlanetFocusGenerator
{
    /// <summary>
    /// Rebuild a single planet for close-up: same orbital identity, moons regenerated
    /// at planet-local visual scale (sibling planets discarded).
    /// </summary>
    public static PlanetFocus Build(
        PlanetarySystem system,
        int planetIndex,
        StarSystemResume resume)
    {
        if (planetIndex < 0 || planetIndex >= system.Planets.Count)
            throw new ArgumentOutOfRangeException(nameof(planetIndex));

        var source = system.Planets[planetIndex];
        // Same visual radius formula as PlanetScene so moon rings match the disc.
        var visualRadius = PlanetScene.VisualPlanetRadius(source.RadiusEarth);
        var moonSeed = unchecked(system.Seed * 397 ^ (planetIndex + 17) ^ 0x504C4E54);
        var rng = new Random(moonSeed);
        var moons = MoonGenerator.GenerateFocus(
            rng,
            system.Star,
            source.Type,
            source.SemiMajorAu,
            source.RadiusEarth,
            visualRadius,
            source.Name,
            source.Moons.Count);

        var planet = new Planet
        {
            Name = source.Name,
            Type = source.Type,
            SemiMajorAu = source.SemiMajorAu,
            Eccentricity = source.Eccentricity,
            PeriodYears = source.PeriodYears,
            RadiusEarth = source.RadiusEarth,
            PhaseRadians = source.PhaseRadians,
            Color = source.Color,
            InHabitableZone = source.InHabitableZone,
            Moons = moons,
        };

        return new PlanetFocus
        {
            Planet = planet,
            HostStar = system.Star,
            HabitableZone = system.HabitableZone,
            PlanetIndex = planetIndex,
            SystemSeed = system.Seed,
            Resume = resume,
        };
    }
}
