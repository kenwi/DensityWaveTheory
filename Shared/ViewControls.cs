namespace DensityWaveTheory.Shared;

/// <summary>Shared zoom / view defaults used by galaxy camera and nested system scenes.</summary>
public static class ViewControls
{
    /// <summary>Default world-space vertical FOV for face-on galaxy presets.</summary>
    public const float DefaultGalaxyFieldOfView = 33960f;

    /// <summary>FOV multiplier per mouse-wheel notch (match beltoforion: FOV *= factor^wheel).</summary>
    public const float ZoomStepFactor = 0.9f;

    /// <summary>Key-zoom speed scale: FOV *= ZoomStepFactor^(dir * dt * KeyZoomRate).</summary>
    public const float KeyZoomRate = 8f;
}
