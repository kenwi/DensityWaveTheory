namespace DensityWaveTheory.Shared;

/// <summary>Shared astronomical unit conversions and literature reference values.</summary>
public static class AstroConstants
{
    /// <summary>IAU nominal solar effective temperature (K). Used for Stefan–Boltzmann radius.</summary>
    public const float SolarEffectiveTempKelvin = 5772f;

    /// <summary>
    /// Kopparapu et al. 2013 HZ polynomial reference: t = T_eff - this (K).
    /// Kept distinct from <see cref="SolarEffectiveTempKelvin"/> so the fit stays faithful.
    /// </summary>
    public const float KopparapuReferenceTempKelvin = 5780f;

    /// <summary>Earth radius in AU (~1 R⊕).</summary>
    public const float EarthRadiusAu = 0.0000426f;

    /// <summary>Earth mass in solar masses (M⊕ / M☉).</summary>
    public const float EarthMassSolar = 3.003e-6f;

    /// <summary>
    /// Outer stable prograde moon orbit as a fraction of the Hill radius
    /// (~0.5 R_H per Domingos et al.; code uses a slightly conservative 0.45).
    /// </summary>
    public const float StableMoonHillFraction = 0.45f;

    /// <summary>Minimum moon semi-major axis in planetary radii (Roche / disruption floor).</summary>
    public const float MinMoonOrbitPlanetRadii = 3f;
}
