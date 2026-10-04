using System.Numerics;
using Raylib_cs;

namespace DensityWaveTheory.Features.StarSystem;

/// <summary>Soft discs via a shared radial-gradient texture (avoids layered-circle banding).</summary>
public static class SoftDisc
{
    private static Texture2D _glow;
    private static bool _ready;

    public static void EnsureLoaded()
    {
        if (_ready)
            return;

        // density 0 = full center; outer color alpha 0 for smooth falloff.
        var img = Raylib.GenImageGradientRadial(
            256,
            256,
            0.05f,
            new Color(255, 255, 255, 255),
            new Color(255, 255, 255, 0));
        _glow = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        Raylib.SetTextureFilter(_glow, TextureFilter.Bilinear);
        _ready = true;
    }

    public static void Draw(Vector2 center, float radius, Color color, int layers = 1)
    {
        if (radius <= 0f || color.A == 0)
            return;

        EnsureLoaded();
        layers = Math.Clamp(layers, 1, 4);
        var src = new Rectangle(0, 0, _glow.Width, _glow.Height);

        for (var i = 0; i < layers; i++)
        {
            var t = 1f - i * 0.18f;
            var r = radius * t;
            var a = (byte)Math.Clamp((int)(color.A * (1f - i * 0.22f)), 0, 255);
            if (a < 1)
                continue;

            var dest = new Rectangle(center.X - r, center.Y - r, r * 2f, r * 2f);
            Raylib.DrawTexturePro(
                _glow,
                src,
                dest,
                Vector2.Zero,
                0f,
                new Color(color.R, color.G, color.B, a));
        }
    }
}
