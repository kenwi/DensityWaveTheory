namespace DensityWaveTheory.Features.StarSystem;

/// <summary>
/// Main-sequence approximation: T_eff → mass, luminosity, radius, spectral class.
/// </summary>
public readonly record struct StellarModel(
    float TempKelvin,
    float MassSolar,
    float LuminositySolar,
    float RadiusSolar,
    string SpectralClass)
{
    public static StellarModel FromTemperature(float tempKelvin)
    {
        var t = Math.Clamp(tempKelvin, 2600f, 12000f);

        // Rough T → M mapping along the main sequence.
        float mass;
        if (t < 3700f)
            mass = 0.20f + 0.35f * ((t - 2600f) / 1100f);
        else if (t < 5200f)
            mass = 0.55f + 0.30f * ((t - 3700f) / 1500f);
        else if (t < 6000f)
            mass = 0.85f + 0.20f * ((t - 5200f) / 800f);
        else if (t < 7500f)
            mass = 1.05f + 0.45f * ((t - 6000f) / 1500f);
        else
            mass = 1.50f + 1.50f * Math.Clamp((t - 7500f) / 4500f, 0f, 1f);

        mass = Math.Clamp(mass, 0.15f, 3.5f);

        // Mass-luminosity: L ∝ M^3.5 near solar mass.
        var luminosity = MathF.Pow(mass, 3.5f);

        // Stefan-Boltzmann: L/Lsun = (R/Rsun)^2 (T/Tsun)^4
        var tSun = 5772f;
        var radius = MathF.Sqrt(luminosity) / MathF.Pow(t / tSun, 2f);
        radius = Math.Clamp(radius, 0.15f, 4.0f);

        var spectral = t switch
        {
            < 3700f => "M",
            < 5200f => "K",
            < 6000f => "G",
            < 7500f => "F",
            _ => "A",
        };

        return new StellarModel(t, mass, luminosity, radius, spectral);
    }
}
