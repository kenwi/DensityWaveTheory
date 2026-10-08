using DensityWaveTheory.Features.GalaxyPopulation;
using Raylib_cs;

namespace DensityWaveTheory.Features.StarSystem;

public sealed class PlanetarySystem
{
    public required StellarModel Star { get; init; }
    public required HabitableZone HabitableZone { get; init; }
    public required IReadOnlyList<Planet> Planets { get; init; }
    public required int SourceStarIndex { get; init; }
    public required int Seed { get; init; }
}

public static class PlanetarySystemGenerator
{
    public static int MakeSeed(uint galaxySeed, int starIndex, float a, float theta0, float temp)
    {
        unchecked
        {
            var h = (int)galaxySeed;
            h = h * 397 ^ starIndex;
            h = h * 397 ^ (int)(a * 10f);
            h = h * 397 ^ (int)(theta0 * 100f);
            h = h * 397 ^ (int)temp;
            return h == 0 ? 1 : h;
        }
    }

    /// <summary>Planet count for a seed - same RNG draw as <see cref="Generate"/>.</summary>
    public static int PlanetCountFor(int seed, float tempKelvin)
    {
        var rng = new Random(seed);
        var star = StellarModel.FromTemperature(tempKelvin);
        return PlanetCount(star.SpectralClass, rng);
    }

    /// <summary>
    /// Exact star-system and planet totals (light path - no full system build).
    /// </summary>
    public static (int Systems, long Planets) CountAll(Star[] stars, uint galaxySeed)
    {
        var systems = 0;
        long planets = 0;
        for (var i = 0; i < stars.Length; i++)
        {
            ref readonly var s = ref stars[i];
            if (s.Type != (int)ParticleType.Star)
                continue;

            var seed = MakeSeed(galaxySeed, i, s.A, s.Theta0, s.Temp);
            planets += PlanetCountFor(seed, s.Temp);
            systems++;
        }

        return (systems, planets);
    }

    /// <summary>
    /// Exact systems, planets, and moons (full generate; textures skipped if no window).
    /// </summary>
    public static (int Systems, long Planets, long Moons) CountAllDeep(Star[] stars, uint galaxySeed)
    {
        var systems = 0;
        long planets = 0;
        long moons = 0;
        for (var i = 0; i < stars.Length; i++)
        {
            ref readonly var s = ref stars[i];
            if (s.Type != (int)ParticleType.Star)
                continue;

            var seed = MakeSeed(galaxySeed, i, s.A, s.Theta0, s.Temp);
            var system = Generate(seed, i, s.Temp);
            planets += system.Planets.Count;
            foreach (var planet in system.Planets)
                moons += planet.Moons.Count;
            systems++;
        }

        return (systems, planets, moons);
    }

