namespace DensityWaveTheory.Features.GalaxyPopulation;

public static class Presets
{
    /// <summary>Hubble-type Sb defaults biased toward visible dust nebula.</summary>
    public static GalaxyParams SpiralSb() => new()
    {
        RadGalaxy = 13000f,
        RadCore = 4000f,
        AngleOffset = 0.00045f,
        ExInner = 0.85f,
        ExOuter = 0.95f,
        NumStars = 70_0000,
        NumDust = 160_000,
        NumH2 = 600,
        HasDarkMatter = true,
        PertN = 2,
        PertAmp = 40f,
        DustRenderSize = 100f,
        BaseTemp = 4000f,
        H2SizeMax = 70f,
        H2Threshold = 0.45f,
        Seed = 42,
    };

    /// <summary>Lighter preset; still dust-heavy for the nebula look.</summary>
    public static GalaxyParams SpiralSbLite() => new()
    {
        RadGalaxy = 13000f,
        RadCore = 4000f,
        AngleOffset = 0.00045f,
        ExInner = 0.85f,
        ExOuter = 0.95f,
        NumStars = 0_000,
        NumDust = 200_000,
        NumH2 = 1000,
        HasDarkMatter = true,
        PertN = 2,
        PertAmp = 40f,
        DustRenderSize = 15f,
        BaseTemp = 4000f,
        H2SizeMax = 65f,
        H2Threshold = 0.45f,
        Seed = 42,
    };
}
