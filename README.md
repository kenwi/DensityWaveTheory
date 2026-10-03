# Density Wave Theory - Spiral Galaxy

C# / Raylib port of [beltoforion's density-wave galaxy renderer](https://beltoforion.de/en/spiral_galaxy_renderer/).

Stars follow tilted elliptical orbits. A GLSL 430 **compute shader** evaluates positions into an SSBO each frame; point-sprite shaders draw stars, dust, filaments, and H-II regions.

## Requirements

- .NET 10 SDK
- GPU / driver with **OpenGL 4.3+**
- Native raylib built with `GRAPHICS_API_OPENGL_43` (stock Raylib-cs NuGet is OpenGL 3.3)

## Build native raylib (OpenGL 4.3)

Stock Raylib-cs ships an OpenGL 3.3 `libraylib.so`. Compute shaders need 4.3:

```bash
./native/build-raylib-gl43.sh
```

This clones raylib **6.0**, builds a shared library with `GRAPHICS_API_OPENGL_43`, and installs it into:

- `native/raylib-gl43/` (cached for rebuilds)
- `bin/Debug/net10.0/runtimes/linux-x64/native/` (where .NET actually loads `raylib`)

The `.csproj` copies `native/raylib-gl43/` into the RID native folder after every `dotnet build`.

## Run

Starts **fullscreen** by default (FOV-matched camera, axis overlay on, HUD off):

```bash
dotnet run -c Release
```

Capture a frame (relative path; Raylib prepends the working directory):

```bash
dotnet run -c Release -- --screenshot screenshots/out.png --after 2 --freeze 2400000
```

Optional flags: `--hud`, `--no-axis`, `--freeze <years>`, `--after <seconds>`.

If compute shaders are unavailable, the app falls back to a CPU star preview and prints build instructions.

## Controls

| Key | Action |
|-----|--------|
| Space | Pause |
| `+` / `-` | Simulation speed |
| `0` | Reset speed |
| `R` | Recenter |
| RMB / MMB | Pan |
| Wheel / `Z` / `X` / PgUp / PgDn | Zoom (adjusts FOV) |
| `1`-`4` | Toggle stars / dust / filaments / H2 |
| `G` | Toggle optional radial palette wash |
| `B` | Toggle soft Gaussian dust glow |
| `[` / `]` | Scale dust glow size |
| `A` / `F4` | Toggle scale lines (axis + ticks + labels) |
| `F2` / `D` | Density-wave overlay |
| `F3` | Toggle dark-matter rotation curve |
| `F5` | Full Sb preset (~200k+ particles) |
| `F6` | Galaxy 1 / article-look preset |
| `F11` | Toggle fullscreen |
| `H` | Cycle HUD / help |

## Architecture

Vertical slices under `Features/`:

- `AppHost` - composition root / main loop
- `Camera` - pan/zoom
- `GalaxyPopulation` - CDF, orbits, generation, presets
- `OrbitSimulation` - compute shader + SSBOs
- `ParticleRendering` - point sprites
- `DebugOverlay` - wave ellipses + HUD

`Shared/` holds constants and thin GPU helpers only.
