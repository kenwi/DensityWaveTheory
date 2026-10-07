using System.Numerics;
using DensityWaveTheory.Features.GalaxyPopulation;
using Raylib_cs;

namespace DensityWaveTheory.Features.StarSystem;

public sealed class StarSystemScene
{
    private PlanetarySystem? _system;
    private LocalDustField? _dust;
    private Camera2D _camera;
    private float _fovAu = 8f;
    private float _timeYears;
    private float _savedGalaxyFov;
    private Vector2 _savedGalaxyTarget;
    private Vector2 _starWorldPos;

    public bool Active => _system is not null;
    public PlanetarySystem? System => _system;

    public void Enter(
        PlanetarySystem system,
        Star[] galaxyStars,
        GalaxyParams galaxyParams,
        float galaxyTimeYears,
        Vector2 starWorldPos,
        float galaxyFov,
        Vector2 galaxyTarget,
        int screenWidth,
        int screenHeight)
    {
        _system = system;
        _timeYears = 0f;
        _savedGalaxyFov = galaxyFov;
        _savedGalaxyTarget = galaxyTarget;
        _starWorldPos = starWorldPos;

        var outer = 2f;
        foreach (var planet in system.Planets)
            outer = Math.Max(outer, planet.SemiMajorAu * (1f + planet.Eccentricity));
        outer = Math.Max(outer, system.HabitableZone.OuterAu * 1.15f);
        _fovAu = Math.Clamp(outer * 2.4f, 0.5f, 80f);

        _dust = LocalDustField.Sample(
            galaxyStars,
            galaxyParams,
            galaxyTimeYears,
            starWorldPos,
            system.Seed,
            _fovAu);

        _camera = new Camera2D
        {
            Target = Vector2.Zero,
            Offset = new Vector2(screenWidth / 2f, screenHeight / 2f),
            Rotation = 0f,
            Zoom = screenHeight / _fovAu,
        };
    }

    public bool TryExit(out float galaxyFov, out Vector2 galaxyTarget)
    {
        galaxyFov = _savedGalaxyFov;
        galaxyTarget = _savedGalaxyTarget;
        if (_system is null)
            return false;

        SurfaceGenerator.DisposeSystemSurfaces(_system);
        _system = null;
        _dust = null;
        return true;
    }

    public bool TryGetResume(out StarSystemResume resume)
    {
        resume = default;
        if (_system is null)
            return false;

        resume = new StarSystemResume(
            _system.Seed,
            _system.SourceStarIndex,
            _system.Star.TempKelvin,
            _starWorldPos,
            _savedGalaxyFov,
            _savedGalaxyTarget);
        return true;
    }

    /// <summary>Release star-system state so only planet focus remains in memory.</summary>
    public void Clear()
    {
        SurfaceGenerator.DisposeSystemSurfaces(_system);
        _system = null;
        _dust = null;
    }

    public bool TryPickPlanet(Vector2 screenPos, out int planetIndex)
    {
        planetIndex = -1;
        if (_system is null)
            return false;

        var bestDistSq = float.MaxValue;
        var best = -1;
        for (var i = 0; i < _system.Planets.Count; i++)
        {
            var planet = _system.Planets[i];
            var world = PlanetPosition(planet, _timeYears);
            var screen = Raylib.GetWorldToScreen2D(world, _camera);
            var radiusAu = Math.Clamp(0.035f + 0.018f * planet.RadiusEarth, 0.04f, 0.28f);
            var radiusPx = Math.Max(radiusAu * _camera.Zoom, 10f) + 8f;
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
        planetIndex = best;
        return true;
    }

    public void HandleResize(int screenWidth, int screenHeight)
    {
        if (_system is null)
            return;
        _camera.Offset = new Vector2(screenWidth / 2f, screenHeight / 2f);
        _camera.Zoom = screenHeight / _fovAu;
    }

    public void Update(float dt, float simSpeed, bool paused)
    {
        if (_system is null)
            return;

        if (!paused)
            _timeYears += dt * simSpeed * 0.35f;

        var wheel = Raylib.GetMouseWheelMove();
        if (wheel != 0)
        {
            _fovAu *= MathF.Pow(0.9f, wheel);
            _fovAu = Math.Clamp(_fovAu, 0.4f, 100f);
            _camera.Zoom = Raylib.GetScreenHeight() / _fovAu;
        }

        var keyZoom = 0f;
        if (Raylib.IsKeyDown(KeyboardKey.Z) || Raylib.IsKeyDown(KeyboardKey.PageUp))
            keyZoom += 1f;
        if (Raylib.IsKeyDown(KeyboardKey.X) || Raylib.IsKeyDown(KeyboardKey.PageDown))
            keyZoom -= 1f;
        if (keyZoom != 0f)
        {
            _fovAu *= MathF.Pow(0.9f, keyZoom * dt * 8f);
            _fovAu = Math.Clamp(_fovAu, 0.4f, 100f);
            _camera.Zoom = Raylib.GetScreenHeight() / _fovAu;
        }

        if (Raylib.IsMouseButtonDown(MouseButton.Right) || Raylib.IsMouseButtonDown(MouseButton.Middle))
        {
            var delta = Raylib.GetMouseDelta();
            _camera.Target -= delta / _camera.Zoom;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.R))
        {
            _camera.Target = Vector2.Zero;
            _camera.Zoom = Raylib.GetScreenHeight() / _fovAu;
        }
    }

