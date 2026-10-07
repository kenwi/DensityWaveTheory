namespace DensityWaveTheory.Features.StarSystem;

/// <summary>Identity to regenerate planet focus after leaving a moon close-up.</summary>
public readonly record struct PlanetResume(StarSystemResume SystemResume, int PlanetIndex);
