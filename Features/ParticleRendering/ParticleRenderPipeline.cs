using System.Numerics;
using DensityWaveTheory.Shared;
using DensityWaveTheory.Shared.Gpu;
using Raylib_cs;

namespace DensityWaveTheory.Features.ParticleRendering;

public sealed unsafe class ParticleRenderPipeline : IDisposable
{
    private uint _program;
    private uint _vao;
    private bool _ready;

    private int _locView;
    private int _locProj;
    private int _locEnableSoftGlow;
    private int _locGlowPass;
    private int _locGlowStrength;

    public bool IsReady => _ready;

    public void Initialize()
    {
        var vs = ComputeProgram.ResolveShaderPath(
            Path.Combine("Features", "ParticleRendering", "Shaders", "star.vs.glsl"));
        var fs = ComputeProgram.ResolveShaderPath(
            Path.Combine("Features", "ParticleRendering", "Shaders", "star.fs.glsl"));
        _program = ComputeProgram.CompileGraphicsProgram(vs, fs);

        _locView = ComputeProgram.GetUniformLocation(_program, "viewMat");
        _locProj = ComputeProgram.GetUniformLocation(_program, "projMat");
        _locEnableSoftGlow = ComputeProgram.GetUniformLocation(_program, "enableSoftGlow");
        _locGlowPass = ComputeProgram.GetUniformLocation(_program, "glowPass");
        _locGlowStrength = ComputeProgram.GetUniformLocation(_program, "glowStrength");

        _vao = Rlgl.LoadVertexArray();
        _ready = true;
    }

    public void Draw(uint drawSsbo, int particleCount, Matrix4x4 view, Matrix4x4 projection, VisualEffects visuals)
    {
        if (!_ready || particleCount <= 0 || drawSsbo == 0)
            return;

        Rlgl.DrawRenderBatchActive();

        Rlgl.EnableShader(_program);
        if (_locView >= 0)
            Rlgl.SetUniformMatrix(_locView, view);
        if (_locProj >= 0)
            Rlgl.SetUniformMatrix(_locProj, projection);

        ComputeProgram.SetUniformInt(_locEnableSoftGlow, visuals.SoftGlow ? 1 : 0);
        ComputeProgram.SetUniformFloat(_locGlowStrength, visuals.GlowStrength);

        Rlgl.BindShaderBuffer(drawSsbo, 1);
        Rlgl.EnableVertexArray(_vao);

        ComputeProgram.EnableProgramPointSize();
        Rlgl.EnableColorBlend();
        Rlgl.SetBlendFactors(Rlgl.SRC_ALPHA, Rlgl.ONE, Rlgl.FUNC_ADD);

        if (visuals.SoftGlow)
        {
            // Wide bloom halo first, then brighter cores on top.
            ComputeProgram.SetUniformInt(_locGlowPass, 1);
            ComputeProgram.DrawPoints(0, particleCount);
        }

        ComputeProgram.SetUniformInt(_locGlowPass, 0);
        ComputeProgram.DrawPoints(0, particleCount);

        Rlgl.SetBlendMode(BlendMode.Alpha);
        ComputeProgram.DisableProgramPointSize();
        Rlgl.DisableShader();
    }

    public void Dispose()
    {
        if (_program != 0)
        {
            Rlgl.UnloadShaderProgram(_program);
            _program = 0;
        }

        _ready = false;
    }
}
