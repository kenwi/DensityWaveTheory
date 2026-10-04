using DensityWaveTheory.Features.Camera;
using DensityWaveTheory.Features.DebugOverlay;
using DensityWaveTheory.Features.GalaxyPopulation;
using DensityWaveTheory.Features.OrbitSimulation;
using DensityWaveTheory.Features.ParticleRendering;
using DensityWaveTheory.Features.Starfield;
using DensityWaveTheory.Features.StarSystem;
using Raylib_cs;

namespace DensityWaveTheory.Features.AppHost;

public enum AppMode
{
    Galaxy,
    StarSystem,
}

public sealed class AppHost : IDisposable
{
    private readonly GalaxyGenerator _generator = new();
    private readonly Camera2DController _camera;
    private readonly OrbitSimulationPipeline _orbits = new();
    private readonly ParticleRenderPipeline _renderer = new();
    private readonly DensityWaveOverlay _waves = new();
    private readonly AxisOverlay _axis = new();
    private readonly Hud _hud = new();
    private readonly StarfieldRenderer _starfield = new();
    private readonly StarSystemScene _starSystem = new();

    private GalaxyParams _params = Presets.ReferenceGalaxy1();
    private Star[] _stars = [];
    private float _timeYears;
    private bool _gpuReady;
    private AppMode _mode = AppMode.Galaxy;
    private int _starSystemCount;
    private long _planetCount;

    private string? _screenshotPath;
    private float _screenshotAfterSec;
    private float _freezeAtYears = 2_400_000f;
    private float? _cliFov;
    private float _elapsedSec;
    private bool _screenshotTaken;
    private bool _randomSystemCli;
    private bool _openScreenshot = true;
    private bool _exitAfterScreenshot;
    private string? _pendingCapturePath;
    private float _pendingCaptureAt = -1f;
    private bool _pendingCaptureRestoreHud;
    private bool _hudBeforeCapture;

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
            else if (args[i] == "--random-system")
            {
                _randomSystemCli = true;
                _exitAfterScreenshot = true;
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    _screenshotPath = args[++i];
                else
                    _screenshotPath = SystemCapture.DefaultPath();
            }
            else if (args[i] == "--after" && i + 1 < args.Length)
                _screenshotAfterSec = float.Parse(args[++i]);
            else if (args[i] == "--freeze" && i + 1 < args.Length)
                _freezeAtYears = float.Parse(args[++i]);
            else if (args[i] == "--hud")
                _hud.ShowOverlay = true;
            else if (args[i] == "--no-hud")
                _hud.ShowOverlay = false;
            else if (args[i] == "--no-axis")
                _axis.Visible = false;
            else if (args[i] == "--no-open")
                _openScreenshot = false;
            else if (args[i] == "--fov" && i + 1 < args.Length)
                _cliFov = float.Parse(args[++i]);
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

        if (_cliFov is float fov)
            _camera.SetFieldOfView(fov, 1920, 1200);

        // Clean capture frames unless --hud was requested explicitly.
        if (_screenshotPath is not null && !args.Contains("--hud"))
            _hud.ShowOverlay = false;

        if (_screenshotPath is not null && _screenshotAfterSec <= 0f)
            _screenshotAfterSec = _randomSystemCli ? 0.75f : 2.5f;
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
        _camera.SetFieldOfView(_cliFov ?? _params.FieldOfView, w, h);

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

        if (_randomSystemCli)
        {
            if (EnterRandomStarSystem(Random.Shared))
            {
                ScheduleSystemCapture(_screenshotPath!, openAfter: _openScreenshot, exitAfter: true);
                Console.WriteLine($"Random star system capture -> {_screenshotPath}");
            }
            else
            {
                Console.Error.WriteLine("No stars available for --random-system.");
                Raylib.CloseWindow();
                return;
            }
        }

