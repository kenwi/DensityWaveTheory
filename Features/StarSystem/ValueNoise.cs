namespace DensityWaveTheory.Features.StarSystem;

/// <summary>Seeded 3D value noise + FBM helpers for procedural body surfaces.</summary>
public static class ValueNoise
{
    public static float Fbm(float x, float y, float z, int seed, int octaves = 5, float lacunarity = 2.02f, float gain = 0.5f)
    {
        var sum = 0f;
        var amp = 0.5f;
        var freq = 1f;
        var norm = 0f;
        for (var i = 0; i < octaves; i++)
        {
            sum += amp * Value3(x * freq, y * freq, z * freq, seed + i * 1013);
            norm += amp;
            freq *= lacunarity;
            amp *= gain;
        }

        return norm > 0f ? sum / norm : 0f;
    }

    public static float Ridged(float x, float y, float z, int seed, int octaves = 4)
    {
        var n = Fbm(x, y, z, seed, octaves);
        var r = 1f - MathF.Abs(2f * n - 1f);
        return r * r;
    }

    public static float Value3(float x, float y, float z, int seed)
    {
        var x0 = (int)MathF.Floor(x);
        var y0 = (int)MathF.Floor(y);
        var z0 = (int)MathF.Floor(z);
        var xf = x - x0;
        var yf = y - y0;
        var zf = z - z0;
        // Smoothstep
        xf = xf * xf * (3f - 2f * xf);
        yf = yf * yf * (3f - 2f * yf);
        zf = zf * zf * (3f - 2f * zf);

        var n000 = Hash(x0, y0, z0, seed);
        var n100 = Hash(x0 + 1, y0, z0, seed);
        var n010 = Hash(x0, y0 + 1, z0, seed);
        var n110 = Hash(x0 + 1, y0 + 1, z0, seed);
        var n001 = Hash(x0, y0, z0 + 1, seed);
        var n101 = Hash(x0 + 1, y0, z0 + 1, seed);
        var n011 = Hash(x0, y0 + 1, z0 + 1, seed);
        var n111 = Hash(x0 + 1, y0 + 1, z0 + 1, seed);

        var nx00 = Lerp(n000, n100, xf);
        var nx10 = Lerp(n010, n110, xf);
        var nx01 = Lerp(n001, n101, xf);
        var nx11 = Lerp(n011, n111, xf);
        var nxy0 = Lerp(nx00, nx10, yf);
        var nxy1 = Lerp(nx01, nx11, yf);
        return Lerp(nxy0, nxy1, zf);
    }

    private static float Hash(int x, int y, int z, int seed)
    {
        unchecked
        {
            var h = seed;
            h = h * 374761393 + x * 668265263;
            h = h * 374761393 + y * 2147483647;
            h = h * 374761393 + z * 1013904223;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0x7FFFFFFF) / (float)0x7FFFFFFF;
        }
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
