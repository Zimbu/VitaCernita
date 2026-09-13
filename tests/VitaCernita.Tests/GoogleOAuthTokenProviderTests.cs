using System;
using System.IO;
using System.Threading.Tasks;
using VitaCernita.Core.Api.Auth;
using Xunit;

namespace VitaCernita.Tests;

public class GoogleOAuthTokenProviderTests
{
    private static string CreateTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"vitacernita_token_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void HasCredentials_ReflectsPresenceOfValidCredentials()
    {
        string tempDir = CreateTempDir();
        try
        {
            var providerWithCreds = new GoogleOAuthTokenProvider(
                credentials: new ClientCredentials("id", "secret"),
                configDir: tempDir);
            Assert.True(providerWithCreds.HasCredentials);

            var providerWithoutCreds = new GoogleOAuthTokenProvider(
                credentials: null,
                configDir: tempDir);
            Assert.False(providerWithoutCreds.HasCredentials);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void HasCachedToken_ReturnsFalse_WhenTokenDirectoryEmpty()
    {
        string tempDir = CreateTempDir();
        try
        {
            var provider = new GoogleOAuthTokenProvider(
                credentials: new ClientCredentials("id", "secret"),
                configDir: tempDir,
                user: "testuser");

            Assert.False(provider.HasCachedToken());
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task HasCachedToken_And_ClearToken_InIsolatedDirectory_WorkCorrectly()
    {
        string tempDir = CreateTempDir();
        try
        {
            string tokenDir = Path.Combine(tempDir, "tokens");
            Directory.CreateDirectory(tokenDir);

            string userTokenFile = Path.Combine(tokenDir, "Google.Apis.Auth.OAuth2.Responses.TokenResponse-testuser");
            await File.WriteAllTextAsync(userTokenFile, "{\"access_token\":\"ya29.fake\"}");

            var provider = new GoogleOAuthTokenProvider(
                credentials: new ClientCredentials("id", "secret"),
                configDir: tempDir,
                user: "testuser");

            Assert.True(provider.HasCachedToken());

            await provider.ClearTokenAsync();
            Assert.False(provider.HasCachedToken());
            Assert.False(File.Exists(userTokenFile));
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }
}
