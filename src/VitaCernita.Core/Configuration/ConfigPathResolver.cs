using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace VitaCernita.Core.Configuration;

/// <summary>
/// Resolves logical configuration and credential file locations for VitaCernita across platforms,
/// handling standard XDG directories, home path expansion, environment overrides, and local fallbacks.
/// Strictly isolates unit tests and test runs from live production (~/.config).
/// </summary>
public static class ConfigPathResolver
{
    public const string DefaultConfigFileName = "gmail.lua";
    public const string DefaultCredentialsFileName = "credentials.json";
    public const string DefaultTokenFolderName = "tokens";
    public const string AppFolderName = "vitacernita";
    public const string ConfigDirEnvVar = "VITACERNITA_CONFIG_DIR";
    public const string TestModeEnvVar = "VITACERNITA_TEST_MODE";

    private static readonly string[] LocalSearchPaths = new[]
    {
        "config/gmail_filter.lua",
        "gmail_filter.lua",
        "config/config.lua"
    };

    /// <summary>
    /// Detects whether code is running inside a unit test runner or test environment.
    /// In test mode, resolving paths without an explicit configDir will NEVER touch or read ~/.config.
    /// </summary>
    public static bool IsTestEnvironment
    {
        get
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(TestModeEnvVar)) ||
                !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ConfigDirEnvVar)))
            {
                return true;
            }

            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    var name = assemblies[i].GetName().Name;
                    if (name != null && (
                        name.StartsWith("VitaCernita.Tests", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("testhost", StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Fallback to false
            }

            return false;
        }
    }

    /// <summary>
    /// Gets the base configuration directory for VitaCernita.
    /// Priority:
    /// 1. customConfigDir parameter (if provided)
    /// 2. VITACERNITA_CONFIG_DIR environment variable (used to isolate test runs from live production config)
    /// 3. If in test environment: safe isolated temp directory (NEVER ~/.config)
    /// 4. customXdg or $XDG_CONFIG_HOME / vitacernita
    /// 5. customHome / .config / vitacernita
    /// 6. On Windows: %APPDATA% / vitacernita
    /// 7. On Unix/macOS: ~/.config / vitacernita
    /// </summary>
    public static string GetDefaultConfigDirectory(
        string? customHome = null,
        string? customXdg = null,
        string? customConfigDir = null)
    {
        if (!string.IsNullOrWhiteSpace(customConfigDir))
        {
            return Path.GetFullPath(ExpandHome(customConfigDir.Trim(), customHome));
        }

        var envOverride = Environment.GetEnvironmentVariable(ConfigDirEnvVar);
        if (!string.IsNullOrWhiteSpace(envOverride))
        {
            return Path.GetFullPath(ExpandHome(envOverride.Trim(), customHome));
        }

        // CRITICAL SAFETY GUARD: If running inside tests and no custom dir specified,
        // use an isolated temp directory to protect ~/.config (live production)
        if (IsTestEnvironment && string.IsNullOrEmpty(customHome) && string.IsNullOrEmpty(customXdg))
        {
            string isolatedTestDir = Path.Combine(Path.GetTempPath(), "vitacernita_test_isolated", AppFolderName);
            return isolatedTestDir;
        }

        if (!string.IsNullOrEmpty(customXdg))
        {
            return Path.Combine(customXdg, AppFolderName);
        }

        if (!string.IsNullOrEmpty(customHome))
        {
            return Path.Combine(customHome, ".config", AppFolderName);
        }

        var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (!string.IsNullOrWhiteSpace(xdgConfigHome))
        {
            return Path.Combine(xdgConfigHome, AppFolderName);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!string.IsNullOrWhiteSpace(appData))
            {
                return Path.Combine(appData, AppFolderName);
            }
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".config", AppFolderName);
    }

    /// <summary>
    /// Gets the default configuration file path (~/.config/vitacernita/gmail.lua).
    /// </summary>
    public static string GetDefaultConfigPath(
        string? customHome = null,
        string? customXdg = null,
        string? customConfigDir = null)
    {
        var baseDir = GetDefaultConfigDirectory(customHome, customXdg, customConfigDir);
        return Path.Combine(baseDir, DefaultConfigFileName);
    }

    /// <summary>
    /// Gets the path to the cached client credentials file (~/.config/vitacernita/credentials.json).
    /// </summary>
    public static string GetCredentialsPath(string? configDir = null)
    {
        var baseDir = configDir ?? GetDefaultConfigDirectory();
        return Path.Combine(baseDir, DefaultCredentialsFileName);
    }

    /// <summary>
    /// Gets the path to the user token storage directory (~/.config/vitacernita/tokens/).
    /// </summary>
    public static string GetTokenStorageDirectory(string? configDir = null)
    {
        var baseDir = configDir ?? GetDefaultConfigDirectory();
        return Path.Combine(baseDir, DefaultTokenFolderName);
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
    public static IReadOnlyList<string> GetCandidatePaths(
        string? customHome = null,
        string? customXdg = null,
        string? customConfigDir = null)
    {
        var list = new List<string>
        {
            GetDefaultConfigPath(customHome, customXdg, customConfigDir)
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
    public static string ResolveConfigPath(
        string? explicitPath = null,
        string? customHome = null,
        string? customXdg = null,
        string? customConfigDir = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return Path.GetFullPath(ExpandHome(explicitPath.Trim(), customHome));
        }

        var candidates = GetCandidatePaths(customHome, customXdg, customConfigDir);
        foreach (var candidate in candidates)
        {
            var expanded = ExpandHome(candidate, customHome);
            if (File.Exists(expanded))
            {
                return Path.GetFullPath(expanded);
            }
        }

        return Path.GetFullPath(GetDefaultConfigPath(customHome, customXdg, customConfigDir));
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
