using DensityWaveTheory.Shared;
using Raylib_cs;

namespace DensityWaveTheory.Features.StarSystem;

/// <summary>
/// Deterministic moon populations informed by Solar System demographics and
/// exomoon survival literature (close-in rocky hosts rarely keep moons; ice/gas
/// giants almost always host regular satellite systems within ~0.5 R_Hill).
/// </summary>
public static class MoonGenerator
{
    private const float HillInnerFloorFraction = 0.05f;
    private const float MinHillSpanFactor = 1.2f;
    private const float MinPlanetMassSolar = 1e-9f;
    private const float MinStarMassSolar = 0.05f;
    private const int MaxMoons = 12;

    private const float SystemMinVisualOrbitFactor = 1.7f;
    private const float SystemMaxVisualOrbitFactor = 4.5f;
    private const float SystemMinVisualSpanFactor = 1.05f;
    private const float SystemClearanceSlotFactor = 0.55f;
    private const float SystemMinClearanceSlotAu = 0.02f;
    private const float SystemMinPeriodYears = 0.0008f;
    private const float SystemMaxPeriodYears = 0.08f;
    private const float SystemVisualPackBase = 0.12f;
    private const float SystemVisualPackSpan = 0.82f;

    private const float FocusFallbackPlanetRadii = 40f;
    private const float FocusMinVisualOrbitFactor = 1.55f;
    private const float FocusMaxVisualBaseFactor = 2.8f;
    private const float FocusMaxVisualPerMoon = 0.55f;
    private const float FocusMaxPeriodYears = 0.12f;
    private const float FocusVisualPackBase = 0.08f;
    private const float FocusVisualPackSpan = 0.88f;

    private const float PhysOrbitJitterBase = 0.9f;
    private const float PhysOrbitJitterSpan = 0.2f;
    private const float SingleMoonOrbitT = 0.55f;
    private const float MultiMoonOrbitTOffset = 0.65f;
    private const float MultiMoonOrbitTDenomPad = 0.3f;

    private const float HaveHotRocky = 0.06f;
    private const float HaveRocky = 0.32f;
    private const float HaveTemperate = 0.48f;
    private const float HaveIce = 0.82f;
    private const float HaveGasGiant = 0.96f;
    private const float HaveFallback = 0.2f;

    private const float CloseInAu0 = 0.08f;
    private const float CloseInAu1 = 0.20f;
    private const float CloseInAu2 = 0.45f;
    private const float CloseInAu3 = 0.80f;
    private const float CloseInSurvive0 = 0.05f;
    private const float CloseInSurvive1 = 0.25f;
    private const float CloseInSurvive2 = 0.55f;
    private const float CloseInSurvive3 = 0.85f;

    private const float RockySecondMoonChance = 0.35f;
    private const float TemperateSecondMoonChance = 0.40f;
    private const int IceMoonBase = 2;
    private const int IceMoonExtra = 5;
    private const int GasGiantMoonBase = 4;
    private const int GasGiantMoonExtra = 9;

    private const float RockyMassRadiusFloor = 0.3f;
    private const float RockyMassRadiusExponent = 3.3f;

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
        var outerStable = hillAu * AstroConstants.StableMoonHillFraction;
        var inner = Math.Max(
            planetRadiusEarth * AstroConstants.EarthRadiusAu * AstroConstants.MinMoonOrbitPlanetRadii,
            outerStable * HillInnerFloorFraction);
        if (outerStable <= inner * MinHillSpanFactor)
            return []; // Hill sphere too small (very close-in)

        // Keep drawn moon rings inside the clear gap to neighboring planet orbits.
        var minVisual = planetVisualRadiusAu * SystemMinVisualOrbitFactor;
        var maxVisual = Math.Min(maxVisualOrbitAu, planetVisualRadiusAu * SystemMaxVisualOrbitFactor);
        if (maxVisual < minVisual * SystemMinVisualSpanFactor)
            return []; // no room to draw moons without crossing neighbors

        // Pack fewer moons when the clearance is tight so rings stay separated.
        var packSpan = maxVisual - minVisual;
        var maxByClearance = 1 + (int)(packSpan / Math.Max(
            planetVisualRadiusAu * SystemClearanceSlotFactor,
            SystemMinClearanceSlotAu));
        count = Math.Min(count, Math.Clamp(maxByClearance, 1, MaxMoons));

