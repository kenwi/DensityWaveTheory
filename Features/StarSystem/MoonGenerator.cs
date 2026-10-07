using Raylib_cs;

namespace DensityWaveTheory.Features.StarSystem;

/// <summary>
/// Deterministic moon populations informed by Solar System demographics and
/// exomoon survival literature (close-in rocky hosts rarely keep moons; ice/gas
/// giants almost always host regular satellite systems within ~0.5 R_Hill).
/// </summary>
public static class MoonGenerator
{
    public static IReadOnlyList<Moon> Generate(
        Random rng,
        StellarModel star,
        PlanetType type,
        float planetSemiMajorAu,
        float planetRadiusEarth,
        float planetVisualRadiusAu,
        float maxVisualOrbitAu,
        string planetName)
    {
        float R() => (float)rng.NextDouble();

        var pHave = BaseHaveProbability(type) * CloseInSurvival(planetSemiMajorAu);
        if (R() > pHave)
            return [];

        var count = SampleCount(type, rng);
        if (count <= 0)
            return [];

        var planetMassEarth = MassEarth(type, planetRadiusEarth);
        var hillAu = HillRadiusAu(planetSemiMajorAu, planetMassEarth, star.MassSolar);
        // Stable prograde moons typically sit inside ~0.5 R_Hill (Domingos et al.).
        var outerStable = hillAu * 0.45f;
        var inner = Math.Max(planetRadiusEarth * 0.0000426f * 3f, outerStable * 0.05f); // ~3 planetary radii floor
        if (outerStable <= inner * 1.2f)
            return []; // Hill sphere too small (very close-in)

        // Keep drawn moon rings inside the clear gap to neighboring planet orbits.
        var minVisual = planetVisualRadiusAu * 1.7f;
        var maxVisual = Math.Min(maxVisualOrbitAu, planetVisualRadiusAu * 4.5f);
        if (maxVisual < minVisual * 1.05f)
            return []; // no room to draw moons without crossing neighbors

        // Pack fewer moons when the clearance is tight so rings stay separated.
        var packSpan = maxVisual - minVisual;
        var maxByClearance = 1 + (int)(packSpan / Math.Max(planetVisualRadiusAu * 0.55f, 0.02f));
        count = Math.Min(count, Math.Clamp(maxByClearance, 1, 12));

        var moons = new List<Moon>(count);
        for (var i = 0; i < count; i++)
        {
            var t = count == 1 ? 0.55f : (i + 0.65f) / (count + 0.3f);
            var aPhys = inner + (outerStable - inner) * t * (0.9f + 0.2f * R());
            aPhys = Math.Clamp(aPhys, inner, outerStable);

            // Kepler period around the planet (years): P = 2π √(a³ / GM).
            // Using Earth units: P_yr ≈ √(a_AU³ / (M_earth / M_sun)) with M_earth/M_sun ≈ 3.003e-6.
            var mSolar = Math.Max(planetMassEarth * 3.003e-6f, 1e-9f);
            var period = MathF.Sqrt(MathF.Pow(aPhys, 3f) / mSolar);
            // Moons orbit fast; clamp so animation stays readable at system time scale.
            period = Math.Clamp(period, 0.0008f, 0.08f);

            // Evenly pack visual orbits between min/max so outer moons never reach neighbors.
            var u = count == 1 ? 0.55f : i / (float)(count - 1);
            var visual = minVisual + packSpan * (0.12f + 0.82f * u);

            var radius = RadiusEarthFor(type, planetRadiusEarth, R());
            moons.Add(new Moon
            {
                Name = $"{planetName}m{i + 1}",
                SemiMajorAu = aPhys,
                VisualOrbitAu = visual,
                PeriodYears = period,
                RadiusEarth = radius,
                PhaseRadians = R() * MathF.Tau,
                Color = ColorFor(type, R()),
            });
        }

        return moons;
    }

