using System.Diagnostics;

namespace DensityWaveTheory.Features.StarSystem;

public static class SystemCapture
{
    public static string DefaultPath()
    {
        var dir = Path.Combine(Directory.GetCurrentDirectory(), "screenshots");
        Directory.CreateDirectory(dir);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        return Path.Combine(dir, $"star-system-{stamp}.png");
    }

    public static void EnsureParentDir(string path)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }

    /// <summary>Open an image with the desktop default viewer (best-effort).</summary>
    public static void OpenForViewing(string path)
    {
        var full = Path.GetFullPath(path);
        if (!File.Exists(full))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                ArgumentList = { full },
                UseShellExecute = false,
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Could not open screenshot viewer: {ex.Message}");
            Console.WriteLine($"Screenshot saved: {full}");
        }
    }
}
