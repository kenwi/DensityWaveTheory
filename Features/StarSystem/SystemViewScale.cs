namespace DensityWaveTheory.Features.StarSystem;

/// <summary>Shared system-view sizing / timing used by generators and nested scenes.</summary>
public static class SystemViewScale
{
    private const float PlanetDiscBaseAu = 0.035f;
    private const float PlanetDiscPerEarthRadiusAu = 0.018f;
    private const float MinPlanetDiscAu = 0.04f;
    private const float MaxPlanetDiscAu = 0.28f;

    /// <summary>
    /// Exaggerated planet disc radius in AU so bodies read at system scale.
    /// Must stay in sync for generation (moon clearance) and picking/drawing.
    /// </summary>
    public static float VisualPlanetRadiusAu(float radiusEarth) =>
        Math.Clamp(
            PlanetDiscBaseAu + PlanetDiscPerEarthRadiusAu * radiusEarth,
            MinPlanetDiscAu,
            MaxPlanetDiscAu);

    /// <summary>Floor for moon orbital periods (animation + generator clamp).</summary>
    public const float MinMoonOrbitPeriodYears = 0.0005f;
}
