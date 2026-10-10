using System.Numerics;
using DensityWaveTheory.Shared;
using Raylib_cs;

namespace DensityWaveTheory.Features.StarSystem;

/// <summary>Dedicated moon view (planet / star system not retained while active).</summary>
public sealed class MoonScene
{
    private MoonFocus? _focus;
    private Camera2D _camera;
    private float _fov = 4f;

    public bool Active => _focus is not null;
    public MoonFocus? Focus => _focus;

    public static float VisualMoonRadius(float radiusEarth) =>
        Math.Clamp(1.1f + 0.55f * radiusEarth, 1.2f, 2.8f);

    public void Enter(MoonFocus focus, int screenWidth, int screenHeight)
    {
        _focus = focus;
        var moonR = VisualMoonRadius(focus.Moon.RadiusEarth);
        _fov = Math.Clamp(moonR * 3.4f, 3f, 14f);

        _camera = new Camera2D
        {
            Target = Vector2.Zero,
            Offset = new Vector2(screenWidth / 2f, screenHeight / 2f),
            Rotation = 0f,
            Zoom = screenHeight / _fov,
        };
    }

    public bool TryExit(out PlanetResume resume)
    {
        resume = default;
        if (_focus is null)
            return false;

        resume = _focus.Resume;
        _focus.Moon.Surface?.Dispose();
        _focus.Moon.Surface = null;
        _focus = null;
        return true;
    }

    public void HandleResize(int screenWidth, int screenHeight)
    {
        if (_focus is null)
            return;
        _camera.Offset = new Vector2(screenWidth / 2f, screenHeight / 2f);
        _camera.Zoom = screenHeight / _fov;
    }

    public void Update(float dt, float simSpeed, bool paused)
    {
        if (_focus is null)
            return;
        _ = (dt, simSpeed, paused);

        var wheel = Raylib.GetMouseWheelMove();
        if (wheel != 0)
        {
            _fov *= MathF.Pow(ViewControls.ZoomStepFactor, wheel);
            _fov = Math.Clamp(_fov, 1.5f, 20f);
            _camera.Zoom = Raylib.GetScreenHeight() / _fov;
        }

        var keyZoom = 0f;
        if (Raylib.IsKeyDown(KeyboardKey.Z) || Raylib.IsKeyDown(KeyboardKey.PageUp))
            keyZoom += 1f;
        if (Raylib.IsKeyDown(KeyboardKey.X) || Raylib.IsKeyDown(KeyboardKey.PageDown))
            keyZoom -= 1f;
        if (keyZoom != 0f)
        {
            _fov *= MathF.Pow(ViewControls.ZoomStepFactor, keyZoom * dt * ViewControls.KeyZoomRate);
            _fov = Math.Clamp(_fov, 1.5f, 20f);
            _camera.Zoom = Raylib.GetScreenHeight() / _fov;
        }

        if (Raylib.IsMouseButtonDown(MouseButton.Right) || Raylib.IsMouseButtonDown(MouseButton.Middle))
        {
            var delta = Raylib.GetMouseDelta();
            _camera.Target -= delta / _camera.Zoom;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.R))
        {
            _camera.Target = Vector2.Zero;
            _camera.Zoom = Raylib.GetScreenHeight() / _fov;
        }
    }

    public void Draw(bool softGlow = true)
    {
        if (_focus is null)
            return;

        var moon = _focus.Moon;
        var moonR = VisualMoonRadius(moon.RadiusEarth);

        Raylib.BeginMode2D(_camera);

        Raylib.BeginBlendMode(BlendMode.Additive);
        var wash = softGlow
            ? new Color(moon.Color.R, moon.Color.G, moon.Color.B, (byte)22)
            : new Color(moon.Color.R, moon.Color.G, moon.Color.B, (byte)12);
        SoftDisc.Draw(Vector2.Zero, _fov * 0.65f, wash, 1);
        Raylib.EndBlendMode();

        if (moon.Surface is not null)
            moon.Surface.Draw(Vector2.Zero, moonR);
        else
            Raylib.DrawCircleV(Vector2.Zero, moonR, moon.Color);

        Raylib.EndMode2D();
    }
}
