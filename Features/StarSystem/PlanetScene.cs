using System.Numerics;
using Raylib_cs;

namespace DensityWaveTheory.Features.StarSystem;

/// <summary>Dedicated planet + moons view (star system not retained while active).</summary>
public sealed class PlanetScene
{
    private PlanetFocus? _focus;
    private Camera2D _camera;
    private float _fov = 8f;
    private float _timeYears;

    public bool Active => _focus is not null;
    public PlanetFocus? Focus => _focus;

    public static float VisualPlanetRadius(float radiusEarth) =>
        Math.Clamp(0.9f + 0.22f * radiusEarth, 1.0f, 3.2f);

    public void Enter(PlanetFocus focus, int screenWidth, int screenHeight)
    {
        _focus = focus;
        _timeYears = 0.15f;

        var planetR = VisualPlanetRadius(focus.Planet.RadiusEarth);
        var outer = planetR * 2.2f;
        foreach (var moon in focus.Planet.Moons)
            outer = Math.Max(outer, moon.VisualOrbitAu * 1.25f);
        _fov = Math.Clamp(outer * 2.3f, 3f, 28f);

        _camera = new Camera2D
        {
            Target = Vector2.Zero,
            Offset = new Vector2(screenWidth / 2f, screenHeight / 2f),
            Rotation = 0f,
            Zoom = screenHeight / _fov,
        };
    }

    public bool TryExit(out StarSystemResume resume)
    {
        resume = default;
        if (_focus is null)
            return false;

        resume = _focus.Resume;
        DisposeFocusSurfaces(_focus);
        _focus = null;
        return true;
    }

    public bool TryGetPlanetResume(out PlanetResume resume)
    {
        resume = default;
        if (_focus is null)
            return false;
        resume = new PlanetResume(_focus.Resume, _focus.PlanetIndex);
        return true;
    }

    /// <summary>Release planet-focus state so only moon focus remains in memory.</summary>
    public void Clear()
    {
        if (_focus is null)
            return;
        DisposeFocusSurfaces(_focus);
        _focus = null;
    }

    public bool TryPickMoon(Vector2 screenPos, out int moonIndex)
    {
        moonIndex = -1;
        if (_focus is null)
            return false;

        var bestDistSq = float.MaxValue;
        var best = -1;
        var planet = _focus.Planet;
        for (var i = 0; i < planet.Moons.Count; i++)
        {
            var moon = planet.Moons[i];
            var angle = moon.PhaseRadians +
                        MathF.Tau * (_timeYears / Math.Max(moon.PeriodYears, 0.0005f));
            var world = new Vector2(
                moon.VisualOrbitAu * MathF.Cos(angle),
                moon.VisualOrbitAu * MathF.Sin(angle));
            var screen = Raylib.GetWorldToScreen2D(world, _camera);
            var moonR = Math.Clamp(0.08f + 0.12f * moon.RadiusEarth, 0.07f, 0.45f);
            var radiusPx = Math.Max(moonR * _camera.Zoom, 12f) + 10f;
            var dx = screen.X - screenPos.X;
            var dy = screen.Y - screenPos.Y;
            var distSq = dx * dx + dy * dy;
            if (distSq > radiusPx * radiusPx || distSq >= bestDistSq)
                continue;
            bestDistSq = distSq;
            best = i;
        }

        if (best < 0)
            return false;
        moonIndex = best;
        return true;
    }

    private static void DisposeFocusSurfaces(PlanetFocus focus)
    {
        focus.Planet.Surface?.Dispose();
        focus.Planet.Surface = null;
        foreach (var moon in focus.Planet.Moons)
        {
            moon.Surface?.Dispose();
            moon.Surface = null;
        }
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

        if (!paused)
            _timeYears += dt * simSpeed * 0.45f;

        var wheel = Raylib.GetMouseWheelMove();
        if (wheel != 0)
        {
            _fov *= MathF.Pow(0.9f, wheel);
            _fov = Math.Clamp(_fov, 2f, 40f);
            _camera.Zoom = Raylib.GetScreenHeight() / _fov;
        }

        var keyZoom = 0f;
        if (Raylib.IsKeyDown(KeyboardKey.Z) || Raylib.IsKeyDown(KeyboardKey.PageUp))
            keyZoom += 1f;
        if (Raylib.IsKeyDown(KeyboardKey.X) || Raylib.IsKeyDown(KeyboardKey.PageDown))
            keyZoom -= 1f;
        if (keyZoom != 0f)
        {
            _fov *= MathF.Pow(0.9f, keyZoom * dt * 8f);
            _fov = Math.Clamp(_fov, 2f, 40f);
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

        var planet = _focus.Planet;
        var planetR = VisualPlanetRadius(planet.RadiusEarth);

        Raylib.BeginMode2D(_camera);

        // Soft local wash - no sibling planets / system dust retained.
        Raylib.BeginBlendMode(BlendMode.Additive);
        var wash = softGlow
            ? new Color(planet.Color.R, planet.Color.G, planet.Color.B, (byte)28)
            : new Color(planet.Color.R, planet.Color.G, planet.Color.B, (byte)16);
        SoftDisc.Draw(Vector2.Zero, _fov * 0.7f, wash, 1);
        Raylib.EndBlendMode();

        foreach (var moon in planet.Moons)
            Raylib.DrawCircleLinesV(Vector2.Zero, moon.VisualOrbitAu, new Color(180, 180, 200, 45));

        foreach (var moon in planet.Moons)
        {
            var angle = moon.PhaseRadians +
                        MathF.Tau * (_timeYears / Math.Max(moon.PeriodYears, 0.0005f));
            var pos = new Vector2(
                moon.VisualOrbitAu * MathF.Cos(angle),
                moon.VisualOrbitAu * MathF.Sin(angle));
            var moonR = Math.Clamp(0.08f + 0.12f * moon.RadiusEarth, 0.07f, 0.45f);
            if (moon.Surface is not null)
                moon.Surface.Draw(pos, moonR);
            else
                Raylib.DrawCircleV(pos, moonR, moon.Color);
        }

        if (planet.Surface is not null)
            planet.Surface.Draw(Vector2.Zero, planetR);
        else
            Raylib.DrawCircleV(Vector2.Zero, planetR, planet.Color);
        if (planet.InHabitableZone)
            Raylib.DrawCircleLinesV(Vector2.Zero, planetR * 1.12f, new Color(120, 255, 180, 160));

        Raylib.EndMode2D();
    }
}