    public static PlanetarySystem Generate(int seed, int starIndex, float tempKelvin)
    {
        var rng = new Random(seed);
        float R() => (float)rng.NextDouble();

        var star = StellarModel.FromTemperature(tempKelvin);
        var hz = HabitableZone.FromStar(star);

        var count = PlanetCount(star.SpectralClass, rng);

        // Geometric spacing scaled so one slot lands near HZ mid when possible.
        var k = star.SpectralClass == "M" ? 1.35f + 0.15f * R() : 1.55f + 0.25f * R();
        var a0 = hz.MidAu / MathF.Pow(k, count * 0.45f);
        a0 = Math.Clamp(a0, hz.InnerAu * 0.08f, hz.InnerAu * 0.55f);

        // Pass 1: place planets (moons need neighbor gaps from the finished layout).
        var drafts = new List<(string Name, PlanetType Type, float A, float Ecc, float Period,
            float Radius, float Phase, Color Color, bool InHz)>(count);
        for (var i = 0; i < count; i++)
        {
            var a = a0 * MathF.Pow(k, i) * (0.94f + 0.12f * R());
            a = Math.Max(a, 0.02f);
            var ecc = 0.01f + 0.07f * R();
            var period = MathF.Sqrt(MathF.Pow(a, 3f) / Math.Max(star.MassSolar, 0.05f));
            var inHz = a >= hz.InnerAu && a <= hz.OuterAu;
            var type = Classify(a, hz, star);
            var radius = RadiusFor(type, R());
            var color = ColorFor(type, inHz, R());
            drafts.Add((
                $"{star.SpectralClass}-{i + 1}",
                type,
                a,
                ecc,
                Math.Max(period, 0.01f),
                radius,
                R() * MathF.Tau,
                color,
                inHz));
        }

        drafts.Sort((x, y) => x.A.CompareTo(y.A));

        // Pass 2: moons sized so visual rings stay inside the gap to neighboring orbits.
        var planets = new List<Planet>(drafts.Count);
        for (var i = 0; i < drafts.Count; i++)
        {
            var d = drafts[i];
            var peri = d.A * (1f - d.Ecc);
            var apo = d.A * (1f + d.Ecc);
            var gapIn = i > 0
                ? peri - drafts[i - 1].A * (1f + drafts[i - 1].Ecc)
                : peri * 0.5f;
            var gapOut = i + 1 < drafts.Count
                ? drafts[i + 1].A * (1f - drafts[i + 1].Ecc) - apo
                : apo * 0.35f;
            var clearance = Math.Max(0f, Math.Min(gapIn, gapOut));
            // Leave margin so moon rings never touch another planet's orbital ellipse.
            var maxMoonVisual = clearance * 0.38f;

            var visualRadius = Math.Clamp(0.035f + 0.018f * d.Radius, 0.04f, 0.28f);
            var moons = MoonGenerator.Generate(
                rng, star, d.Type, d.A, d.Radius, visualRadius, maxMoonVisual, d.Name);

            planets.Add(new Planet
            {
                Name = d.Name,
                Type = d.Type,
                SemiMajorAu = d.A,
                Eccentricity = d.Ecc,
                PeriodYears = d.Period,
                RadiusEarth = d.Radius,
                PhaseRadians = d.Phase,
                Color = d.Color,
                InHabitableZone = d.InHz,
                Moons = moons,
            });
        }

        var system = new PlanetarySystem
        {
            Star = star,
            HabitableZone = hz,
            Planets = planets,
            SourceStarIndex = starIndex,
            Seed = seed,
        };
        SurfaceGenerator.AssignSystemSurfaces(system, detail: 48);
        return system;
    }

    private static int PlanetCount(string spectralClass, Random rng) => spectralClass switch
    {
        "M" => 3 + rng.Next(0, 3),
        "K" => 4 + rng.Next(0, 3),
        "G" => 5 + rng.Next(0, 4),
        "F" => 4 + rng.Next(0, 4),
        _ => 3 + rng.Next(0, 3),
    };

    private static PlanetType Classify(float a, HabitableZone hz, StellarModel star)
    {
        if (a < hz.InnerAu * 0.55f)
            return PlanetType.HotRocky;
        if (a < hz.InnerAu)
            return PlanetType.Rocky;
        if (a <= hz.OuterAu)
            return PlanetType.Temperate;
        if (a < hz.OuterAu * (star.SpectralClass is "F" or "A" ? 2.2f : 1.7f))
            return PlanetType.Ice;
        return PlanetType.GasGiant;
    }

    private static float RadiusFor(PlanetType type, float r) => type switch
    {
        PlanetType.HotRocky => 0.5f + 0.7f * r,
        PlanetType.Rocky => 0.6f + 0.9f * r,
        PlanetType.Temperate => 0.7f + 0.8f * r,
        PlanetType.Ice => 0.8f + 1.2f * r,
        PlanetType.GasGiant => 4.0f + 8.0f * r,
        _ => 1f,
    };

    private static Color ColorFor(PlanetType type, bool inHz, float r) => type switch
    {
        PlanetType.HotRocky => new Color(180, (int)(90 + 40 * r), 60, 255),
        PlanetType.Rocky => new Color((int)(120 + 40 * r), (int)(100 + 30 * r), 90, 255),
        PlanetType.Temperate => inHz
            ? new Color(70, (int)(140 + 50 * r), (int)(160 + 40 * r), 255)
            : new Color(90, 130, 100, 255),
        PlanetType.Ice => new Color((int)(170 + 40 * r), (int)(200 + 30 * r), 230, 255),
        PlanetType.GasGiant => new Color((int)(180 + 40 * r), (int)(140 + 30 * r), 90, 255),
        _ => Color.Gray,
    };
}
