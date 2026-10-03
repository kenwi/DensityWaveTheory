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
    private int _locRenderPass;

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
        _locRenderPass = ComputeProgram.GetUniformLocation(_program, "renderPass");

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

        Rlgl.BindShaderBuffer(drawSsbo, 1);
        Rlgl.EnableVertexArray(_vao);
        ComputeProgram.EnableProgramPointSize();

        // Pass 0: luminous stars/dust/H2 (additive).
        ComputeProgram.SetUniformInt(_locRenderPass, 0);
        ComputeProgram.SetAdditiveBlend();
        ComputeProgram.DrawPoints(0, particleCount);

        // Pass 1: dust lanes multiply-darken (soft brown veil, no black punch-outs).
        ComputeProgram.SetUniformInt(_locRenderPass, 1);
        ComputeProgram.SetMultiplyBlend();
        ComputeProgram.DrawPoints(0, particleCount);

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
