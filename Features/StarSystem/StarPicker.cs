using System.Numerics;
using DensityWaveTheory.Features.GalaxyPopulation;
using Raylib_cs;

namespace DensityWaveTheory.Features.StarSystem;

public readonly record struct PickedStar(
    int Index,
    float Temp,
    float A,
    float Theta0,
    Vector2 WorldPos);

public static class StarPicker
{
    private const float HitRadiusPx = 12f;

    public static bool TryPick(
        Star[] stars,
        GalaxyParams p,
        float timeYears,
        Camera2D camera,
        Vector2 screenPos,
        out PickedStar picked)
    {
        picked = default;
        var bestDistSq = HitRadiusPx * HitRadiusPx;
        var bestIndex = -1;
        float bestTemp = 0f, bestA = 0f, bestTheta = 0f;
        var bestWorld = Vector2.Zero;

        for (var i = 0; i < stars.Length; i++)
        {
            ref readonly var s = ref stars[i];
            if (s.Type != (int)ParticleType.Star)
                continue;

            var (wx, wy) = OrbitMath.CalcPos(
                s.A, s.B, s.Theta0, s.VelTheta, timeYears, s.TiltAngle,
                p.PertN, p.PertAmp);
            var world = ProjectView(new Vector2(wx, wy), p.InclinationDeg, p.ViewRotationDeg);
            var screen = Raylib.GetWorldToScreen2D(world, camera);
            var dx = screen.X - screenPos.X;
            var dy = screen.Y - screenPos.Y;
            var distSq = dx * dx + dy * dy;
            if (distSq >= bestDistSq)
                continue;

            bestDistSq = distSq;
            bestIndex = i;
            bestTemp = s.Temp;
            bestA = s.A;
            bestTheta = s.Theta0;
            bestWorld = world;
        }

        if (bestIndex < 0)
            return false;

        picked = new PickedStar(bestIndex, bestTemp, bestA, bestTheta, bestWorld);
        return true;
    }

    public static bool TryPickRandom(
        Star[] stars,
        GalaxyParams p,
        float timeYears,
        Random rng,
        out PickedStar picked)
    {
        picked = default;
        var starCount = 0;
        for (var i = 0; i < stars.Length; i++)
        {
            if (stars[i].Type == (int)ParticleType.Star)
                starCount++;
        }

        if (starCount == 0)
            return false;

        var target = rng.Next(starCount);
        var seen = 0;
        for (var i = 0; i < stars.Length; i++)
        {
            ref readonly var s = ref stars[i];
            if (s.Type != (int)ParticleType.Star)
                continue;
            if (seen++ != target)
                continue;

            var (wx, wy) = OrbitMath.CalcPos(
                s.A, s.B, s.Theta0, s.VelTheta, timeYears, s.TiltAngle,
                p.PertN, p.PertAmp);
            var world = ProjectView(new Vector2(wx, wy), p.InclinationDeg, p.ViewRotationDeg);
            picked = new PickedStar(i, s.Temp, s.A, s.Theta0, world);
            return true;
        }

        return false;
    }

    /// <summary>Matches orbit_compute.glsl projectView.</summary>
    public static Vector2 ProjectView(Vector2 ps, float inclinationDeg, float viewRotationDeg)
    {
        var inc = Math.Clamp(inclinationDeg, 0f, 89f) * (MathF.PI / 180f);
        ps.Y *= MathF.Cos(inc);
        var rot = viewRotationDeg * (MathF.PI / 180f);
        var c = MathF.Cos(rot);
        var s = MathF.Sin(rot);
        return new Vector2(c * ps.X - s * ps.Y, s * ps.X + c * ps.Y);
    }
}
