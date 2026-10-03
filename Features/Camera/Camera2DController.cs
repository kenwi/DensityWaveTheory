using System.Numerics;
using Raylib_cs;

namespace DensityWaveTheory.Features.Camera;

public sealed class Camera2DController
{
    public Camera2D Camera;
    public float SimSpeed { get; private set; } = 1f;
    public bool Paused { get; private set; }

    public Camera2DController(int screenWidth, int screenHeight)
    {
        Camera = new Camera2D
        {
            Target = Vector2.Zero,
            Offset = new Vector2(screenWidth / 2f, screenHeight / 2f),
            Rotation = 0f,
            Zoom = 0.085f,
        };
    }

    public void HandleResize(int screenWidth, int screenHeight)
    {
        Camera.Offset = new Vector2(screenWidth / 2f, screenHeight / 2f);
    }

    public void Update()
    {
        var wheel = Raylib.GetMouseWheelMove();
        if (wheel != 0)
            ZoomAt(Raylib.GetMousePosition(), 1f + wheel * 0.1f);

        var keyZoom = 0f;
        if (Raylib.IsKeyDown(KeyboardKey.Z) || Raylib.IsKeyDown(KeyboardKey.PageUp))
            keyZoom += 1f;
        if (Raylib.IsKeyDown(KeyboardKey.X) || Raylib.IsKeyDown(KeyboardKey.PageDown))
            keyZoom -= 1f;
        if (keyZoom != 0f)
        {
            // Zoom toward screen center while holding keys.
            var factor = MathF.Pow(1.8f, keyZoom * Raylib.GetFrameTime());
            ZoomAt(Camera.Offset, factor);
        }

        if (Raylib.IsMouseButtonDown(MouseButton.Right) || Raylib.IsMouseButtonDown(MouseButton.Middle))
        {
            var delta = Raylib.GetMouseDelta();
            Camera.Target -= delta / Camera.Zoom;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Space))
            Paused = !Paused;

        if (Raylib.IsKeyPressed(KeyboardKey.Equal) || Raylib.IsKeyPressed(KeyboardKey.KpAdd))
            SimSpeed = MathF.Min(SimSpeed * 1.5f, 64f);

        if (Raylib.IsKeyPressed(KeyboardKey.Minus) || Raylib.IsKeyPressed(KeyboardKey.KpSubtract))
            SimSpeed = MathF.Max(SimSpeed / 1.5f, 0.05f);

        if (Raylib.IsKeyPressed(KeyboardKey.Zero))
            SimSpeed = 1f;
    }

    private void ZoomAt(Vector2 screenPivot, float factor)
    {
        var worldBefore = Raylib.GetScreenToWorld2D(screenPivot, Camera);
        Camera.Zoom = Math.Clamp(Camera.Zoom * factor, 0.005f, 2f);
        var worldAfter = Raylib.GetScreenToWorld2D(screenPivot, Camera);
        Camera.Target += worldBefore - worldAfter;
    }
}
