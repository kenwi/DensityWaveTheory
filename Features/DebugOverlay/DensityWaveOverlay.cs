using System.Numerics;
using DensityWaveTheory.Features.GalaxyPopulation;
using Raylib_cs;

namespace DensityWaveTheory.Features.DebugOverlay;

public sealed class DensityWaveOverlay
{
    public bool Visible { get; set; }

    public void Draw(GalaxyParams p, int pertN, float pertAmp)
    {
        if (!Visible)
            return;

        const int orbitCount = 40;
        var dr = p.RadFarField / orbitCount;
        for (var i = 1; i <= orbitCount; i++)
        {
            var r = dr * i;
            var a = r;
            var b = r * OrbitMath.GetExcentricity(p, r);
            var tilt = OrbitMath.GetAngularOffset(p, r);
            DrawEllipseOrbit(a, b, tilt, pertN, pertAmp, new Color(255, 255, 255, 40));
        }

        DrawCircleBoundary(p.RadCore, new Color(255, 255, 0, 120));
        DrawCircleBoundary(p.RadGalaxy, new Color(0, 255, 0, 120));
        DrawCircleBoundary(p.RadFarField, new Color(255, 0, 0, 80));
    }

    private static void DrawCircleBoundary(float radius, Color color)
    {
        DrawEllipseOrbit(radius, radius, 0f, 0, 0f, color);
    }

    private static void DrawEllipseOrbit(float a, float b, float tiltDeg, int pertN, float pertAmp, Color color)
    {
        const int steps = 90;
        Vector2? prev = null;
        for (var i = 0; i <= steps; i++)
        {
            var theta = 360f * i / steps;
            var (x, y) = OrbitMath.CalcPos(a, b, theta, 0f, 0f, tiltDeg, pertN, pertAmp);
            var cur = new Vector2(x, y);
            if (prev is { } p0)
                Raylib.DrawLineV(p0, cur, color);
            prev = cur;
        }
    }
}
