using Raylib_cs;

namespace DensityWaveTheory.Features.StarSystem;

/// <summary>
/// Orthographic sphere-disc textures via seeded FBM (Gaia-style elevation / band maps).
/// Samples noise on the unit sphere to avoid polar pinching in the 2D disc view.
/// </summary>
public static class SurfaceGenerator
{
    public static BodySurface CreatePlanet(
        int seed,
        PlanetType type,
        Color baseColor,
        bool inHabitableZone,
        int size)
    {
        size = Math.Clamp(size, 16, 256);
        var img = Raylib.GenImageColor(size, size, Color.Blank);

        for (var py = 0; py < size; py++)
        for (var px = 0; px < size; px++)
        {
            var nx = 2f * px / (size - 1) - 1f;
            var ny = 2f * py / (size - 1) - 1f;
            var r2 = nx * nx + ny * ny;
            if (r2 > 1f)
                continue;

            var nz = MathF.Sqrt(Math.Max(0f, 1f - r2));
            // Domain-warp so continents / bands are less grid-aligned.
            var wx = nx + 0.35f * (ValueNoise.Fbm(nx * 1.7f, ny * 1.7f, nz * 1.7f, seed ^ 11, 3) - 0.5f);
            var wy = ny + 0.35f * (ValueNoise.Fbm(nx * 1.7f, ny * 1.7f, nz * 1.7f, seed ^ 29, 3) - 0.5f);
            var wz = nz + 0.25f * (ValueNoise.Fbm(nx * 1.7f, ny * 1.7f, nz * 1.7f, seed ^ 47, 3) - 0.5f);

            var color = type switch
            {
                PlanetType.GasGiant => GasGiant(wx, wy, wz, seed, baseColor),
                PlanetType.Ice => Ice(wx, wy, wz, seed, baseColor),
                PlanetType.HotRocky => HotRocky(wx, wy, wz, seed, baseColor),
                PlanetType.Temperate => Temperate(wx, wy, wz, seed, baseColor, inHabitableZone),
                _ => Rocky(wx, wy, wz, seed, baseColor),
            };

            // Soft limb darkening for a spherical read without a second highlight disc.
            var limb = MathF.Pow(Math.Clamp(nz, 0f, 1f), 0.55f);
            color = new Color(
                (int)(color.R * (0.55f + 0.45f * limb)),
                (int)(color.G * (0.55f + 0.45f * limb)),
                (int)(color.B * (0.55f + 0.45f * limb)),
                255);

            Raylib.ImageDrawPixel(ref img, px, py, color);
        }

        var tex = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
        return new BodySurface(tex);
    }

    public static BodySurface CreateMoon(int seed, Color baseColor, int size)
    {
        size = Math.Clamp(size, 12, 128);
        var img = Raylib.GenImageColor(size, size, Color.Blank);

        for (var py = 0; py < size; py++)
        for (var px = 0; px < size; px++)
        {
            var nx = 2f * px / (size - 1) - 1f;
            var ny = 2f * py / (size - 1) - 1f;
            var r2 = nx * nx + ny * ny;
            if (r2 > 1f)
                continue;

            var nz = MathF.Sqrt(Math.Max(0f, 1f - r2));
            var elev = ValueNoise.Fbm(nx * 3.2f, ny * 3.2f, nz * 3.2f, seed, 5);
            var ridge = ValueNoise.Ridged(nx * 4.5f, ny * 4.5f, nz * 4.5f, seed ^ 91, 4);
            // Crater-like bowls: sharp dark depressions (common on airless moons).
            var craterN = ValueNoise.Fbm(nx * 7.2f, ny * 7.2f, nz * 7.2f, seed ^ 203, 3);
            var crater = MathF.Pow(1f - Math.Clamp(craterN, 0f, 1f), 3.4f);

            var t = Math.Clamp(0.35f * elev + 0.35f * ridge - 0.45f * crater, 0f, 1f);
            var shade = 0.35f + 0.65f * t;
            var color = new Color(
                (int)(baseColor.R * shade),
                (int)(baseColor.G * shade),
                (int)(baseColor.B * shade * 0.98f),
                255);

            var limb = MathF.Pow(Math.Clamp(nz, 0f, 1f), 0.55f);
            color = new Color(
                (int)(color.R * (0.55f + 0.45f * limb)),
                (int)(color.G * (0.55f + 0.45f * limb)),
                (int)(color.B * (0.55f + 0.45f * limb)),
                255);

            Raylib.ImageDrawPixel(ref img, px, py, color);
        }

        var tex = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
        return new BodySurface(tex);
    }

    public static void AssignSystemSurfaces(PlanetarySystem system, int detail = 48)
    {
        // Allow offline counts / tests before a window exists.
        if (!Raylib.IsWindowReady())
            return;

        var i = 0;
        foreach (var planet in system.Planets)
        {
            var pSeed = unchecked(system.Seed * 397 ^ i * 7919 ^ StableHash(planet.Name));
            planet.Surface?.Dispose();
            planet.Surface = CreatePlanet(pSeed, planet.Type, planet.Color, planet.InHabitableZone, detail);

            var mi = 0;
            foreach (var moon in planet.Moons)
            {
                var mSeed = unchecked(pSeed * 31 ^ mi * 577 ^ StableHash(moon.Name));
                moon.Surface?.Dispose();
                moon.Surface = CreateMoon(mSeed, moon.Color, Math.Max(16, detail / 2));
                mi++;
            }

            i++;
        }
    }

