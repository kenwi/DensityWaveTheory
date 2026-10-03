using DensityWaveTheory.Features.GalaxyPopulation;
using DensityWaveTheory.Shared;
using DensityWaveTheory.Shared.Gpu;
using Raylib_cs;

namespace DensityWaveTheory.Features.OrbitSimulation;

public sealed unsafe class OrbitSimulationPipeline : IDisposable
{
    private const int LocalSize = 256;

    private uint _computeProgram;
    private uint _starSsbo;
    private uint _drawSsbo;
    private int _particleCount;
    private bool _ready;

    private int _locTime;
    private int _locCount;
    private int _locPertN;
    private int _locPertAmp;
    private int _locDustSize;
    private int _locSizeFactor;
    private int _locBrightnessFactor;
    private int _locRadCore;
    private int _locRadGalaxy;
    private int _locRadFarField;
    private int _locExInner;
    private int _locExOuter;
    private int _locAngleOffset;
    private int _locH2SizeMax;
    private int _locH2Threshold;
    private int _locDisplayFeatures;
    private int _locEnableRadialPalette;
    private int _locPaletteStrength;
    private int _locEnableSoftGlow;
    private int _locGlowStrength;
    private int _locInclinationDeg;
    private int _locViewRotationDeg;
    private int _locPhotoLook;

    public int ParticleCount => _particleCount;
    public uint DrawSsbo => _drawSsbo;
    public bool IsReady => _ready;

    public void Initialize()
    {
        var path = ComputeProgram.ResolveShaderPath(
            Path.Combine("Features", "OrbitSimulation", "Shaders", "orbit_compute.glsl"));
        _computeProgram = ComputeProgram.CompileComputeProgram(path);

        _locTime = ComputeProgram.GetUniformLocation(_computeProgram, "time");
        _locCount = ComputeProgram.GetUniformLocation(_computeProgram, "particleCount");
        _locPertN = ComputeProgram.GetUniformLocation(_computeProgram, "pertN");
        _locPertAmp = ComputeProgram.GetUniformLocation(_computeProgram, "pertAmp");
        _locDustSize = ComputeProgram.GetUniformLocation(_computeProgram, "dustSize");
        _locSizeFactor = ComputeProgram.GetUniformLocation(_computeProgram, "sizeFactor");
        _locBrightnessFactor = ComputeProgram.GetUniformLocation(_computeProgram, "brightnessFactor");
        _locRadCore = ComputeProgram.GetUniformLocation(_computeProgram, "radCore");
        _locRadGalaxy = ComputeProgram.GetUniformLocation(_computeProgram, "radGalaxy");
        _locRadFarField = ComputeProgram.GetUniformLocation(_computeProgram, "radFarField");
        _locExInner = ComputeProgram.GetUniformLocation(_computeProgram, "exInner");
        _locExOuter = ComputeProgram.GetUniformLocation(_computeProgram, "exOuter");
        _locAngleOffset = ComputeProgram.GetUniformLocation(_computeProgram, "angleOffset");
        _locH2SizeMax = ComputeProgram.GetUniformLocation(_computeProgram, "h2SizeMax");
        _locH2Threshold = ComputeProgram.GetUniformLocation(_computeProgram, "h2Threshold");
        _locDisplayFeatures = ComputeProgram.GetUniformLocation(_computeProgram, "displayFeatures");
        _locEnableRadialPalette = ComputeProgram.GetUniformLocation(_computeProgram, "enableRadialPalette");
        _locPaletteStrength = ComputeProgram.GetUniformLocation(_computeProgram, "paletteStrength");
        _locEnableSoftGlow = ComputeProgram.GetUniformLocation(_computeProgram, "enableSoftGlow");
        _locGlowStrength = ComputeProgram.GetUniformLocation(_computeProgram, "glowStrength");
        _locInclinationDeg = ComputeProgram.GetUniformLocation(_computeProgram, "inclinationDeg");
        _locViewRotationDeg = ComputeProgram.GetUniformLocation(_computeProgram, "viewRotationDeg");
        _locPhotoLook = ComputeProgram.GetUniformLocation(_computeProgram, "photoLook");

        _ready = true;
    }

