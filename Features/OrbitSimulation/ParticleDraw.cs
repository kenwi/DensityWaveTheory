using System.Numerics;
using System.Runtime.InteropServices;

namespace DensityWaveTheory.Features.OrbitSimulation;

/// <summary>GPU draw record written by the orbit compute shader (32 bytes, std430).</summary>
[StructLayout(LayoutKind.Sequential)]
public struct ParticleDraw
{
    public Vector2 Pos;
    public float Size;
    public float Type;
    public Vector4 Color;
}
