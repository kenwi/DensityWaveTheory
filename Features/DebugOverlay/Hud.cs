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
    All = Stars | Dust | Filaments | H2,
}

public sealed class Hud
{
    public DisplayFeatures Features { get; private set; } = DisplayFeatures.All;
    public VisualEffects Visuals { get; } = new();
    public bool ShowHelp { get; private set; } = true;

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
        if (Raylib.IsKeyPressed(KeyboardKey.G))
            Visuals.RadialPalette = !Visuals.RadialPalette;
        if (Raylib.IsKeyPressed(KeyboardKey.B))
            Visuals.SoftGlow = !Visuals.SoftGlow;
        if (Raylib.IsKeyPressed(KeyboardKey.LeftBracket))
            Visuals.GlowStrength = MathF.Max(0.5f, Visuals.GlowStrength - 0.15f);
        if (Raylib.IsKeyPressed(KeyboardKey.RightBracket))
            Visuals.GlowStrength = MathF.Min(3f, Visuals.GlowStrength + 0.15f);
        if (Raylib.IsKeyPressed(KeyboardKey.H))
            ShowHelp = !ShowHelp;
    }

    private void Toggle(DisplayFeatures flag) => Features ^= flag;

    public void Draw(
        GalaxyParams p,
        int particleCount,
        float timeYears,
        float simSpeed,
        bool paused,
        bool wavesVisible,
        bool darkMatter)
    {
        var fps = Raylib.GetFPS();
        var y = 10;
        void Line(string text)
        {
            Raylib.DrawText(text, 10, y, 16, Color.RayWhite);
            y += 20;
        }

        Line($"FPS {fps}  particles {particleCount:N0}");
        Line($"t = {timeYears:F1} yr   speed x{simSpeed:0.##}{(paused ? "  PAUSED" : "")}");
        Line($"core {p.RadCore:0}  disk {p.RadGalaxy:0}  offset {p.AngleOffset:0.####}");
        Line($"dark matter {(darkMatter ? "on" : "off")}  waves {(wavesVisible ? "on" : "off")}");
        Line($"palette {(Visuals.RadialPalette ? "on" : "off")}  soft glow {(Visuals.SoftGlow ? "on" : "off")} x{Visuals.GlowStrength:0.00}");
        Line($"features: {Features}");

        if (!ShowHelp)
            return;

        y += 10;
        Line("Space pause  +/- speed  0 reset speed");
        Line("RMB/MMB pan  wheel zoom");
        Line("1-4 toggle stars/dust/filaments/H2");
        Line("G radial palette  B soft glow  [/] glow strength");
        Line("F2 density waves  F3 dark matter  F5 Sb preset  H help");
    }
}