    public void Draw(bool softGlow = true)
    {
        if (_system is null)
            return;

        var sys = _system;
        Raylib.BeginMode2D(_camera);

        _dust?.Draw(_fovAu, softGlow);
        DrawHabitableZone(sys.HabitableZone);
        DrawOrbits(sys);
        DrawStar(sys.Star);
        DrawPlanets(sys);

        Raylib.EndMode2D();
    }

    private static void DrawHabitableZone(HabitableZone hz)
    {
        var center = Vector2.Zero;
        // Filled ring only - DrawRingLines leaves a visible radial seam at 0°.
        Raylib.DrawRing(center, hz.InnerAu, hz.OuterAu, 0f, 360f, 128, new Color(80, 180, 120, 32));
    }

    private static void DrawOrbits(PlanetarySystem sys)
    {
        foreach (var planet in sys.Planets)
        {
            var a = planet.SemiMajorAu;
            var b = a * MathF.Sqrt(Math.Max(0f, 1f - planet.Eccentricity * planet.Eccentricity));
            var cx = -a * planet.Eccentricity;
            var color = new Color(200, 200, 220, 70);
            // Approximate ellipse with line segments in AU world space.
            Vector2? prev = null;
            const int segments = 96;
            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments * MathF.Tau;
                var p = new Vector2(cx + a * MathF.Cos(t), b * MathF.Sin(t));
                if (prev is Vector2 q)
                    Raylib.DrawLineV(q, p, color);
                prev = p;
            }
        }
    }

    private static void DrawStar(StellarModel star)
    {
        var lut = BlackbodyColor.FromTemperature(star.TempKelvin);
        var core = new Color(
            (byte)Math.Clamp((int)(lut.X * 255), 0, 255),
            (byte)Math.Clamp((int)(lut.Y * 255), 0, 255),
            (byte)Math.Clamp((int)(lut.Z * 255), 0, 255),
            (byte)255);

        // Visual radius in AU (exaggerated for visibility).
        var r = Math.Clamp(0.05f + 0.07f * star.RadiusSolar, 0.06f, 0.4f);

        Raylib.BeginBlendMode(BlendMode.Additive);
        SoftDisc.Draw(Vector2.Zero, r * 3.2f, new Color(core.R, core.G, core.B, (byte)50), 1);
        SoftDisc.Draw(Vector2.Zero, r * 1.5f, new Color(core.R, core.G, core.B, (byte)80), 1);
        Raylib.EndBlendMode();
        Raylib.DrawCircleV(Vector2.Zero, r, core);
        Raylib.DrawCircleV(Vector2.Zero, r * 0.4f, new Color((byte)255, (byte)255, (byte)255, (byte)200));
    }

    private void DrawPlanets(PlanetarySystem sys)
    {
        foreach (var planet in sys.Planets)
        {
            var pos = PlanetPosition(planet, _timeYears);
            // Disc size exaggerated vs true Earth radii so planets read at AU scale.
            var radius = Math.Clamp(0.035f + 0.018f * planet.RadiusEarth, 0.04f, 0.28f);

            DrawMoonOrbits(pos, planet);
            DrawMoons(pos, planet);

            if (planet.Surface is not null)
                planet.Surface.Draw(pos, radius);
            else
                Raylib.DrawCircleV(pos, radius, planet.Color);
            if (planet.InHabitableZone)
                Raylib.DrawCircleLinesV(pos, radius * 1.45f, new Color(120, 255, 180, 180));
        }
    }

    private void DrawMoonOrbits(Vector2 planetPos, Planet planet)
    {
        foreach (var moon in planet.Moons)
        {
            Raylib.DrawCircleLinesV(
                planetPos,
                moon.VisualOrbitAu,
                new Color(180, 180, 200, 35));
        }
    }

    private void DrawMoons(Vector2 planetPos, Planet planet)
    {
        foreach (var moon in planet.Moons)
        {
            var angle = moon.PhaseRadians +
                        MathF.Tau * (_timeYears / Math.Max(moon.PeriodYears, 0.0005f));
            var offset = new Vector2(
                moon.VisualOrbitAu * MathF.Cos(angle),
                moon.VisualOrbitAu * MathF.Sin(angle));
            var moonR = Math.Clamp(0.012f + 0.02f * moon.RadiusEarth, 0.012f, 0.07f);
            if (moon.Surface is not null)
                moon.Surface.Draw(planetPos + offset, moonR);
            else
                Raylib.DrawCircleV(planetPos + offset, moonR, moon.Color);
        }
    }

    public void SeekYears(float years) => _timeYears = Math.Max(0f, years);

    private static Vector2 PlanetPosition(Planet planet, float timeYears)
    {
        var mean = planet.PhaseRadians + MathF.Tau * (timeYears / Math.Max(planet.PeriodYears, 0.01f));
        var e = planet.Eccentricity;
        var a = planet.SemiMajorAu;
        var r = a * (1f - e * e) / (1f + e * MathF.Cos(mean));
        return new Vector2(r * MathF.Cos(mean), r * MathF.Sin(mean));
    }
}
