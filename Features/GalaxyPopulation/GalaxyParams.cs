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
    public float H2SizeMax { get; set; } = 100f;
    public float H2Threshold { get; set; } = 1.2f;
    /// <summary>Occluding dark dust-lane particles (0 = none).</summary>
    public int NumDustLanes { get; set; }
    /// <summary>World-space vertical field of view used to set camera zoom.</summary>
    public float FieldOfView { get; set; } = 33960f;
    /// <summary>Disc inclination from face-on, degrees (0 = face-on, ~58 ≈ M81).</summary>
    public float InclinationDeg { get; set; }
    /// <summary>In-plane view rotation after inclination, degrees.</summary>
    public float ViewRotationDeg { get; set; }
    /// <summary>Photographic grading: cream bulge, stronger pink H2, blue arms.</summary>
    public bool PhotoLook { get; set; }
    /// <summary>Static distant field stars drawn behind the galaxy.</summary>
    public int NumBackgroundStars { get; set; } = 8000;
    public uint Seed { get; set; } = 42;

    public float RadFarField => RadGalaxy * 2f;

    public GalaxyParams Clone() => (GalaxyParams)MemberwiseClone();
}
