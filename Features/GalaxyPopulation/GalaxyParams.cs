namespace DensityWaveTheory.Features.GalaxyPopulation;

public sealed class GalaxyParams
{
    public float RadGalaxy { get; set; } = 13000f;
    public float RadCore { get; set; } = 4000f;
    public float AngleOffset { get; set; } = 0.0004f;
    public float ExInner { get; set; } = 0.85f;
    public float ExOuter { get; set; } = 0.95f;
    public int NumStars { get; set; } = 100_000;
    public int NumDust { get; set; } = 100_000;
    public int NumH2 { get; set; } = 400;
    public bool HasDarkMatter { get; set; } = true;
    public int PertN { get; set; } = 2;
    public float PertAmp { get; set; } = 40f;
    public float DustRenderSize { get; set; } = 70f;
    public float BaseTemp { get; set; } = 4000f;
    public float H2SizeMax { get; set; } = 50f;
    public float H2Threshold { get; set; } = 0.55f;
    public uint Seed { get; set; } = 42;

    public float RadFarField => RadGalaxy * 2f;

    public GalaxyParams Clone() => (GalaxyParams)MemberwiseClone();
}
