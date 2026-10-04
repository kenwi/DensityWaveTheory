namespace DensityWaveTheory.Features.StarSystem;

/// <summary>
/// Kopparapu et al. habitable-zone distances for a 1 Earth-mass planet.
/// Inner = runaway greenhouse, outer = maximum greenhouse.
/// </summary>
public readonly record struct HabitableZone(float InnerAu, float OuterAu)
{
    public float MidAu => 0.5f * (InnerAu + OuterAu);

    public static HabitableZone FromStar(StellarModel star)
    {
        var tStar = star.TempKelvin - 5780f;
        var innerSeff = Seff(tStar, 1.107f, 1.332e-4f, 1.58e-8f, -8.308e-12f, -1.931e-15f);
        var outerSeff = Seff(tStar, 0.356f, 6.171e-5f, 1.698e-9f, -3.198e-12f, -5.575e-16f);

        innerSeff = Math.Max(innerSeff, 0.05f);
        outerSeff = Math.Max(outerSeff, 0.02f);

        var l = Math.Max(star.LuminositySolar, 1e-4f);
        var inner = MathF.Sqrt(l / innerSeff);
        var outer = MathF.Sqrt(l / outerSeff);
        if (outer < inner)
            (inner, outer) = (outer, inner);

        return new HabitableZone(inner, outer);
    }

    private static float Seff(float t, float s0, float a, float b, float c, float d) =>
        s0 + a * t + b * t * t + c * t * t * t + d * t * t * t * t;
}
