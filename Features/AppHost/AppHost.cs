using DensityWaveTheory.Features.Camera;
using DensityWaveTheory.Features.DebugOverlay;
using DensityWaveTheory.Features.GalaxyPopulation;
using DensityWaveTheory.Features.OrbitSimulation;
using DensityWaveTheory.Features.ParticleRendering;
using Raylib_cs;

namespace DensityWaveTheory.Features.AppHost;

public sealed class AppHost : IDisposable
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 800;

    private readonly GalaxyGenerator _generator = new();
    private readonly Camera2DController _camera = new(ScreenWidth, ScreenHeight);
    private readonly OrbitSimulationPipeline _orbits = new();
    private readonly ParticleRenderPipeline _renderer = new();
    private readonly DensityWaveOverlay _waves = new();
    private readonly Hud _hud = new();

    private GalaxyParams _params = Presets.SpiralSbLite();
    private Star[] _stars = [];
    private float _timeYears;
    private bool _gpuReady;

    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.VSyncHint);
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "Density Wave Theory - Spiral Galaxy");
        Raylib.SetTargetFPS(60);

        try
        {
            _orbits.Initialize();
            _renderer.Initialize();
            _gpuReady = true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine("Falling back to CPU point rendering.");
            _gpuReady = false;
        }

        RebuildGalaxy();

        while (!Raylib.WindowShouldClose())
        {
            if (Raylib.IsWindowResized())
                _camera.HandleResize(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

            _camera.Update();
            _hud.Update();
            HandleHotkeys();

            if (!_camera.Paused)
                _timeYears += Raylib.GetFrameTime() * 200_000f * _camera.SimSpeed;

            if (_gpuReady)
                _orbits.Dispatch(_params, _timeYears, (int)_hud.Features);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(2, 2, 8, 255));

            Raylib.BeginMode2D(_camera.Camera);
            if (_gpuReady)
            {
                // Use the matrices Mode2D just installed so world units match overlays.
                var view = Rlgl.GetMatrixModelview();
                var proj = Rlgl.GetMatrixProjection();
                _renderer.Draw(_orbits.DrawSsbo, _orbits.ParticleCount, view, proj);
            }
            else
            {
                DrawCpuFallback();
            }

            _waves.Draw(_params, _params.PertN, _params.PertAmp);
            Raylib.EndMode2D();

            _hud.Draw(
                _params,
                _stars.Length,
                _timeYears,
                _camera.SimSpeed,
                _camera.Paused,
                _waves.Visible,
                _params.HasDarkMatter);

            if (!_gpuReady)
            {
                Raylib.DrawText(
                    "GPU compute unavailable - using CPU fallback. See native/build-raylib-gl43.sh",
                    10,
                    Raylib.GetScreenHeight() - 28,
                    16,
                    Color.Orange);
            }

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }

    private void HandleHotkeys()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.F2) || Raylib.IsKeyPressed(KeyboardKey.D))
            _waves.Visible = !_waves.Visible;

        if (Raylib.IsKeyPressed(KeyboardKey.F3))
        {
            _params.HasDarkMatter = !_params.HasDarkMatter;
            RebuildGalaxy();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.F5))
        {
            _params = Presets.SpiralSb();
            RebuildGalaxy();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.F6))
        {
            _params = Presets.SpiralSbLite();
            RebuildGalaxy();
        }
    }

    private void RebuildGalaxy()
    {
        _stars = _generator.Generate(_params);
        _timeYears = 0f;
        if (_gpuReady)
            _orbits.UploadStars(_stars);
    }

    private void DrawCpuFallback()
    {
        var maxDraw = Math.Min(_stars.Length, 25_000);
        for (var i = 0; i < maxDraw; i++)
        {
            ref readonly var s = ref _stars[i];
            if (s.Type != (int)ParticleType.Star)
                continue;

            var (x, y) = OrbitMath.CalcPos(
                s.A, s.B, s.Theta0, s.VelTheta, _timeYears, s.TiltAngle,
                _params.PertN, _params.PertAmp);
            var c = BlackbodyColor.FromTemperature(s.Temp);
            var color = new Color(
                (byte)Math.Clamp((int)(c.X * 255), 0, 255),
                (byte)Math.Clamp((int)(c.Y * 255), 0, 255),
                (byte)Math.Clamp((int)(c.Z * 255), 0, 255),
                (byte)180);
            Raylib.DrawCircleV(new System.Numerics.Vector2(x, y), 1.5f, color);
        }
    }

    public void Dispose()
    {
        _orbits.Dispose();
        _renderer.Dispose();
    }
}