        while (!Raylib.WindowShouldClose())
        {
            var dt = Raylib.GetFrameTime();
            _elapsedSec += dt;

            if (Raylib.IsWindowResized())
            {
                var sw = Raylib.GetScreenWidth();
                var sh = Raylib.GetScreenHeight();
                _camera.HandleResize(sw, sh);
                _starSystem.HandleResize(sw, sh);
            }

            if (_mode == AppMode.Galaxy)
                _camera.Update();
            else
                _camera.UpdateSimControlsOnly();

            _hud.Update();
            HandleHotkeys();
            HandleStarSystemInput();

            if (_mode == AppMode.StarSystem)
            {
                _starSystem.Update(dt, _camera.SimSpeed, _camera.Paused);
            }
            else
            {
                if (!_camera.Paused)
                    _timeYears += dt * 200_000f * _camera.SimSpeed;

                if (_gpuReady)
                {
                    // Point sizes are in pixels; packing density scales with zoom^2.
                    // Scale size with zoom (clamped). Residual intensity uses a soft
                    // power - full ^2 over-brightens the saturated core when zoomed in.
                    var zoomRatio = _params.FieldOfView / Math.Max(_camera.FieldOfView, 1f);
                    var sizeFactor = Math.Clamp(zoomRatio, 0.4f, 3.25f);
                    var residual = zoomRatio / sizeFactor;
                    var brightnessFactor = Math.Clamp(MathF.Pow(residual, 1.25f), 0.25f, 6f);
                    _orbits.Dispatch(
                        _params,
                        _timeYears,
                        (int)_hud.Features,
                        _hud.Visuals,
                        sizeFactor,
                        brightnessFactor);
                }
            }

            Raylib.BeginDrawing();
            // Reference article: (0,0,0.08). Photo look uses deeper black like deep-sky frames.
            Raylib.ClearBackground(_params.PhotoLook
                ? new Color(0, 0, 0, 255)
                : new Color(0, 0, 20, 255));

            if (_mode == AppMode.StarSystem)
            {
                _starSystem.Draw(_hud.Visuals.SoftGlow);
            }
            else
            {
                Raylib.BeginMode2D(_camera.Camera);
                if (_hud.StarfieldVisible)
                    _starfield.Draw(_camera.Camera.Zoom, _hud.StarfieldBloom);
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
            }

            _hud.Draw(
                _params,
                _stars.Length,
                _timeYears,
                _camera.SimSpeed,
                _camera.Paused,
                _waves.Visible,
                _axis.Visible,
                _params.HasDarkMatter,
                _camera.FieldOfView,
                _mode == AppMode.StarSystem ? _starSystem.System : null,
                _starSystemCount,
                _planetCount);

            if (!_gpuReady && _mode == AppMode.Galaxy)
            {
                Raylib.DrawText(
                    "GPU compute unavailable - using CPU fallback. See native/build-raylib-gl43.sh",
                    10,
                    Raylib.GetScreenHeight() - 28,
                    16,
                    Color.Orange);
            }

            Raylib.EndDrawing();

            if (TryFinishPendingCapture())
                break;

            if (!_randomSystemCli &&
                _screenshotPath is not null &&
                !_screenshotTaken &&
                _elapsedSec >= _screenshotAfterSec)
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
        if (Raylib.IsKeyPressed(KeyboardKey.F11))
            Raylib.ToggleFullscreen();

        // F8: jump to a random system, screenshot it, and open the image.
        if (Raylib.IsKeyPressed(KeyboardKey.F8))
        {
            if (EnterRandomStarSystem(Random.Shared))
            {
                var path = SystemCapture.DefaultPath();
                ScheduleSystemCapture(path, openAfter: true, exitAfter: false);
                Console.WriteLine($"Capturing random star system -> {path}");
            }
        }

        // Galaxy-only hotkeys while exploring a system would mutate culled state.
        if (_mode != AppMode.Galaxy)
            return;

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
    }

