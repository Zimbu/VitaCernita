using System;
using System.IO;
using VitaCernita.Core.Configuration;
using Xunit;

namespace VitaCernita.Tests.Core.Configuration;

public class ConfigPathResolverTests
{
    [Fact]
    public void GetDefaultConfigPath_WithCustomXdg_UsesXdgFolder()
    {
        string customXdg = Path.Combine(Path.GetTempPath(), "test_xdg");
        string expected = Path.Combine(customXdg, "vitacernita", "gmail.lua");

        string actual = ConfigPathResolver.GetDefaultConfigPath(customXdg: customXdg);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetDefaultConfigPath_WithCustomHome_UsesDotConfigFolder()
    {
        string customHome = Path.Combine(Path.GetTempPath(), "test_home");
        string expected = Path.Combine(customHome, ".config", "vitacernita", "gmail.lua");

        string actual = ConfigPathResolver.GetDefaultConfigPath(customHome: customHome);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ExpandHome_ExpandsLeadingTilde()
    {
        string customHome = "/home/testuser";
        string path = "~/my_configs/filter.lua";

        string expanded = ConfigPathResolver.ExpandHome(path, customHome);

        Assert.Equal(Path.Combine("/home/testuser", "my_configs/filter.lua"), expanded);
    }

    [Fact]
    public void ExpandHome_LeavesAbsolutePathUnchanged()
    {
        string abs = Path.GetFullPath("test.lua");
        string result = ConfigPathResolver.ExpandHome(abs);

        Assert.Equal(abs, result);
    }

    [Fact]
    public void ResolveConfigPath_ExplicitPath_ExpandsAndReturnsFullPath()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "vitacernita_test_" + Guid.NewGuid().ToString("N"));
        string customHome = tempDir;
        string explicitPath = "~/custom_filter.lua";

        string resolved = ConfigPathResolver.ResolveConfigPath(explicitPath, customHome: customHome);

        Assert.Equal(Path.GetFullPath(Path.Combine(tempDir, "custom_filter.lua")), resolved);
    }

    [Fact]
    public void ResolveConfigPath_WithoutExplicitPath_FindsLocalFileIfExists()
    {
        // When config/gmail_filter.lua exists in the current repo, it should resolve to it
        if (File.Exists("config/gmail_filter.lua"))
        {
            string resolved = ConfigPathResolver.ResolveConfigPath(null);
            Assert.True(File.Exists(resolved));
        }
    }

    [Fact]
    public void EnsureDirectoryExists_CreatesDirectoryIfNotExisting()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "vitacernita_dirtest_" + Guid.NewGuid().ToString("N"));
        string targetFile = Path.Combine(tempDir, "nested", "gmail.lua");

        try
        {
            Assert.False(Directory.Exists(Path.GetDirectoryName(targetFile)));
            ConfigPathResolver.EnsureDirectoryExists(targetFile);
            Assert.True(Directory.Exists(Path.GetDirectoryName(targetFile)));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void ConfigDirEnvOverride_IsolatesAllPathsFromLiveSystem()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "vitacernita_isolated_" + Guid.NewGuid().ToString("N"));
        string? orig = Environment.GetEnvironmentVariable(ConfigPathResolver.ConfigDirEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(ConfigPathResolver.ConfigDirEnvVar, tempDir);

            string configDir = ConfigPathResolver.GetDefaultConfigDirectory();
            string configPath = ConfigPathResolver.GetDefaultConfigPath();
            string credsPath = ConfigPathResolver.GetCredentialsPath();
            string tokenDir = ConfigPathResolver.GetTokenStorageDirectory();

            Assert.Equal(Path.GetFullPath(tempDir), configDir);
            Assert.Equal(Path.Combine(Path.GetFullPath(tempDir), "gmail.lua"), configPath);
            Assert.Equal(Path.Combine(Path.GetFullPath(tempDir), "credentials.json"), credsPath);
            Assert.Equal(Path.Combine(Path.GetFullPath(tempDir), "tokens"), tokenDir);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConfigPathResolver.ConfigDirEnvVar, orig);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }
}