    public static void DisposeSystemSurfaces(PlanetarySystem? system)
    {
        if (system is null)
            return;
        foreach (var planet in system.Planets)
        {
            planet.Surface?.Dispose();
            planet.Surface = null;
            foreach (var moon in planet.Moons)
            {
                moon.Surface?.Dispose();
                moon.Surface = null;
            }
        }
    }

    private static Color GasGiant(float x, float y, float z, int seed, Color baseColor)
    {
        // Latitudinal bands with longitudinal warp (classic gas-giant look).
        var warp = ValueNoise.Fbm(x * 2.2f, y * 0.4f, z * 2.2f, seed, 4) - 0.5f;
        var lat = y + 0.22f * warp;
        var bands = 0.5f + 0.5f * MathF.Sin(lat * MathF.PI * 10f + warp * 3f);
        var storm = ValueNoise.Fbm(x * 5f, y * 5f, z * 5f, seed ^ 17, 3);
        var t = Math.Clamp(0.55f * bands + 0.35f * storm, 0f, 1f);
        var light = LerpColor(baseColor, new Color(255, 230, 200, 255), t * 0.55f);
        var dark = LerpColor(baseColor, new Color(60, 40, 30, 255), (1f - t) * 0.45f);
        return LerpColor(dark, light, t);
    }

    private static Color Ice(float x, float y, float z, int seed, Color baseColor)
    {
        var elev = ValueNoise.Fbm(x * 2.8f, y * 2.8f, z * 2.8f, seed, 5);
        var crack = ValueNoise.Ridged(x * 6f, y * 6f, z * 6f, seed ^ 5, 4);
        var t = Math.Clamp(elev * 0.7f + crack * 0.3f, 0f, 1f);
        var deep = new Color(40, 70, 110, 255);
        var snow = new Color(230, 240, 255, 255);
        return LerpColor(LerpColor(deep, baseColor, 0.55f), snow, t);
    }

    private static Color HotRocky(float x, float y, float z, int seed, Color baseColor)
    {
        var elev = ValueNoise.Fbm(x * 3f, y * 3f, z * 3f, seed, 5);
        var lava = ValueNoise.Ridged(x * 5.5f, y * 5.5f, z * 5.5f, seed ^ 7, 4);
        var crust = LerpColor(new Color(40, 25, 20, 255), baseColor, elev);
        if (lava > 0.72f)
            return LerpColor(crust, new Color(255, 90, 20, 255), (lava - 0.72f) / 0.28f);
        return crust;
    }

    private static Color Temperate(float x, float y, float z, int seed, Color baseColor, bool inHz)
    {
        var elev = ValueNoise.Fbm(x * 2.6f, y * 2.6f, z * 2.6f, seed, 5);
        var moist = ValueNoise.Fbm(x * 3.4f + 10f, y * 3.4f, z * 3.4f, seed ^ 13, 4);
        if (!inHz)
            return Rocky(x, y, z, seed, baseColor);

        // Simple ocean / land / ice-cap biomes from elevation + moisture.
        if (elev < 0.42f)
            return LerpColor(new Color(15, 40, 90, 255), new Color(30, 90, 150, 255), elev / 0.42f);
        if (elev > 0.82f || MathF.Abs(y) > 0.78f)
            return LerpColor(baseColor, new Color(235, 240, 245, 255), 0.7f);

        var land = moist > 0.5f
            ? LerpColor(new Color(30, 90, 40, 255), new Color(70, 140, 60, 255), moist)
            : LerpColor(new Color(120, 100, 55, 255), new Color(90, 110, 50, 255), elev);
        return LerpColor(land, baseColor, 0.2f);
    }

    private static Color Rocky(float x, float y, float z, int seed, Color baseColor)
    {
        var elev = ValueNoise.Fbm(x * 3f, y * 3f, z * 3f, seed, 5);
        var ridge = ValueNoise.Ridged(x * 4.2f, y * 4.2f, z * 4.2f, seed ^ 3, 4);
        var t = Math.Clamp(0.55f * elev + 0.45f * ridge, 0f, 1f);
        var dark = new Color(
            Math.Max(20, baseColor.R / 3),
            Math.Max(18, baseColor.G / 3),
            Math.Max(16, baseColor.B / 3),
            255);
        var light = LerpColor(baseColor, new Color(200, 190, 170, 255), 0.35f);
        return LerpColor(dark, light, t);
    }

    private static Color LerpColor(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new Color(
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t),
            255);
    }

    private static int StableHash(string s)
    {
        unchecked
        {
            var h = 23;
            foreach (var c in s)
                h = h * 31 + c;
            return h;
        }
    }
}
