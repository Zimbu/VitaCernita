using System;
using System.IO;
using System.Threading.Tasks;
using VitaCernita.Cli;
using VitaCernita.Cli.Commands;
using Xunit;

namespace VitaCernita.Tests;

public class LoginAndLogoutCommandTests
{
    private static string CreateTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"vitacernita_cli_auth_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public async Task LoginCommand_WithExplicitClientIdAndSecret_SavesCredentials()
    {
        string tempDir = CreateTempDir();
        try
        {
            var cmd = new LoginCommand();
            // Since we don't want to actually launch a browser in automated unit tests,
            // we pass client-id and secret. If already cached token check or browser trigger happens,
            // let's verify that credentials.json was written before the browser trigger!
            string credPath = Path.Combine(tempDir, "credentials.json");

            // We can test credentials saving via CLI args:
            int exitCode = await cmd.ExecuteAsync(new[]
            {
                "--client-id", "test-client-id",
                "--client-secret", "test-client-secret",
                "--credentials-only",
                "--config-dir", tempDir
            });

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(credPath));
            string json = await File.ReadAllTextAsync(credPath);
            Assert.Contains("test-client-id", json);
            Assert.Contains("test-client-secret", json);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task LoginCommand_WithCredentialsJsonFile_ImportsAndSavesCredentials()
    {
        string tempDir = CreateTempDir();
        try
        {
            string sourceJson = Path.Combine(tempDir, "google_secret.json");
            await File.WriteAllTextAsync(sourceJson, @"{
  ""installed"": {
    ""client_id"": ""imported-id.apps.googleusercontent.com"",
    ""client_secret"": ""imported-secret""
  }
}");

            var cmd = new LoginCommand();
            int exitCode = await cmd.ExecuteAsync(new[]
            {
                "--credentials", sourceJson,
                "--credentials-only",
                "--config-dir", tempDir
            });

            Assert.Equal(0, exitCode);
            string destCredPath = Path.Combine(tempDir, "credentials.json");
            Assert.True(File.Exists(destCredPath));
            string content = await File.ReadAllTextAsync(destCredPath);
            Assert.Contains("imported-id.apps.googleusercontent.com", content);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task LoginCommand_WithNonExistentCredentialsFile_ReturnsErrorCode()
    {
        string tempDir = CreateTempDir();
        try
        {
            string missingFile = Path.Combine(tempDir, "does_not_exist.json");
            var cmd = new LoginCommand();
            int exitCode = await cmd.ExecuteAsync(new[]
            {
                "--credentials", missingFile,
                "--config-dir", tempDir
            });

            Assert.Equal(1, exitCode);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task LoginCommand_WithInvalidCredentialsFile_ReturnsErrorCode()
    {
        string tempDir = CreateTempDir();
        try
        {
            string badFile = Path.Combine(tempDir, "invalid.json");
            await File.WriteAllTextAsync(badFile, "{\"invalid\": 123}");

            var cmd = new LoginCommand();
            int exitCode = await cmd.ExecuteAsync(new[]
            {
                "--credentials", badFile,
                "--config-dir", tempDir
            });

            Assert.Equal(1, exitCode);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task LogoutCommand_RemovesTokensAndOptionallyCredentials()
    {
        string tempDir = CreateTempDir();
        try
        {
            string tokenDir = Path.Combine(tempDir, "tokens");
            Directory.CreateDirectory(tokenDir);
            string tokenFile = Path.Combine(tokenDir, "Google.Apis.Auth.OAuth2.Responses.TokenResponse-test");
            await File.WriteAllTextAsync(tokenFile, "fake token");

            string credFile = Path.Combine(tempDir, "credentials.json");
            await File.WriteAllTextAsync(credFile, "fake creds");

            var cmd = new LogoutCommand();

            // First logout without --all
            int exit1 = await cmd.ExecuteAsync(new[] { "--config-dir", tempDir });
            Assert.Equal(0, exit1);
            Assert.False(File.Exists(tokenFile));
            Assert.True(File.Exists(credFile)); // credentials still intact

            // Logout with --all
            int exit2 = await cmd.ExecuteAsync(new[] { "--config-dir", tempDir, "--all" });
            Assert.Equal(0, exit2);
            Assert.False(File.Exists(credFile)); // credentials now removed
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Theory]
    [InlineData("login")]
    [InlineData("logout")]
    public async Task Dispatcher_HelpForLoginAndLogout_ReturnsZero(string command)
    {
        var dispatcher = Program.CreateDefaultDispatcher();
        int exitCode1 = await dispatcher.DispatchAsync(new[] { command, "--help" });
        int exitCode2 = await dispatcher.DispatchAsync(new[] { "help", command });

        Assert.Equal(0, exitCode1);
        Assert.Equal(0, exitCode2);
    }
}
