using DensityWaveTheory.Shared;

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
        var tStar = star.TempKelvin - AstroConstants.KopparapuReferenceTempKelvin;
        var innerSeff = Seff(
            tStar,
            Kopparapu2013.InnerS0,
            Kopparapu2013.InnerA,
            Kopparapu2013.InnerB,
            Kopparapu2013.InnerC,
            Kopparapu2013.InnerD);
        var outerSeff = Seff(
            tStar,
            Kopparapu2013.OuterS0,
            Kopparapu2013.OuterA,
            Kopparapu2013.OuterB,
            Kopparapu2013.OuterC,
            Kopparapu2013.OuterD);

        innerSeff = Math.Max(innerSeff, Kopparapu2013.MinInnerSeff);
        outerSeff = Math.Max(outerSeff, Kopparapu2013.MinOuterSeff);

        var l = Math.Max(star.LuminositySolar, Kopparapu2013.MinLuminositySolar);
        var inner = MathF.Sqrt(l / innerSeff);
        var outer = MathF.Sqrt(l / outerSeff);
        if (outer < inner)
            (inner, outer) = (outer, inner);

        return new HabitableZone(inner, outer);
    }

    private static float Seff(float t, float s0, float a, float b, float c, float d) =>
        s0 + a * t + b * t * t + c * t * t * t + d * t * t * t * t;

    /// <summary>
    /// Kopparapu et al. 2013 ApJ 765:131 polynomial coefficients for 1 M⊕.
    /// Inner = runaway greenhouse; outer = maximum greenhouse.
    /// </summary>
    private static class Kopparapu2013
    {
        public const float InnerS0 = 1.107f;
        public const float InnerA = 1.332e-4f;
        public const float InnerB = 1.58e-8f;
        public const float InnerC = -8.308e-12f;
        public const float InnerD = -1.931e-15f;

        public const float OuterS0 = 0.356f;
        public const float OuterA = 6.171e-5f;
        public const float OuterB = 1.698e-9f;
        public const float OuterC = -3.198e-12f;
        public const float OuterD = -5.575e-16f;

        public const float MinInnerSeff = 0.05f;
        public const float MinOuterSeff = 0.02f;
        public const float MinLuminositySolar = 1e-4f;
    }
}
