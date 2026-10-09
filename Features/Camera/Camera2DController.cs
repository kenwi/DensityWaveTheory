using System.Numerics;
using Raylib_cs;

namespace DensityWaveTheory.Features.Camera;

public sealed class Camera2DController
{
    private const float DefaultFieldOfView = 33960f;
    private const float MinFieldOfView = 10f;
    private const float MaxFieldOfView = 60000f;
    private const float ZoomStepFactor = 0.9f;
    private const float KeyZoomRate = 8f;
    private const float PanScreenHeightPerSecond = 0.5f;
    private const float DefaultSimSpeed = 1f;
    private const float MinSimSpeed = 0.0005f;
    private const float MaxSimSpeed = 64f;
    private const float SimSpeedStepFactor = 1.5f;

    public Camera2D Camera;
    public float SimSpeed { get; private set; } = DefaultSimSpeed;
    public bool Paused { get; private set; }
    public float FieldOfView { get; private set; } = DefaultFieldOfView;

    public Camera2DController(int screenWidth, int screenHeight, float fieldOfView = DefaultFieldOfView)
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
        FieldOfView = Math.Clamp(fieldOfView, MinFieldOfView, MaxFieldOfView);
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
            // Match reference: FOV *= ZoomStepFactor^wheel (wheel up zooms in).
            FieldOfView *= MathF.Pow(ZoomStepFactor, wheel);
            FieldOfView = Math.Clamp(FieldOfView, MinFieldOfView, MaxFieldOfView);
            Camera.Zoom = Raylib.GetScreenHeight() / FieldOfView;
        }

        var keyZoom = 0f;
        if (Raylib.IsKeyDown(KeyboardKey.Z) || Raylib.IsKeyDown(KeyboardKey.PageUp))
            keyZoom += 1f;
        if (Raylib.IsKeyDown(KeyboardKey.X) || Raylib.IsKeyDown(KeyboardKey.PageDown))
            keyZoom -= 1f;
        if (keyZoom != 0f)
        {
            FieldOfView *= MathF.Pow(ZoomStepFactor, keyZoom * Raylib.GetFrameTime() * KeyZoomRate);
            FieldOfView = Math.Clamp(FieldOfView, MinFieldOfView, MaxFieldOfView);
            Camera.Zoom = Raylib.GetScreenHeight() / FieldOfView;
        }

        if (Raylib.IsMouseButtonDown(MouseButton.Right) || Raylib.IsMouseButtonDown(MouseButton.Middle))
        {
            var delta = Raylib.GetMouseDelta();
            Camera.Target -= delta / Camera.Zoom;
        }

        var keyPan = Vector2.Zero;
        if (Raylib.IsKeyDown(KeyboardKey.Left))
            keyPan.X -= 1f;
        if (Raylib.IsKeyDown(KeyboardKey.Right))
            keyPan.X += 1f;
        if (Raylib.IsKeyDown(KeyboardKey.Up))
            keyPan.Y -= 1f;
        if (Raylib.IsKeyDown(KeyboardKey.Down))
            keyPan.Y += 1f;
        if (keyPan != Vector2.Zero)
        {
            // Screen-space pan rate scales with FOV via / zoom (faster when zoomed out).
            var screenSpeed = Raylib.GetScreenHeight() * PanScreenHeightPerSecond;
            var dir = Vector2.Normalize(keyPan);
            Camera.Target += dir * screenSpeed * Raylib.GetFrameTime() / Camera.Zoom;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Space))
            Paused = !Paused;

        if (Raylib.IsKeyPressed(KeyboardKey.Equal) || Raylib.IsKeyPressed(KeyboardKey.KpAdd))
            SimSpeed = MathF.Min(SimSpeed * SimSpeedStepFactor, MaxSimSpeed);

        if (Raylib.IsKeyPressed(KeyboardKey.Minus) || Raylib.IsKeyPressed(KeyboardKey.KpSubtract))
            SimSpeed = MathF.Max(SimSpeed / SimSpeedStepFactor, MinSimSpeed);

        if (Raylib.IsKeyPressed(KeyboardKey.Zero))
            SimSpeed = DefaultSimSpeed;

        if (Raylib.IsKeyPressed(KeyboardKey.R))
        {
            Camera.Target = Vector2.Zero;
            SetFieldOfView(FieldOfView, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
        }
    }

    public void SetPaused(bool paused) => Paused = paused;

    public void SetSimSpeed(float speed) => SimSpeed = Math.Clamp(speed, MinSimSpeed, MaxSimSpeed);

    public void MultiplySimSpeed(float factor)
    {
        if (factor > 1f)
            SimSpeed = MathF.Min(SimSpeed * factor, MaxSimSpeed);
        else if (factor > 0f)
            SimSpeed = MathF.Max(SimSpeed * factor, MinSimSpeed);
    }

    public void UpdateSimControlsOnly()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Space))
            Paused = !Paused;

        if (Raylib.IsKeyPressed(KeyboardKey.Equal) || Raylib.IsKeyPressed(KeyboardKey.KpAdd))
            MultiplySimSpeed(SimSpeedStepFactor);

        if (Raylib.IsKeyPressed(KeyboardKey.Minus) || Raylib.IsKeyPressed(KeyboardKey.KpSubtract))
            MultiplySimSpeed(1f / SimSpeedStepFactor);

        if (Raylib.IsKeyPressed(KeyboardKey.Zero))
            SimSpeed = DefaultSimSpeed;
    }
}
