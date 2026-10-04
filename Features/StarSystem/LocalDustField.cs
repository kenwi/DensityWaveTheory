using System.Numerics;
using DensityWaveTheory.Features.GalaxyPopulation;
using Raylib_cs;

namespace DensityWaveTheory.Features.StarSystem;

/// <summary>
/// Soft dust / filament glow sampled from galaxy particles near a picked star,
/// remapped into the star-system AU view as a subtle nebula wash.
/// </summary>
public sealed class LocalDustField
{
    private readonly DustSpeck[] _specks;
    private readonly Color _ambient;

    private LocalDustField(DustSpeck[] specks, Color ambient)
    {
        _specks = specks;
        _ambient = ambient;
    }

    public static LocalDustField Sample(
        Star[] stars,
        GalaxyParams p,
        float timeYears,
        Vector2 starWorld,
        int seed,
        float systemFovAu)
    {
        var sampleRadius = Math.Clamp(p.RadGalaxy * 0.035f, 180f, 900f);
        var sampleRadiusSq = sampleRadius * sampleRadius;

        var candidates = new List<(Vector2 Rel, float Mag, Vector4 Color, int Type)>(256);
        var sumR = 0f;
        var sumG = 0f;
        var sumB = 0f;
        var weightSum = 0f;

        for (var i = 0; i < stars.Length; i++)
        {
            ref readonly var s = ref stars[i];
            if (s.Type != (int)ParticleType.Dust && s.Type != (int)ParticleType.Filament)
                continue;

            var (wx, wy) = OrbitMath.CalcPos(
                s.A, s.B, s.Theta0, s.VelTheta, timeYears, s.TiltAngle,
                p.PertN, p.PertAmp);
            var world = StarPicker.ProjectView(new Vector2(wx, wy), p.InclinationDeg, p.ViewRotationDeg);
            var rel = world - starWorld;
            var distSq = rel.LengthSquared();
            if (distSq > sampleRadiusSq)
                continue;

            var w = 1f / (1f + distSq / (sampleRadiusSq * 0.25f));
            sumR += s.ColorR * w;
            sumG += s.ColorG * w;
            sumB += s.ColorB * w;
            weightSum += w;

            candidates.Add((rel, s.Mag, new Vector4(s.ColorR, s.ColorG, s.ColorB, 1f), s.Type));
        }

        // Keep ambient muted - additive texture glow blows out easily.
        var ambient = weightSum > 0f
            ? ToColor(sumR / weightSum * 0.85f, sumG / weightSum * 0.85f, sumB / weightSum * 0.85f, 36)
            : new Color(100, 80, 65, 28);

        candidates.Sort((a, b) =>
        {
            var sa = a.Rel.LengthSquared() / Math.Max(a.Mag, 0.01f);
            var sb = b.Rel.LengthSquared() / Math.Max(b.Mag, 0.01f);
            return sa.CompareTo(sb);
        });

        var take = Math.Min(candidates.Count, 10);
        var rng = new Random(seed ^ 0x5D1757);
        var specks = new DustSpeck[take];
        var spread = systemFovAu * 0.4f;

        for (var i = 0; i < take; i++)
        {
            var c = candidates[i];
            var len = c.Rel.Length();
            var dir = len > 1e-4f ? c.Rel / len : RandomUnit(rng);
            var radial = Math.Clamp(len / sampleRadius, 0.05f, 1f);
            var offset = dir * (spread * (0.08f + 0.4f * radial));
            offset += new Vector2(
                ((float)rng.NextDouble() - 0.5f) * spread * 0.1f,
                ((float)rng.NextDouble() - 0.5f) * spread * 0.1f);

            var isFilament = c.Type == (int)ParticleType.Filament;
            var radius = systemFovAu * (isFilament ? 0.35f : 0.5f) * (0.65f + c.Mag * 1.8f);
            radius = Math.Clamp(radius, systemFovAu * 0.22f, systemFovAu * 0.7f);

            var intensity = isFilament ? 0.6f : 0.9f;
            var a = (byte)Math.Clamp((int)(14 + c.Mag * 40 * intensity), 10, 32);
            specks[i] = new DustSpeck(
                offset,
                radius,
                ToColor(c.Color.X * intensity, c.Color.Y * intensity, c.Color.Z * intensity, a));
        }

        return new LocalDustField(specks, ambient);
    }

    public void Draw(float systemFovAu, bool softGlow)
    {
        Raylib.BeginBlendMode(BlendMode.Additive);

        var ambA = softGlow ? _ambient.A : (byte)Math.Max(8, _ambient.A * 2 / 3);
        SoftDisc.Draw(
            Vector2.Zero,
            systemFovAu * (softGlow ? 0.9f : 0.65f),
            new Color(_ambient.R, _ambient.G, _ambient.B, ambA),
            1);

        foreach (var speck in _specks)
        {
            var a = softGlow ? speck.Color.A : (byte)Math.Max(6, speck.Color.A * 2 / 3);
            SoftDisc.Draw(
                speck.Offset,
                speck.Radius,
                new Color(speck.Color.R, speck.Color.G, speck.Color.B, a),
                1);
        }

        Raylib.EndBlendMode();
    }

    private static Vector2 RandomUnit(Random rng)
    {
        var a = (float)rng.NextDouble() * MathF.Tau;
        return new Vector2(MathF.Cos(a), MathF.Sin(a));
    }

    private static Color ToColor(float r, float g, float b, byte a) => new(
        (byte)Math.Clamp((int)(r * 255f), 0, 255),
        (byte)Math.Clamp((int)(g * 255f), 0, 255),
        (byte)Math.Clamp((int)(b * 255f), 0, 255),
        a);

    private readonly record struct DustSpeck(Vector2 Offset, float Radius, Color Color);
}
