using DensityWaveTheory.Shared;

namespace DensityWaveTheory.Features.GalaxyPopulation;

public static class OrbitMath
{
    public static float GetExcentricity(GalaxyParams p, float r)
    {
        if (r < p.RadCore)
            return 1f + (r / p.RadCore) * (p.ExInner - 1f);

        if (r <= p.RadGalaxy)
            return p.ExInner + (r - p.RadCore) / (p.RadGalaxy - p.RadCore) * (p.ExOuter - p.ExInner);

        if (r < p.RadFarField)
            return p.ExOuter + (r - p.RadGalaxy) / (p.RadFarField - p.RadGalaxy) * (1f - p.ExOuter);

        return 1f;
    }

    public static float GetAngularOffset(GalaxyParams p, float rad) => rad * p.AngleOffset;

    /// <summary>Orbital angular velocity in degrees per year.</summary>
    public static float GetOrbitalVelocity(GalaxyParams p, float rad)
    {
        if (rad < 1f)
            rad = 1f;

        var velKms = p.HasDarkMatter
            ? VelocityWithDarkMatter(rad)
            : VelocityWithoutDarkMatter(rad);

        var circumferenceKm = 2f * Constants.Pi * rad * Constants.PcToKm;
        var yearsPerOrbit = circumferenceKm / (velKms * Constants.SecPerYear);
        return 360f / yearsPerOrbit;
    }

    public static float VelocityWithDarkMatter(float r)
    {
        const float mz = 100f;
        return 20000f * MathF.Sqrt(Constants.ConstantOfGravity * (MassHalo(r) + MassDisc(r) + mz) / r);
    }

    public static float VelocityWithoutDarkMatter(float r)
    {
        const float mz = 100f;
        return 20000f * MathF.Sqrt(Constants.ConstantOfGravity * (MassDisc(r) + mz) / r);
    }

    private static float MassDisc(float r)
    {
        const float d = 2000f;
        const float rhoSo = 1f;
        const float rH = 2000f;
        return rhoSo * MathF.Exp(-r / rH) * (r * r) * Constants.Pi * d;
    }

    private static float MassHalo(float r)
    {
        const float rhoH0 = 0.15f;
        const float rC = 2500f;
        return rhoH0 * (1f / (1f + MathF.Pow(r / rC, 2f))) * (4f * Constants.Pi * MathF.Pow(r, 3f) / 3f);
    }

    /// <summary>CPU-side position for debug / fallback (matches shader calcPos).</summary>
    public static (float X, float Y) CalcPos(
        float a, float b, float theta0, float velTheta, float time, float tiltAngle,
        int pertN, float pertAmp)
    {
        var thetaActual = theta0 + velTheta * time;
        var beta = -tiltAngle;
        var alpha = thetaActual * Constants.DegToRad;
        var cosAlpha = MathF.Cos(alpha);
        var sinAlpha = MathF.Sin(alpha);
        var cosBeta = MathF.Cos(beta);
        var sinBeta = MathF.Sin(beta);

        var x = a * cosAlpha * cosBeta - b * sinAlpha * sinBeta;
        var y = a * cosAlpha * sinBeta + b * sinAlpha * cosBeta;

        if (pertAmp > 0f && pertN > 0)
        {
            x += (a / pertAmp) * MathF.Sin(alpha * 2f * pertN);
            y += (a / pertAmp) * MathF.Cos(alpha * 2f * pertN);
        }

        return (x, y);
    }
}
