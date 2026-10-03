using System.Runtime.InteropServices;

namespace DensityWaveTheory.Features.GalaxyPopulation;

public enum ParticleType : int
{
    Star = 0,
    Dust = 1,
    Filament = 2,
    H2Halo = 3,
    H2Core = 4,
}

/// <summary>
/// Orbital parameters + baked blackbody color (std430, 48 bytes).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Star
{
    public float Theta0;
    public float VelTheta;
    public float TiltAngle;
    public float A;
    public float B;
    public float Temp;
    public float Mag;
    public int Type;
    public float ColorR;
    public float ColorG;
    public float ColorB;
    public float ColorA;

    public void SetColorFromTemperature()
    {
        var c = BlackbodyColor.FromTemperature(Temp);
        ColorR = MathF.Max(c.X, 0f);
        ColorG = MathF.Max(c.Y, 0f);
        ColorB = MathF.Max(c.Z, 0f);
        ColorA = 1f;
    }
}
