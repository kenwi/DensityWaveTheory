using System.Numerics;
using DensityWaveTheory.Shared.Gpu;
using Raylib_cs;

namespace DensityWaveTheory.Features.Starfield;

/// <summary>
/// Static world-space starfield drawn behind the galaxy (distant field stars
/// like those in deep-sky M81 frames).
/// </summary>
public sealed class StarfieldRenderer
{
    private readonly record struct BgStar(Vector2 Pos, Color Color, float PixelSize, bool Spike);

    private BgStar[] _stars = [];

    public int Count => _stars.Length;

    public void Rebuild(uint seed, float worldExtent, int count = 3500)
    {
        count = Math.Max(0, count);
        var rng = new Random(unchecked((int)(seed * 2654435761u)));
        float Rnum() => (float)rng.NextDouble();

        var stars = new BgStar[count];
        var half = worldExtent;
        var brightCount = Math.Max(8, count / 120);

        for (var i = 0; i < count; i++)
        {
            var pos = new Vector2((Rnum() * 2f - 1f) * half, (Rnum() * 2f - 1f) * half);

            // Mostly cool white/blue field stars; a few warmer ones.
            var roll = Rnum();
            Color color;
            if (roll < 0.15f)
                color = new Color(255, 210, 170, 255); // warm
            else if (roll < 0.45f)
                color = new Color(200, 220, 255, 255); // blue-white
            else
                color = new Color(235, 235, 245, 255); // soft white

            var bright = i < brightCount;
            var pixelSize = bright
                ? 1.2f + 1.0f * Rnum()
                : 0.55f + 0.55f * Rnum();

            // Dim most stars; keep a handful punchy.
            var dim = bright ? 0.80f + 0.20f * Rnum() : 0.40f + 0.45f * Rnum();
            color = new Color(
                (byte)Math.Clamp((int)(color.R * dim), 0, 255),
                (byte)Math.Clamp((int)(color.G * dim), 0, 255),
                (byte)Math.Clamp((int)(color.B * dim), 0, 255),
                (byte)255);

            stars[i] = new BgStar(pos, color, pixelSize, Spike: bright && Rnum() < 0.55f);
        }

        _stars = stars;
    }

    public void Draw(float cameraZoom)
    {
        if (_stars.Length == 0)
            return;

        var zoom = Math.Max(cameraZoom, 0.0001f);
        ComputeProgram.SetAdditiveBlend();

        foreach (var s in _stars)
        {
            // Keep approximate screen-pixel size across zoom.
            var r = Math.Max(s.PixelSize / zoom, 0.35f / zoom);
            Raylib.DrawCircleV(s.Pos, r, s.Color);

            if (!s.Spike)
                continue;

            var arm = r * 4.0f;
            var tip = new Color(s.Color.R, s.Color.G, s.Color.B, (byte)70);
            Raylib.DrawLineEx(
                new Vector2(s.Pos.X - arm, s.Pos.Y),
                new Vector2(s.Pos.X + arm, s.Pos.Y),
                Math.Max(r * 0.25f, 0.25f / zoom),
                tip);
            Raylib.DrawLineEx(
                new Vector2(s.Pos.X, s.Pos.Y - arm),
                new Vector2(s.Pos.X, s.Pos.Y + arm),
                Math.Max(r * 0.25f, 0.25f / zoom),
                tip);
        }

        ComputeProgram.SetAlphaBlend();
    }
}
