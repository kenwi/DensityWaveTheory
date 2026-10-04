using DensityWaveTheory.Features.GalaxyPopulation;
using DensityWaveTheory.Shared;
using Raylib_cs;

namespace DensityWaveTheory.Features.DebugOverlay;

[Flags]
public enum DisplayFeatures
{
    None = 0,
    Stars = 1,
    Dust = 2,
    Filaments = 4,
    H2 = 8,
    DustLanes = 16,
    All = Stars | Dust | Filaments | H2 | DustLanes,
}

public sealed class Hud
{
    private const double TimeRefreshSec = 0.25;

    public DisplayFeatures Features { get; private set; } = DisplayFeatures.All;
    public VisualEffects Visuals { get; } = new();
    public bool ShowOverlay { get; set; } = true;
    public bool ShowHelp { get; private set; }
    public bool StarfieldVisible { get; private set; } = true;
    public bool StarfieldBloom { get; private set; } = true;

    private double _nextTimeRefresh;
    private float _displayedTimeMyr;

    public void ApplyPhotoLook(bool photoLook)
    {
        Visuals.SoftGlow = photoLook;
        Visuals.GlowStrength = photoLook ? 1.0f : 1.0f;
    }

    public void Update()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.One))
            Toggle(DisplayFeatures.Stars);
        if (Raylib.IsKeyPressed(KeyboardKey.Two))
            Toggle(DisplayFeatures.Dust);
        if (Raylib.IsKeyPressed(KeyboardKey.Three))
            Toggle(DisplayFeatures.Filaments);
        if (Raylib.IsKeyPressed(KeyboardKey.Four))
            Toggle(DisplayFeatures.H2);
        if (Raylib.IsKeyPressed(KeyboardKey.Five))
            Toggle(DisplayFeatures.DustLanes);
        if (Raylib.IsKeyPressed(KeyboardKey.Six))
            StarfieldVisible = !StarfieldVisible;
        if (Raylib.IsKeyPressed(KeyboardKey.Seven))
            StarfieldBloom = !StarfieldBloom;
        if (Raylib.IsKeyPressed(KeyboardKey.G))
            Visuals.RadialPalette = !Visuals.RadialPalette;
        if (Raylib.IsKeyPressed(KeyboardKey.B))
            Visuals.SoftGlow = !Visuals.SoftGlow;
        if (Raylib.IsKeyPressed(KeyboardKey.LeftBracket))
            Visuals.GlowStrength = MathF.Max(0.4f, Visuals.GlowStrength - 0.1f);
        if (Raylib.IsKeyPressed(KeyboardKey.RightBracket))
            Visuals.GlowStrength = MathF.Min(2.5f, Visuals.GlowStrength + 0.1f);
        if (Raylib.IsKeyPressed(KeyboardKey.H))
        {
            if (!ShowOverlay)
            {
                ShowOverlay = true;
                ShowHelp = false;
            }
            else if (!ShowHelp)
            {
                ShowHelp = true;
            }
            else
            {
                ShowOverlay = false;
                ShowHelp = false;
            }
        }
    }

    private void Toggle(DisplayFeatures flag) => Features ^= flag;

    public void Draw(
        GalaxyParams p,
        int particleCount,
        float timeYears,
        float simSpeed,
        bool paused,
        bool wavesVisible,
        bool axisVisible,
        bool darkMatter,
        float fieldOfView)
    {
        if (!ShowOverlay)
            return;

        var now = Raylib.GetTime();
        if (now >= _nextTimeRefresh)
        {
            _displayedTimeMyr = timeYears / 1_000_000f;
            _nextTimeRefresh = now + TimeRefreshSec;
        }

        var fps = Raylib.GetFPS();
        var y = 10;
        void Line(string text)
        {
            Raylib.DrawText(text, 10, y, 16, Color.RayWhite);
            y += 20;
        }

        Line($"FPS {fps}  particles {particleCount:N0}");
        Line($"t = {_displayedTimeMyr:F2} Myr   speed x{simSpeed:0.##}{(paused ? "  PAUSED" : "")}");
        Line($"core {p.RadCore:0}  disk {p.RadGalaxy:0}  offset {p.AngleOffset:0.####}");
        Line($"fov {fieldOfView:0}  dust {p.DustRenderSize:0}  temp {p.BaseTemp:0}");
        Line($"incl {p.InclinationDeg:0}°  rot {p.ViewRotationDeg:0}°  photo {(p.PhotoLook ? "on" : "off")}");
        Line($"dark matter {(darkMatter ? "on" : "off")}  waves {(wavesVisible ? "on" : "off")}  scale {(axisVisible ? "on" : "off")}");
        Line($"palette {(Visuals.RadialPalette ? "on" : "off")}  soft glow {(Visuals.SoftGlow ? "on" : "off")} x{Visuals.GlowStrength:0.00}");
        Line($"field stars {(StarfieldVisible ? "on" : "off")}  star bloom {(StarfieldBloom ? "on" : "off")}");
        Line($"features: {Features}");

        if (!ShowHelp)
            return;

        y += 10;
        Line("Space pause  +/- speed  0 reset speed  R recenter");
        Line("arrows/RMB/MMB pan  wheel/Z/X/PgUp/PgDn zoom");
        Line("1-5 stars/dust/filaments/H2/dust-lanes");
        Line("6 field stars  7 star bloom");
        Line("G palette  B soft glow  [/] glow  I incline");
        Line("F2 waves  F4/A scale  F5 Sb  F6 face-on  F7 M81  H hud");
    }
}