    /// <summary>
    /// Planet-focus close-up: keep the system moon count, pack orbits at local visual scale
    /// with no sibling-planet clearance (the rest of the system is not loaded).
    /// </summary>
    public static IReadOnlyList<Moon> GenerateFocus(
        Random rng,
        StellarModel star,
        PlanetType type,
        float planetSemiMajorAu,
        float planetRadiusEarth,
        float planetVisualRadiusAu,
        string planetName,
        int forceCount)
    {
        if (forceCount <= 0)
            return [];

        float R() => (float)rng.NextDouble();
        var count = Math.Clamp(forceCount, 1, 12);

        var planetMassEarth = MassEarth(type, planetRadiusEarth);
        var hillAu = HillRadiusAu(planetSemiMajorAu, planetMassEarth, star.MassSolar);
        var outerStable = Math.Max(hillAu * 0.45f, planetRadiusEarth * 0.0000426f * 40f);
        var inner = Math.Max(planetRadiusEarth * 0.0000426f * 3f, outerStable * 0.05f);

        var minVisual = planetVisualRadiusAu * 1.55f;
        var maxVisual = planetVisualRadiusAu * (2.8f + 0.55f * count);
        var packSpan = maxVisual - minVisual;

        var moons = new List<Moon>(count);
        for (var i = 0; i < count; i++)
        {
            var t = count == 1 ? 0.55f : (i + 0.65f) / (count + 0.3f);
            var aPhys = inner + (outerStable - inner) * t * (0.9f + 0.2f * R());
            aPhys = Math.Clamp(aPhys, inner, outerStable);

            var mSolar = Math.Max(planetMassEarth * 3.003e-6f, 1e-9f);
            var period = MathF.Sqrt(MathF.Pow(Math.Max(aPhys, 1e-9f), 3f) / mSolar);
            period = Math.Clamp(period, 0.0005f, 0.12f);

            var u = count == 1 ? 0.5f : i / (float)(count - 1);
            var visual = minVisual + packSpan * (0.08f + 0.88f * u);

            moons.Add(new Moon
            {
                Name = $"{planetName}m{i + 1}",
                SemiMajorAu = aPhys,
                VisualOrbitAu = visual,
                PeriodYears = period,
                RadiusEarth = RadiusEarthFor(type, planetRadiusEarth, R()),
                PhaseRadians = R() * MathF.Tau,
                Color = ColorFor(type, R()),
            });
        }

        return moons;
    }

    /// <summary>
    /// Chance the planet hosts at least one long-lived moon.
    /// Hot/close-in rocky worlds almost never retain moons; cold giants nearly always do.
    /// </summary>
    private static float BaseHaveProbability(PlanetType type) => type switch
    {
        PlanetType.HotRocky => 0.06f,   // Mercury/Venus-like; tidal loss / tiny Hill sphere
        PlanetType.Rocky => 0.32f,      // Mars-like often has tiny moons; Earth-like ~coin flip
        PlanetType.Temperate => 0.48f,  // HZ rocky: Earth has 1; many may have 0-2
        PlanetType.Ice => 0.82f,        // Uranus/Neptune-class: regular mid-size systems common
        PlanetType.GasGiant => 0.96f,   // Jupiter/Saturn-class: regular satellite systems ubiquitous
        _ => 0.2f,
    };

    /// <summary>Close-in planets lose moons to tides / Hill shrinkage (Sasaki, Zollinger et al.).</summary>
    private static float CloseInSurvival(float aAu)
    {
        if (aAu < 0.08f) return 0.05f;
        if (aAu < 0.20f) return 0.25f;
        if (aAu < 0.45f) return 0.55f;
        if (aAu < 0.80f) return 0.85f;
        return 1f;
    }

    private static int SampleCount(PlanetType type, Random rng)
    {
        // Counts are "noteworthy" moons (regular / major), not every capture debris rock.
        return type switch
        {
            PlanetType.HotRocky => 1,
            PlanetType.Rocky => 1 + (rng.NextDouble() < 0.35 ? 1 : 0),           // 1-2
            PlanetType.Temperate => 1 + (rng.NextDouble() < 0.40 ? 1 : 0),       // 1-2
            PlanetType.Ice => 2 + rng.Next(0, 5),                                // 2-6 (Uranus ~5 major)
            PlanetType.GasGiant => 4 + rng.Next(0, 9),                           // 4-12 (Galilean+)
            _ => 1,
        };
    }

    private static float MassEarth(PlanetType type, float radiusEarth) => type switch
    {
        PlanetType.HotRocky or PlanetType.Rocky or PlanetType.Temperate =>
            MathF.Pow(Math.Max(radiusEarth, 0.3f), 3.3f),
        PlanetType.Ice => Math.Clamp(8f + radiusEarth * 6f, 10f, 25f),
        PlanetType.GasGiant => Math.Clamp(50f + radiusEarth * 40f, 80f, 400f),
        _ => 1f,
    };

    private static float HillRadiusAu(float aAu, float massEarth, float starMassSolar)
    {
        var mSolar = massEarth * 3.003e-6f;
        var mu = mSolar / Math.Max(starMassSolar, 0.05f);
        return aAu * MathF.Pow(mu / 3f, 1f / 3f);
    }

    private static float RadiusEarthFor(PlanetType type, float planetRadiusEarth, float r) => type switch
    {
        PlanetType.GasGiant => 0.15f + 0.55f * r,          // Luna–Titan class
        PlanetType.Ice => 0.10f + 0.35f * r,
        _ => Math.Clamp(0.08f + 0.22f * r, 0.06f, planetRadiusEarth * 0.45f),
    };

    private static Color ColorFor(PlanetType type, float r) => type switch
    {
        PlanetType.GasGiant => new Color(
            (int)(160 + 50 * r), (int)(150 + 40 * r), (int)(140 + 30 * r), 230),
        PlanetType.Ice => new Color(
            (int)(190 + 40 * r), (int)(200 + 30 * r), (int)(210 + 30 * r), 230),
        _ => new Color(
            (int)(120 + 40 * r), (int)(115 + 35 * r), (int)(110 + 30 * r), 220),
    };
}
