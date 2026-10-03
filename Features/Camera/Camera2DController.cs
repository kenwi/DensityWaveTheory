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
            Zoom = 0.055f,
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
        {
            var mouseWorld = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), Camera);
            Camera.Zoom = Math.Clamp(Camera.Zoom * (1f + wheel * 0.1f), 0.005f, 2f);
            var mouseWorldAfter = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), Camera);
            Camera.Target += mouseWorld - mouseWorldAfter;
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
}
