using Raylib_cs;

namespace DensityWaveTheory.Features.DebugOverlay;

/// <summary>Coordinate crosshair + scale ticks/labels matching GalaxyWnd::UpdateAxis.</summary>
public sealed class AxisOverlay
{
    public bool Visible { get; set; } = true;

    public void DrawWorld(float fieldOfView)
    {
        if (!Visible)
            return;

        var color = new Color(76, 76, 76, 204); // 0.3, 0.3, 0.3, 0.8
        var tick = fieldOfView / 100f;
        var step = MathF.Pow(10f, MathF.Floor(MathF.Log10(fieldOfView / 2f)));

        Raylib.DrawLineV(new System.Numerics.Vector2(-fieldOfView, 0), new System.Numerics.Vector2(fieldOfView, 0), color);
        Raylib.DrawLineV(new System.Numerics.Vector2(0, -fieldOfView), new System.Numerics.Vector2(0, fieldOfView), color);

        for (var p = step; p < fieldOfView; p += step)
        {
            Raylib.DrawLineV(new System.Numerics.Vector2(p, -tick), new System.Numerics.Vector2(p, tick), color);
            Raylib.DrawLineV(new System.Numerics.Vector2(-p, -tick), new System.Numerics.Vector2(-p, tick), color);
            Raylib.DrawLineV(new System.Numerics.Vector2(-tick, p), new System.Numerics.Vector2(tick, p), color);
            Raylib.DrawLineV(new System.Numerics.Vector2(-tick, -p), new System.Numerics.Vector2(tick, -p), color);
        }
    }

    public void DrawLabels(Camera2D camera, float fieldOfView)
    {
        if (!Visible)
            return;

        var step = MathF.Pow(10f, MathF.Floor(MathF.Log10(fieldOfView / 2f)));
        var tick = fieldOfView / 100f;
        var i = 0;
        for (var p = step; p < fieldOfView; p += step, i++)
        {
            if (i % 2 != 0)
                continue;

            var screen = Raylib.GetWorldToScreen2D(new System.Numerics.Vector2(p - tick, -4f * tick), camera);
            Raylib.DrawText($"{p:0}", (int)screen.X, (int)screen.Y, 16, Color.RayWhite);
        }
    }
}
