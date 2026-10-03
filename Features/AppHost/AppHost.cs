using DensityWaveTheory.Features.Camera;
using DensityWaveTheory.Features.DebugOverlay;
using DensityWaveTheory.Features.GalaxyPopulation;
using DensityWaveTheory.Features.OrbitSimulation;
using DensityWaveTheory.Features.ParticleRendering;
using Raylib_cs;

namespace DensityWaveTheory.Features.AppHost;

public sealed class AppHost : IDisposable
{
    private readonly GalaxyGenerator _generator = new();
    private readonly Camera2DController _camera;
    private readonly OrbitSimulationPipeline _orbits = new();
    private readonly ParticleRenderPipeline _renderer = new();
    private readonly DensityWaveOverlay _waves = new();
    private readonly AxisOverlay _axis = new();
    private readonly Hud _hud = new();

    private GalaxyParams _params = Presets.ReferenceGalaxy1();
    private Star[] _stars = [];
    private float _timeYears;
    private bool _gpuReady;

    private string? _screenshotPath;
    private float _screenshotAfterSec;
    private float _freezeAtYears = 2_400_000f;
    private float _elapsedSec;
    private bool _screenshotTaken;

    public AppHost(string[]? args = null)
    {
        _camera = new Camera2DController(1920, 1200, _params.FieldOfView);
        _axis.Visible = true;
        _hud.ApplyPhotoLook(_params.PhotoLook);
        ParseArgs(args ?? []);
    }

    private void ParseArgs(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--screenshot" && i + 1 < args.Length)
                _screenshotPath = args[++i];
            else if (args[i] == "--after" && i + 1 < args.Length)
                _screenshotAfterSec = float.Parse(args[++i]);
            else if (args[i] == "--freeze" && i + 1 < args.Length)
                _freezeAtYears = float.Parse(args[++i]);
            else if (args[i] == "--hud")
                _hud.ShowOverlay = true;
            else if (args[i] == "--no-axis")
                _axis.Visible = false;
            else if (args[i] == "--preset" && i + 1 < args.Length)
            {
                var name = args[++i].ToLowerInvariant();
                _params = name switch
                {
                    "m81" => Presets.M81Approx(),
                    "sb" => Presets.SpiralSb(),
                    _ => Presets.ReferenceGalaxy1(),
                };
                _axis.Visible = !_params.PhotoLook && _axis.Visible;
                _hud.ApplyPhotoLook(_params.PhotoLook);
                _camera.SetFieldOfView(_params.FieldOfView, 1920, 1200);
            }
        }

        if (_screenshotPath is not null && _screenshotAfterSec <= 0f)
            _screenshotAfterSec = 2.5f;
    }

    public void Run()
    {
        Raylib.SetConfigFlags(
            ConfigFlags.FullscreenMode |
            ConfigFlags.VSyncHint);
        Raylib.InitWindow(0, 0, "Density Wave Theory - Spiral Galaxy");
        Raylib.SetTargetFPS(60);

        var w = Raylib.GetScreenWidth();
        var h = Raylib.GetScreenHeight();
        _camera.SetFieldOfView(_params.FieldOfView, w, h);

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
        // Start near a phase that shows clear arms + H2 ignition.
        _timeYears = _freezeAtYears;
        _camera.SetPaused(_screenshotPath is not null);
        Console.WriteLine($"GPU={_gpuReady} particles={_stars.Length} fov={_camera.FieldOfView:0} dust={_params.DustRenderSize}");

        while (!Raylib.WindowShouldClose())
        {
            _elapsedSec += Raylib.GetFrameTime();

            if (Raylib.IsWindowResized())
                _camera.HandleResize(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

            _camera.Update();
            _hud.Update();
            HandleHotkeys();

            if (!_camera.Paused)
                _timeYears += Raylib.GetFrameTime() * 200_000f * _camera.SimSpeed;

            if (_gpuReady)
                _orbits.Dispatch(_params, _timeYears, (int)_hud.Features, _hud.Visuals, sizeFactor: 1f);

            Raylib.BeginDrawing();
            // Reference article: (0,0,0.08). Photo look uses deeper black like deep-sky frames.
            Raylib.ClearBackground(_params.PhotoLook
                ? new Color(0, 0, 0, 255)
                : new Color(0, 0, 20, 255));

            Raylib.BeginMode2D(_camera.Camera);
            if (_gpuReady)
            {
                var view = Rlgl.GetMatrixModelview();
                var proj = Rlgl.GetMatrixProjection();
                _renderer.Draw(_orbits.DrawSsbo, _orbits.ParticleCount, view, proj, _hud.Visuals);
            }
            else
            {
                DrawCpuFallback();
            }

            _axis.DrawWorld(_camera.FieldOfView);
            _waves.Draw(_params, _params.PertN, _params.PertAmp);
            Raylib.EndMode2D();

            _axis.DrawLabels(_camera.Camera, _camera.FieldOfView);
            _hud.Draw(
                _params,
                _stars.Length,
                _timeYears,
                _camera.SimSpeed,
                _camera.Paused,
                _waves.Visible,
                _axis.Visible,
                _params.HasDarkMatter,
                _camera.FieldOfView);

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

            if (_screenshotPath is not null && !_screenshotTaken && _elapsedSec >= _screenshotAfterSec)
            {
                Raylib.TakeScreenshot(_screenshotPath);
                _screenshotTaken = true;
                Console.WriteLine($"Wrote screenshot {_screenshotPath}");
                break;
            }
        }

        Raylib.CloseWindow();
    }

    private void HandleHotkeys()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.F2) || Raylib.IsKeyPressed(KeyboardKey.D))
            _waves.Visible = !_waves.Visible;

        if (Raylib.IsKeyPressed(KeyboardKey.F4) || Raylib.IsKeyPressed(KeyboardKey.A))
            _axis.Visible = !_axis.Visible;

        if (Raylib.IsKeyPressed(KeyboardKey.F3))
        {
            _params.HasDarkMatter = !_params.HasDarkMatter;
            RebuildGalaxy();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.F5))
            ApplyPreset(Presets.SpiralSb());

        if (Raylib.IsKeyPressed(KeyboardKey.F6))
            ApplyPreset(Presets.ReferenceGalaxy1());

        if (Raylib.IsKeyPressed(KeyboardKey.F7))
            ApplyPreset(Presets.M81Approx());

        if (Raylib.IsKeyPressed(KeyboardKey.I))
        {
            // Cycle face-on → shallow → M81-like inclination.
            _params.InclinationDeg = _params.InclinationDeg switch
            {
                < 1f => 35f,
                < 45f => 58f,
                _ => 0f,
            };
        }

        if (Raylib.IsKeyPressed(KeyboardKey.F11))
            Raylib.ToggleFullscreen();
    }

    private void ApplyPreset(GalaxyParams preset)
    {
        _params = preset;
        _axis.Visible = !_params.PhotoLook;
        _hud.ApplyPhotoLook(_params.PhotoLook);
        RebuildGalaxy();
        _camera.SetFieldOfView(_params.FieldOfView, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
    }

    private void RebuildGalaxy()
    {
        _stars = _generator.Generate(_params);
        _timeYears = _freezeAtYears > 0 ? _freezeAtYears : 0f;
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
