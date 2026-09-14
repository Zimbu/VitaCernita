using System;
using System.IO;
using System.Threading.Tasks;
using VitaCernita.Core.Api.Auth;
using Xunit;

namespace VitaCernita.Tests;

public class ClientCredentialsManagerTests
{
    private static string CreateTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"vitacernita_cred_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void ParseCredentialsJson_GoogleInstalledFormat_ParsesCorrectly()
    {
        string json = @"
{
  ""installed"": {
    ""client_id"": ""123456-test.apps.googleusercontent.com"",
    ""project_id"": ""test-project"",
    ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
    ""token_uri"": ""https://oauth2.googleapis.com/token"",
    ""client_secret"": ""GOCSPX-secret123""
  }
}";
        var creds = ClientCredentialsManager.ParseCredentialsJson(json);

        Assert.NotNull(creds);
        Assert.Equal("123456-test.apps.googleusercontent.com", creds.ClientId);
        Assert.Equal("GOCSPX-secret123", creds.ClientSecret);
        Assert.True(creds.IsValid);
    }

    [Fact]
    public void ParseCredentialsJson_GoogleWebFormat_ParsesCorrectly()
    {
        string json = @"
{
  ""web"": {
    ""client_id"": ""987654-web.apps.googleusercontent.com"",
    ""client_secret"": ""GOCSPX-websecret456""
  }
}";
        var creds = ClientCredentialsManager.ParseCredentialsJson(json);

        Assert.NotNull(creds);
        Assert.Equal("987654-web.apps.googleusercontent.com", creds.ClientId);
        Assert.Equal("GOCSPX-websecret456", creds.ClientSecret);
        Assert.True(creds.IsValid);
    }

    [Fact]
    public void ParseCredentialsJson_FlatFormat_ParsesCorrectly()
    {
        string json = @"{ ""client_id"": ""flat-id"", ""client_secret"": ""flat-secret"" }";
        var creds = ClientCredentialsManager.ParseCredentialsJson(json);

        Assert.NotNull(creds);
        Assert.Equal("flat-id", creds.ClientId);
        Assert.Equal("flat-secret", creds.ClientSecret);
        Assert.True(creds.IsValid);
    }

    [Fact]
    public void ParseCredentialsJson_InvalidJson_ReturnsNull()
    {
        Assert.Null(ClientCredentialsManager.ParseCredentialsJson("not valid json"));
        Assert.Null(ClientCredentialsManager.ParseCredentialsJson(""));
        Assert.Null(ClientCredentialsManager.ParseCredentialsJson("{}"));
    }

    [Fact]
    public async Task SaveAndLoadCredentials_InIsolatedTempDirectory_Succeeds()
    {
        string tempDir = CreateTempDir();
        try
        {
            var original = new ClientCredentials("client-id-test", "client-secret-test");
            await ClientCredentialsManager.SaveCredentialsAsync(original, configDir: tempDir);

            string savedPath = Path.Combine(tempDir, "credentials.json");
            Assert.True(File.Exists(savedPath));

            var loaded = await ClientCredentialsManager.LoadCredentialsAsync(configDir: tempDir);
            Assert.NotNull(loaded);
            Assert.Equal("client-id-test", loaded.ClientId);
            Assert.Equal("client-secret-test", loaded.ClientSecret);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task LoadCredentials_FromEnvironmentVariables_Succeeds()
    {
        string tempDir = CreateTempDir();
        string? origId = Environment.GetEnvironmentVariable(ClientCredentialsManager.ClientIdEnvVar);
        string? origSecret = Environment.GetEnvironmentVariable(ClientCredentialsManager.ClientSecretEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(ClientCredentialsManager.ClientIdEnvVar, "env-client-id");
            Environment.SetEnvironmentVariable(ClientCredentialsManager.ClientSecretEnvVar, "env-client-secret");

            var creds = await ClientCredentialsManager.LoadCredentialsAsync(configDir: tempDir);
            Assert.NotNull(creds);
            Assert.Equal("env-client-id", creds.ClientId);
            Assert.Equal("env-client-secret", creds.ClientSecret);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ClientCredentialsManager.ClientIdEnvVar, origId);
            Environment.SetEnvironmentVariable(ClientCredentialsManager.ClientSecretEnvVar, origSecret);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task LoadCredentials_FromExplicitFile_Succeeds()
    {
        string tempDir = CreateTempDir();
        try
        {
            string customFile = Path.Combine(tempDir, "custom_credentials.json");
            string json = @"{ ""client_id"": ""explicit-id"", ""client_secret"": ""explicit-secret"" }";
            await File.WriteAllTextAsync(customFile, json);

            var creds = await ClientCredentialsManager.LoadCredentialsAsync(explicitFilePath: customFile, configDir: tempDir);
            Assert.NotNull(creds);
            Assert.Equal("explicit-id", creds.ClientId);
            Assert.Equal("explicit-secret", creds.ClientSecret);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void SecureFilePermissions_OnDirectory_AllowsFileCreationInsideIt()
    {
        string tempDir = CreateTempDir();
        try
        {
            string subDir = Path.Combine(tempDir, "tokens");
            Directory.CreateDirectory(subDir);

            ClientCredentialsManager.SecureFilePermissions(subDir, isDirectory: true);

            string fileInside = Path.Combine(subDir, "test_token_file");
            File.WriteAllText(fileInside, "token-data");

            Assert.True(File.Exists(fileInside));
            Assert.Equal("token-data", File.ReadAllText(fileInside));
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task LoadCredentials_ExplicitPathDoesNotExist_ThrowsFileNotFoundException()
    {
        string nonExistent = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.json");
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            ClientCredentialsManager.LoadCredentialsAsync(explicitFilePath: nonExistent));
    }

    [Fact]
    public async Task LoadCredentials_ExplicitPathInvalidFormat_ThrowsFormatException()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"invalid_{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(tempFile, "{ \"invalid\": true }");
            await Assert.ThrowsAsync<FormatException>(() =>
                ClientCredentialsManager.LoadCredentialsAsync(explicitFilePath: tempFile));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
