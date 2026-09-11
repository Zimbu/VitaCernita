using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace VitaCernita.Core.Configuration;

/// <summary>
/// Resolves logical configuration file locations for VitaCernita across platforms,
/// handling standard XDG directories, home path expansion, and local fallbacks.
/// </summary>
public static class ConfigPathResolver
{
    public const string DefaultConfigFileName = "gmail.lua";
    public const string AppFolderName = "vitacernita";

    private static readonly string[] LocalSearchPaths = new[]
    {
        "config/gmail_filter.lua",
        "gmail_filter.lua",
        "config/config.lua"
    };

    /// <summary>
    /// Gets the default configuration file path for the current user and platform.
    /// Priority:
    /// - If customHome / customXdg are provided (useful for testing), use them.
    /// - On Unix/macOS: $XDG_CONFIG_HOME/vitacernita/gmail.lua, or ~/.config/vitacernita/gmail.lua.
    /// - On Windows: %APPDATA%/vitacernita/gmail.lua.
    /// </summary>
    public static string GetDefaultConfigPath(string? customHome = null, string? customXdg = null)
    {
        if (!string.IsNullOrEmpty(customXdg))
        {
            return Path.Combine(customXdg, AppFolderName, DefaultConfigFileName);
        }

        if (!string.IsNullOrEmpty(customHome))
        {
            return Path.Combine(customHome, ".config", AppFolderName, DefaultConfigFileName);
        }

        var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (!string.IsNullOrWhiteSpace(xdgConfigHome))
        {
            return Path.Combine(xdgConfigHome, AppFolderName, DefaultConfigFileName);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!string.IsNullOrWhiteSpace(appData))
            {
                return Path.Combine(appData, AppFolderName, DefaultConfigFileName);
            }
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".config", AppFolderName, DefaultConfigFileName);
    }

    /// <summary>
    /// Expands a leading tilde ('~') to the user's home directory.
    /// </summary>
    public static string ExpandHome(string path, string? customHome = null)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        if (path == "~")
        {
            return customHome ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        if (path.StartsWith("~/") || path.StartsWith("~\\"))
        {
            var home = customHome ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, path[2..]);
        }

        return path;
    }

    /// <summary>
    /// Returns the ordered candidate paths checked when resolving configuration without an explicit path.
    /// </summary>
    public static IReadOnlyList<string> GetCandidatePaths(string? customHome = null, string? customXdg = null)
    {
        var list = new List<string>
        {
            GetDefaultConfigPath(customHome, customXdg)
        };

        foreach (var local in LocalSearchPaths)
        {
            list.Add(local);
        }

        return list;
    }

    /// <summary>
    /// Resolves the configuration path. If an explicit path is provided, it is expanded and returned.
    /// Otherwise, candidates are searched in priority order. If none exist, the default path is returned.
    /// </summary>
    public static string ResolveConfigPath(string? explicitPath = null, string? customHome = null, string? customXdg = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return Path.GetFullPath(ExpandHome(explicitPath.Trim(), customHome));
        }

        var candidates = GetCandidatePaths(customHome, customXdg);
        foreach (var candidate in candidates)
        {
            var expanded = ExpandHome(candidate, customHome);
            if (File.Exists(expanded))
            {
                return Path.GetFullPath(expanded);
            }
        }

        return Path.GetFullPath(GetDefaultConfigPath(customHome, customXdg));
    }

    /// <summary>
    /// Ensures that the directory for the specified file path exists.
    /// </summary>
    public static void EnsureDirectoryExists(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }
}
