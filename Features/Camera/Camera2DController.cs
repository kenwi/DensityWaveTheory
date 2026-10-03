using System.Numerics;
using Raylib_cs;

namespace DensityWaveTheory.Features.Camera;

public sealed class Camera2DController
{
    public Camera2D Camera;
    public float SimSpeed { get; private set; } = 1f;
    public bool Paused { get; private set; }
    public float FieldOfView { get; private set; } = 33960f;

    public Camera2DController(int screenWidth, int screenHeight, float fieldOfView = 33960f)
    {
        FieldOfView = fieldOfView;
        Camera = new Camera2D
        {
            Target = Vector2.Zero,
            Offset = new Vector2(screenWidth / 2f, screenHeight / 2f),
            Rotation = 0f,
            // Reference AdjustCamera: vertical span = FOV, so zoom = height / FOV.
            Zoom = screenHeight / fieldOfView,
        };
    }

    public void SetFieldOfView(float fieldOfView, int screenWidth, int screenHeight)
    {
        FieldOfView = Math.Clamp(fieldOfView, 1000f, 60000f);
        Camera.Offset = new Vector2(screenWidth / 2f, screenHeight / 2f);
        Camera.Zoom = screenHeight / FieldOfView;
        Camera.Target = Vector2.Zero;
    }

    public void HandleResize(int screenWidth, int screenHeight)
    {
        Camera.Offset = new Vector2(screenWidth / 2f, screenHeight / 2f);
        Camera.Zoom = screenHeight / FieldOfView;
    }

    public void Update()
    {
        var wheel = Raylib.GetMouseWheelMove();
        if (wheel != 0)
        {
            // Match reference: FOV *= 0.9^wheel (wheel up zooms in).
            FieldOfView *= MathF.Pow(0.9f, wheel);
            FieldOfView = Math.Clamp(FieldOfView, 1000f, 60000f);
            Camera.Zoom = Raylib.GetScreenHeight() / FieldOfView;
        }

        var keyZoom = 0f;
        if (Raylib.IsKeyDown(KeyboardKey.Z) || Raylib.IsKeyDown(KeyboardKey.PageUp))
            keyZoom += 1f;
        if (Raylib.IsKeyDown(KeyboardKey.X) || Raylib.IsKeyDown(KeyboardKey.PageDown))
            keyZoom -= 1f;
        if (keyZoom != 0f)
        {
            FieldOfView *= MathF.Pow(0.9f, keyZoom * Raylib.GetFrameTime() * 8f);
            FieldOfView = Math.Clamp(FieldOfView, 1000f, 60000f);
            Camera.Zoom = Raylib.GetScreenHeight() / FieldOfView;
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

        if (Raylib.IsKeyPressed(KeyboardKey.R))
        {
            Camera.Target = Vector2.Zero;
            SetFieldOfView(FieldOfView, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
        }
    }

    public void SetPaused(bool paused) => Paused = paused;
}
