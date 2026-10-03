using System.Runtime.InteropServices;
using System.Text;
using DensityWaveTheory.Shared;
using Raylib_cs;

namespace DensityWaveTheory.Shared.Gpu;

/// <summary>
/// Thin wrappers around Rlgl compute / SSBO APIs.
/// </summary>
public static unsafe class ComputeProgram
{
    public static string ResolveShaderPath(string relativeToFeatures)
    {
        var baseDir = AppContext.BaseDirectory;
        var candidate = Path.Combine(baseDir, relativeToFeatures);
        if (File.Exists(candidate))
            return candidate;

        // Dev-time: running from project root / bin
        var walk = new DirectoryInfo(baseDir);
        while (walk != null)
        {
            candidate = Path.Combine(walk.FullName, relativeToFeatures);
            if (File.Exists(candidate))
                return candidate;
            walk = walk.Parent;
        }

        throw new FileNotFoundException($"Shader not found: {relativeToFeatures}");
    }

    public static uint CompileComputeProgram(string shaderPath)
    {
        var source = File.ReadAllText(shaderPath);
        var bytes = Encoding.UTF8.GetBytes(source + "\0");
        fixed (byte* ptr = bytes)
        {
            var shaderId = Rlgl.LoadShader((sbyte*)ptr, Constants.GlComputeShader);
            if (shaderId == 0)
            {
                throw new InvalidOperationException(
                    "Failed to compile compute shader. Rebuild native raylib with OpenGL 4.3 " +
                    "(see native/build-raylib-gl43.sh).");
            }

            var program = Rlgl.LoadShaderProgramCompute(shaderId);
            if (program == 0)
            {
                throw new InvalidOperationException(
                    "Failed to link compute program. Rebuild native raylib with OpenGL 4.3 " +
                    "(see native/build-raylib-gl43.sh).");
            }

            return program;
        }
    }

    public static uint CompileGraphicsProgram(string vertexPath, string fragmentPath)
    {
        var vs = File.ReadAllText(vertexPath) + "\0";
        var fs = File.ReadAllText(fragmentPath) + "\0";
        var vsBytes = Encoding.UTF8.GetBytes(vs);
        var fsBytes = Encoding.UTF8.GetBytes(fs);
        fixed (byte* vsPtr = vsBytes)
        fixed (byte* fsPtr = fsBytes)
        {
            var vsId = Rlgl.LoadShader((sbyte*)vsPtr, Constants.GlVertexShader);
            var fsId = Rlgl.LoadShader((sbyte*)fsPtr, Constants.GlFragmentShader);
            if (vsId == 0 || fsId == 0)
                throw new InvalidOperationException("Failed to compile graphics shaders.");

            var program = Rlgl.LoadShaderProgramEx(vsId, fsId);
            if (program == 0)
                throw new InvalidOperationException("Failed to link graphics program.");
            return program;
        }
    }

    public static uint CreateSsbo<T>(ReadOnlySpan<T> data, int usageHint) where T : unmanaged
    {
        var byteCount = (uint)(data.Length * sizeof(T));
        fixed (T* ptr = data)
        {
            return Rlgl.LoadShaderBuffer(byteCount, ptr, usageHint);
        }
    }

    public static uint CreateEmptySsbo(uint byteCount, int usageHint)
    {
        return Rlgl.LoadShaderBuffer(byteCount, null, usageHint);
    }

    public static void UpdateSsbo<T>(uint id, ReadOnlySpan<T> data) where T : unmanaged
    {
        var byteCount = (uint)(data.Length * sizeof(T));
        fixed (T* ptr = data)
        {
            Rlgl.UpdateShaderBuffer(id, ptr, byteCount, 0);
        }
    }

    public static int GetUniformLocation(uint program, string name)
    {
        var bytes = Encoding.UTF8.GetBytes(name + "\0");
        fixed (byte* ptr = bytes)
        {
            return Rlgl.GetLocationUniform(program, (sbyte*)ptr);
        }
    }

    public static void SetUniformFloat(int loc, float value)
    {
        if (loc < 0) return;
        Rlgl.SetUniform(loc, &value, Constants.ShaderUniformFloat, 1);
    }

    public static void SetUniformInt(int loc, int value)
    {
        if (loc < 0) return;
        Rlgl.SetUniform(loc, &value, Constants.ShaderUniformInt, 1);
    }

    public static void MemoryBarrierShaderStorage()
    {
        GlNative.MemoryBarrier(Constants.GlShaderStorageBarrierBit);
    }

    public static void EnableProgramPointSize()
    {
        GlNative.Enable(Constants.GlProgramPointSize);
    }

    public static void DisableProgramPointSize()
    {
        GlNative.Disable(Constants.GlProgramPointSize);
    }

    /// <summary>
    /// rlDrawVertexArray hardcodes GL_TRIANGLES; particles need GL_POINTS.
    /// </summary>
    public static void DrawPoints(int first, int count)
    {
        GlNative.DrawArrays(Constants.GlPoints, first, count);
    }
}

internal static partial class GlNative
{
    private const string Lib = "libGL.so.1";

    [LibraryImport(Lib, EntryPoint = "glMemoryBarrier")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void MemoryBarrier(int barriers);

    [LibraryImport(Lib, EntryPoint = "glEnable")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void Enable(int cap);

    [LibraryImport(Lib, EntryPoint = "glDisable")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void Disable(int cap);

    [LibraryImport(Lib, EntryPoint = "glDrawArrays")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void DrawArrays(int mode, int first, int count);
}
