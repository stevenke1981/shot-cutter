using System.Diagnostics;

namespace ShotCutter.Core.Services;

public static class ToolPathResolver
{
    public static string ResolveFfmpegPath(string? configuredPath = null)
        => ResolveExecutablePath("ffmpeg", configuredPath);

    public static string ResolveFfprobePath(string? configuredPath = null)
        => ResolveExecutablePath("ffprobe", configuredPath);

    public static string? ResolveBrowserPath(string? configuredPath = null)
    {
        if (File.Exists(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        return null;
    }

    public static string ResolveExecutablePath(string toolName, string? configuredPath = null)
    {
        if (File.Exists(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        var executableName = toolName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? toolName
            : $"{toolName}.exe";

        foreach (var candidate in GetCandidatePaths(executableName))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var pathLookup = TryResolveFromPath(executableName);
        if (!string.IsNullOrWhiteSpace(pathLookup))
        {
            return pathLookup;
        }

        return executableName;
    }

    private static IEnumerable<string> GetCandidatePaths(string executableName)
    {
        yield return Path.Combine(AppContext.BaseDirectory, "tools", "ffmpeg", executableName);
        yield return Path.Combine(AppContext.BaseDirectory, executableName);

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            yield return Path.Combine(localAppData, "Microsoft", "WinGet", "Links", executableName);
        }
    }

    private static string? TryResolveFromPath(string executableName)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executableName,
                    Arguments = "-version",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            if (process.Start())
            {
                return executableName;
            }
        }
        catch
        {
            // Fall back to plain executable name below.
        }

        return null;
    }
}