        var moons = new List<Moon>(count);
        for (var i = 0; i < count; i++)
        {
            var t = count == 1
                ? SingleMoonOrbitT
                : (i + MultiMoonOrbitTOffset) / (count + MultiMoonOrbitTDenomPad);
            var aPhys = inner + (outerStable - inner) * t * (PhysOrbitJitterBase + PhysOrbitJitterSpan * R());
            aPhys = Math.Clamp(aPhys, inner, outerStable);

            // Kepler period around the planet (years): P ≈ √(a_AU³ / (M_p / M☉)).
            var mSolar = Math.Max(planetMassEarth * AstroConstants.EarthMassSolar, MinPlanetMassSolar);
            var period = MathF.Sqrt(MathF.Pow(aPhys, 3f) / mSolar);
            // Moons orbit fast; clamp so animation stays readable at system time scale.
            period = Math.Clamp(period, SystemMinPeriodYears, SystemMaxPeriodYears);

            // Evenly pack visual orbits between min/max so outer moons never reach neighbors.
            var u = count == 1 ? SingleMoonOrbitT : i / (float)(count - 1);
            var visual = minVisual + packSpan * (SystemVisualPackBase + SystemVisualPackSpan * u);

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
        var count = Math.Clamp(forceCount, 1, MaxMoons);

        var planetMassEarth = MassEarth(type, planetRadiusEarth);
        var hillAu = HillRadiusAu(planetSemiMajorAu, planetMassEarth, star.MassSolar);
        var outerStable = Math.Max(
            hillAu * AstroConstants.StableMoonHillFraction,
            planetRadiusEarth * AstroConstants.EarthRadiusAu * FocusFallbackPlanetRadii);
        var inner = Math.Max(
            planetRadiusEarth * AstroConstants.EarthRadiusAu * AstroConstants.MinMoonOrbitPlanetRadii,
            outerStable * HillInnerFloorFraction);

        var minVisual = planetVisualRadiusAu * FocusMinVisualOrbitFactor;
        var maxVisual = planetVisualRadiusAu * (FocusMaxVisualBaseFactor + FocusMaxVisualPerMoon * count);
        var packSpan = maxVisual - minVisual;

        var moons = new List<Moon>(count);
        for (var i = 0; i < count; i++)
        {
            var t = count == 1
                ? SingleMoonOrbitT
                : (i + MultiMoonOrbitTOffset) / (count + MultiMoonOrbitTDenomPad);
            var aPhys = inner + (outerStable - inner) * t * (PhysOrbitJitterBase + PhysOrbitJitterSpan * R());
            aPhys = Math.Clamp(aPhys, inner, outerStable);

            var mSolar = Math.Max(planetMassEarth * AstroConstants.EarthMassSolar, MinPlanetMassSolar);
            var period = MathF.Sqrt(MathF.Pow(Math.Max(aPhys, 1e-9f), 3f) / mSolar);
            period = Math.Clamp(period, SystemViewScale.MinMoonOrbitPeriodYears, FocusMaxPeriodYears);

            var u = count == 1 ? 0.5f : i / (float)(count - 1);
            var visual = minVisual + packSpan * (FocusVisualPackBase + FocusVisualPackSpan * u);

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
        PlanetType.HotRocky => HaveHotRocky,   // Mercury/Venus-like; tidal loss / tiny Hill sphere
        PlanetType.Rocky => HaveRocky,         // Mars-like often has tiny moons; Earth-like ~coin flip
        PlanetType.Temperate => HaveTemperate, // HZ rocky: Earth has 1; many may have 0-2
        PlanetType.Ice => HaveIce,             // Uranus/Neptune-class: regular mid-size systems common
        PlanetType.GasGiant => HaveGasGiant,   // Jupiter/Saturn-class: regular satellite systems ubiquitous
        _ => HaveFallback,
    };

    /// <summary>Close-in planets lose moons to tides / Hill shrinkage (Sasaki, Zollinger et al.).</summary>
    private static float CloseInSurvival(float aAu)
    {
        if (aAu < CloseInAu0) return CloseInSurvive0;
        if (aAu < CloseInAu1) return CloseInSurvive1;
        if (aAu < CloseInAu2) return CloseInSurvive2;
        if (aAu < CloseInAu3) return CloseInSurvive3;
        return 1f;
    }

    private static int SampleCount(PlanetType type, Random rng)
    {
        // Counts are "noteworthy" moons (regular / major), not every capture debris rock.
        return type switch
        {
            PlanetType.HotRocky => 1,
            PlanetType.Rocky => 1 + (rng.NextDouble() < RockySecondMoonChance ? 1 : 0),
            PlanetType.Temperate => 1 + (rng.NextDouble() < TemperateSecondMoonChance ? 1 : 0),
            PlanetType.Ice => IceMoonBase + rng.Next(0, IceMoonExtra),
            PlanetType.GasGiant => GasGiantMoonBase + rng.Next(0, GasGiantMoonExtra),
            _ => 1,
        };
    }

    private static float MassEarth(PlanetType type, float radiusEarth) => type switch
    {
        PlanetType.HotRocky or PlanetType.Rocky or PlanetType.Temperate =>
            MathF.Pow(Math.Max(radiusEarth, RockyMassRadiusFloor), RockyMassRadiusExponent),
        PlanetType.Ice => Math.Clamp(8f + radiusEarth * 6f, 10f, 25f),
        PlanetType.GasGiant => Math.Clamp(50f + radiusEarth * 40f, 80f, 400f),
        _ => 1f,
    };

    private static float HillRadiusAu(float aAu, float massEarth, float starMassSolar)
    {
        var mSolar = massEarth * AstroConstants.EarthMassSolar;
        var mu = mSolar / Math.Max(starMassSolar, MinStarMassSolar);
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
