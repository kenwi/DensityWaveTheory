namespace DensityWaveTheory.Shared;

public static class Constants
{
    public const float PcToKm = 3.08567758129e13f;
    public const float SecPerYear = 365.25f * 86400f;
    public const float DegToRad = MathF.PI / 180f;
    public const float RadToDeg = 180f / MathF.PI;
    public const float ConstantOfGravity = 6.672e-11f;
    public const float Pi = MathF.PI;

    // OpenGL / rlgl shader stage enums
    public const int GlVertexShader = 0x8B31;
    public const int GlFragmentShader = 0x8B30;
    public const int GlComputeShader = 0x91B9;

    // rlSetUniform types (from rlgl.h)
    public const int ShaderUniformFloat = 0;
    public const int ShaderUniformVec2 = 1;
    public const int ShaderUniformVec3 = 2;
    public const int ShaderUniformVec4 = 3;
    public const int ShaderUniformInt = 4;
    public const int ShaderUniformIvec2 = 5;
    public const int ShaderUniformIvec3 = 6;
    public const int ShaderUniformIvec4 = 7;

    public const int GlPoints = 0x0000;
    public const int GlShaderStorageBarrierBit = 0x00002000;
    public const int GlProgramPointSize = 0x8642;
}
