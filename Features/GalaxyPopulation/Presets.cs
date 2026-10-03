namespace DensityWaveTheory.Features.GalaxyPopulation;

public static class Presets
{
    /// <summary>Hubble-type Sb defaults from the reference renderer.</summary>
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
        DustRenderSize = 55f,
        BaseTemp = 4000f,
        Seed = 42,
    };

    /// <summary>Lighter preset for machines without a strong GPU.</summary>
    public static GalaxyParams SpiralSbLite() => new()
    {
        RadGalaxy = 13000f,
        RadCore = 4000f,
        AngleOffset = 0.0004f,
        ExInner = 0.85f,
        ExOuter = 0.95f,
        NumStars = 40_0000,
        NumDust = 40_0000,
        NumH2 = 200,
        HasDarkMatter = true,
        PertN = 2,
        PertAmp = 40f,
        DustRenderSize = 45f,
        BaseTemp = 4000f,
        Seed = 42,
    };
}
