namespace DensityWaveTheory.Features.GalaxyPopulation;

public static class Presets
{
    /// <summary>
    /// Visual match to the published beltoforion article screenshot (face-on).
    /// </summary>
    public static GalaxyParams ReferenceGalaxy1() => new()
    {
        RadGalaxy = 13000f,
        RadCore = 4000f,
        AngleOffset = 0.0004f,
        ExInner = 0.85f,
        ExOuter = 0.95f,
        NumStars = 40_000,
        NumDust = 40_000,
        NumH2 = 400,
        NumDustLanes = 0,
        HasDarkMatter = true,
        PertN = 2,
        PertAmp = 40f,
        DustRenderSize = 95f,
        BaseTemp = 4900f,
        H2SizeMax = 100f,
        H2Threshold = 1.2f,
        FieldOfView = 33960f,
        InclinationDeg = 0f,
        ViewRotationDeg = 0f,
        PhotoLook = false,
        Seed = 420,
    };

    /// <summary>
    /// Approximate Messier 81: inclined, warm bulge, discrete pink HII,
    /// blue outer arms, dark dust-lane filaments. Tuned against the photo.
    /// </summary>
    public static GalaxyParams M81Approx() => new()
    {
        RadGalaxy = 15000f,
        RadCore = 5000f,
        AngleOffset = 0.00032f,
        ExInner = 0.80f,
        ExOuter = 0.90f,
        NumStars = 90_000,
        NumDust = 55_000,
        NumH2 = 500,
        NumDustLanes = 12_000,
        HasDarkMatter = true,
        PertN = 2,
        PertAmp = 55f,
        DustRenderSize = 100f,
        BaseTemp = 3400f,
        H2SizeMax = 70f,
        H2Threshold = 1.25f,
        FieldOfView = 30000f,
        InclinationDeg = 62f,
        ViewRotationDeg = 22f,
        PhotoLook = true,
        Seed = 81,
    };

    /// <summary>Heavier Sb defaults from InitSimulation in the C++ app.</summary>
    public static GalaxyParams SpiralSb() => new()
    {
        RadGalaxy = 13000f,
        RadCore = 4000f,
        AngleOffset = 0.0004f,
        ExInner = 0.85f,
        ExOuter = 0.95f,
        NumStars = 100_000,
        NumDust = 100_000,
        NumH2 = 400,
        NumDustLanes = 0,
        HasDarkMatter = true,
        PertN = 2,
        PertAmp = 40f,
        DustRenderSize = 70f,
        BaseTemp = 4000f,
        H2SizeMax = 100f,
        H2Threshold = 1.2f,
        FieldOfView = 33960f,
        InclinationDeg = 0f,
        ViewRotationDeg = 0f,
        PhotoLook = false,
        Seed = 42,
    };

    public static GalaxyParams SpiralSbLite() => ReferenceGalaxy1();
}
