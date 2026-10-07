using System.Numerics;
using Raylib_cs;

namespace DensityWaveTheory.Features.StarSystem;

/// <summary>GPU texture for a procedurally painted planetary disc.</summary>
public sealed class BodySurface : IDisposable
{
    public Texture2D Texture { get; }
    private bool _disposed;

    public BodySurface(Texture2D texture) => Texture = texture;

    public void Draw(Vector2 center, float radiusAu, float rotationDegrees = 0f)
    {
        if (_disposed || Texture.Id == 0)
            return;

        var size = radiusAu * 2f;
        var src = new Rectangle(0, 0, Texture.Width, Texture.Height);
        var dest = new Rectangle(center.X, center.Y, size, size);
        Raylib.DrawTexturePro(Texture, src, dest, new Vector2(radiusAu, radiusAu), rotationDegrees, Color.White);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        if (Texture.Id != 0)
            Raylib.UnloadTexture(Texture);
    }
}