    public void UploadStars(Star[] stars)
    {
        if (!_ready)
            throw new InvalidOperationException("OrbitSimulationPipeline not initialized.");

        UnloadBuffers();

        _particleCount = stars.Length;
        if (_particleCount == 0)
            return;

        _starSsbo = ComputeProgram.CreateSsbo<Star>(stars, Rlgl.STATIC_DRAW);
        var drawBytes = (uint)(_particleCount * sizeof(ParticleDraw));
        _drawSsbo = ComputeProgram.CreateEmptySsbo(drawBytes, Rlgl.DYNAMIC_COPY);
    }

    public void Dispatch(
        GalaxyParams p,
        float timeYears,
        int displayFeatures,
        VisualEffects visuals,
        float sizeFactor = 1f,
        float brightnessFactor = 1f)
    {
        if (!_ready || _particleCount == 0)
            return;

        Rlgl.EnableShader(_computeProgram);

        ComputeProgram.SetUniformFloat(_locTime, timeYears);
        ComputeProgram.SetUniformInt(_locCount, _particleCount);
        ComputeProgram.SetUniformInt(_locPertN, p.PertN);
        ComputeProgram.SetUniformFloat(_locPertAmp, p.PertAmp);
        ComputeProgram.SetUniformInt(_locDustSize, (int)p.DustRenderSize);
        ComputeProgram.SetUniformFloat(_locSizeFactor, sizeFactor);
        ComputeProgram.SetUniformFloat(_locBrightnessFactor, brightnessFactor);
        ComputeProgram.SetUniformFloat(_locRadCore, p.RadCore);
        ComputeProgram.SetUniformFloat(_locRadGalaxy, p.RadGalaxy);
        ComputeProgram.SetUniformFloat(_locRadFarField, p.RadFarField);
        ComputeProgram.SetUniformFloat(_locExInner, p.ExInner);
        ComputeProgram.SetUniformFloat(_locExOuter, p.ExOuter);
        ComputeProgram.SetUniformFloat(_locAngleOffset, p.AngleOffset);
        ComputeProgram.SetUniformFloat(_locH2SizeMax, p.H2SizeMax);
        ComputeProgram.SetUniformFloat(_locH2Threshold, p.H2Threshold);
        ComputeProgram.SetUniformInt(_locDisplayFeatures, displayFeatures);
        ComputeProgram.SetUniformInt(_locEnableRadialPalette, visuals.RadialPalette ? 1 : 0);
        ComputeProgram.SetUniformFloat(_locPaletteStrength, visuals.PaletteStrength);
        ComputeProgram.SetUniformInt(_locEnableSoftGlow, visuals.SoftGlow ? 1 : 0);
        ComputeProgram.SetUniformFloat(_locGlowStrength, visuals.GlowStrength);
        ComputeProgram.SetUniformFloat(_locInclinationDeg, p.InclinationDeg);
        ComputeProgram.SetUniformFloat(_locViewRotationDeg, p.ViewRotationDeg);
        ComputeProgram.SetUniformInt(_locPhotoLook, p.PhotoLook ? 1 : 0);

        Rlgl.BindShaderBuffer(_starSsbo, 0);
        Rlgl.BindShaderBuffer(_drawSsbo, 1);

        var groups = (uint)((_particleCount + LocalSize - 1) / LocalSize);
        Rlgl.ComputeShaderDispatch(groups, 1, 1);
        ComputeProgram.MemoryBarrierShaderStorage();

        Rlgl.DisableShader();
    }

    private void UnloadBuffers()
    {
        if (_starSsbo != 0)
        {
            Rlgl.UnloadShaderBuffer(_starSsbo);
            _starSsbo = 0;
        }

        if (_drawSsbo != 0)
        {
            Rlgl.UnloadShaderBuffer(_drawSsbo);
            _drawSsbo = 0;
        }

        _particleCount = 0;
    }

    public void Dispose()
    {
        UnloadBuffers();
        if (_computeProgram != 0)
        {
            Rlgl.UnloadShaderProgram(_computeProgram);
            _computeProgram = 0;
        }

        _ready = false;
    }
}