    private void HandleStarSystemInput()
    {
        if (_mode == AppMode.StarSystem)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Escape) &&
                _starSystem.TryExit(out var fov, out var target))
            {
                _mode = AppMode.Galaxy;
                Raylib.SetExitKey(KeyboardKey.Escape);
                _camera.SetFieldOfView(fov, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
                var cam = _camera.Camera;
                cam.Target = target;
                _camera.Camera = cam;
            }

            return;
        }

        if (!Raylib.IsMouseButtonPressed(MouseButton.Left))
            return;

        var mouse = Raylib.GetMousePosition();
        if (!StarPicker.TryPick(_stars, _params, _timeYears, _camera.Camera, mouse, out var picked))
            return;

        EnterStarSystem(picked);
    }

    private bool EnterRandomStarSystem(Random rng)
    {
        if (!StarPicker.TryPickRandom(_stars, _params, _timeYears, rng, out var picked))
            return false;
        EnterStarSystem(picked);
        // Advance local clock so orbits aren't frozen at t=0 for captures.
        _starSystem.SeekYears(1.8f + (float)rng.NextDouble() * 4f);
        return true;
    }

    private void EnterStarSystem(PickedStar picked)
    {
        var seed = PlanetarySystemGenerator.MakeSeed(
            _params.Seed, picked.Index, picked.A, picked.Theta0, picked.Temp);
        var system = PlanetarySystemGenerator.Generate(seed, picked.Index, picked.Temp);

        // If already in a system, keep the original galaxy camera restore target.
        float galaxyFov;
        System.Numerics.Vector2 galaxyTarget;
        if (_mode == AppMode.StarSystem && _starSystem.Active)
        {
            // Peek saved restore values by exiting then re-entering would lose them;
            // Enter overwrites saved FOV/target, so pass current saved via TryExit first.
            _starSystem.TryExit(out galaxyFov, out galaxyTarget);
        }
        else
        {
            galaxyFov = _camera.FieldOfView;
            galaxyTarget = _camera.Camera.Target;
        }

        Raylib.SetExitKey(KeyboardKey.Null);
        _starSystem.Enter(
            system,
            _stars,
            _params,
            _timeYears,
            picked.WorldPos,
            galaxyFov,
            galaxyTarget,
            Raylib.GetScreenWidth(),
            Raylib.GetScreenHeight());
        _mode = AppMode.StarSystem;
    }

    private void ScheduleSystemCapture(string path, bool openAfter, bool exitAfter)
    {
        SystemCapture.EnsureParentDir(path);
        _pendingCapturePath = Path.GetFullPath(path);
        _pendingCaptureAt = _elapsedSec + Math.Max(_screenshotAfterSec, 0.35f);
        _exitAfterScreenshot = exitAfter;
        _openScreenshot = openAfter;
        _hudBeforeCapture = _hud.ShowOverlay;
        _hud.ShowOverlay = false;
        _pendingCaptureRestoreHud = true;
        _screenshotTaken = false;
    }

    private bool TryFinishPendingCapture()
    {
        if (_pendingCapturePath is null || _elapsedSec < _pendingCaptureAt || _screenshotTaken)
            return false;

        // Raylib prepends GetWorkingDirectory(); feed a relative path when possible.
        var cwd = Directory.GetCurrentDirectory();
        var path = _pendingCapturePath;
        if (path.StartsWith(cwd, StringComparison.Ordinal))
            path = Path.GetRelativePath(cwd, path);

        Raylib.TakeScreenshot(path);
        _screenshotTaken = true;
        Console.WriteLine($"Wrote screenshot {_pendingCapturePath}");

        if (_openScreenshot)
            SystemCapture.OpenForViewing(_pendingCapturePath);

        if (_pendingCaptureRestoreHud)
            _hud.ShowOverlay = _hudBeforeCapture;

        var exit = _exitAfterScreenshot;
        _pendingCapturePath = null;
        _pendingCaptureAt = -1f;
        return exit;
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
        (_starSystemCount, _planetCount) = PlanetarySystemGenerator.CountAll(_stars, _params.Seed);
        // Cover camera max zoom-out (FOV clamp 60000) with a little margin.
        var extent = 60000f * 1.15f;
        _starfield.Rebuild(_params.Seed, extent, _params.NumBackgroundStars);
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
