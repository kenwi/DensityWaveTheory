namespace DensityWaveTheory.Features.GalaxyPopulation;

public static class Presets
{
    /// <summary>
    /// Visual match to the published beltoforion article screenshot.
    /// Structure follows Galaxy 1.txt; dust temp/size tuned for blue-violet arms.
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
        HasDarkMatter = true,
        PertN = 2,
        PertAmp = 40f,
        DustRenderSize = 95f,
        BaseTemp = 4900f,
        H2SizeMax = 100f,
        H2Threshold = 1.2f,
        FieldOfView = 33960f,
        Seed = 42,
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
        HasDarkMatter = true,
        PertN = 2,
        PertAmp = 40f,
        DustRenderSize = 70f,
        BaseTemp = 4000f,
        H2SizeMax = 100f,
        H2Threshold = 1.2f,
        FieldOfView = 33960f,
        Seed = 42,
    };

    public static GalaxyParams SpiralSbLite() => ReferenceGalaxy1();
}
