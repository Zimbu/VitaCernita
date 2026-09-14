using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using VitaCernita.Core.Configuration;

namespace VitaCernita.Core.Api.Auth;

/// <summary>
/// Provides OAuth 2.0 access tokens using Google's official <see cref="GoogleWebAuthorizationBroker"/>.
/// Manages automatic background token refresh, loopback browser authorization, and isolated file-based caching.
/// </summary>
public class GoogleOAuthTokenProvider : IGmailTokenProvider
{
    public static readonly string[] DefaultScopes = new[]
    {
        "https://www.googleapis.com/auth/gmail.settings.basic",
        "https://www.googleapis.com/auth/gmail.labels"
    };

    private readonly ClientCredentials? _credentials;
    private readonly string _tokenStorageDir;
    private readonly string _user;
    private readonly IReadOnlyList<string> _scopes;
    private readonly ICodeReceiver? _codeReceiver;
    private readonly IDataStore? _dataStoreOverride;
    private UserCredential? _cachedCredential;

    public GoogleOAuthTokenProvider(
        ClientCredentials? credentials = null,
        string? configDir = null,
        string? tokenStorageDir = null,
        string user = "user",
        IEnumerable<string>? scopes = null,
        ICodeReceiver? codeReceiver = null,
        IDataStore? dataStoreOverride = null)
    {
        _credentials = credentials ?? ClientCredentialsManager.LoadCredentials(configDir: configDir);
        _tokenStorageDir = tokenStorageDir ?? ConfigPathResolver.GetTokenStorageDirectory(configDir);
        _user = string.IsNullOrWhiteSpace(user) ? "user" : user;
        _scopes = scopes?.ToArray() ?? DefaultScopes;
        _codeReceiver = codeReceiver;
        _dataStoreOverride = dataStoreOverride;
    }

    /// <summary>
    /// Gets the user identifier associated with this token provider.
    /// </summary>
    public string User => _user;

    /// <summary>
    /// Gets the token storage directory.
    /// </summary>
    public string TokenStorageDir => _tokenStorageDir;

    /// <summary>
    /// Indicates whether client credentials (Client ID and Client Secret) are available.
    /// </summary>
    public bool HasCredentials => _credentials != null && _credentials.IsValid;

    /// <summary>
    /// Checks whether a cached token exists on disk or in the data store for this user (or a default session).
    /// </summary>
    public bool HasCachedToken()
    {
        if (_dataStoreOverride != null) return false;
        if (!Directory.Exists(_tokenStorageDir)) return false;
        if (Directory.EnumerateFiles(_tokenStorageDir, $"*{_user}*").Any()) return true;
        return Directory.EnumerateFiles(_tokenStorageDir, "*TokenResponse*").Any();
    }

    /// <summary>
    /// Obtains an active OAuth 2.0 access token for Gmail API requests.
    /// If the cached token is expired, automatically refreshes it using the refresh token.
    /// </summary>
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var credential = await GetUserCredentialAsync(cancellationToken);
        if (credential == null) return null;

        return await credential.GetAccessTokenForRequestAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets or authorizes the underlying <see cref="UserCredential"/>.
    /// </summary>
    public async Task<UserCredential> GetUserCredentialAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedCredential != null)
        {
            return _cachedCredential;
        }

        if (_credentials == null || !_credentials.IsValid)
        {
            throw new InvalidOperationException(
                "Google OAuth Client credentials not found. " +
                "Run 'vitacernita login' to authenticate, or provide --client-id and --client-secret.");
        }

        ConfigPathResolver.EnsureDirectoryExists(Path.Combine(_tokenStorageDir, "dummy"));
        ClientCredentialsManager.SecureFilePermissions(_tokenStorageDir, isDirectory: true);

        IDataStore dataStore = _dataStoreOverride ?? new FileDataStore(_tokenStorageDir, fullPath: true);
        var secrets = new ClientSecrets
        {
            ClientId = _credentials.ClientId,
            ClientSecret = _credentials.ClientSecret
        };

        if (_codeReceiver == null && ConfigPathResolver.IsTestEnvironment)
        {
            throw new InvalidOperationException("Interactive browser authorization cannot be executed in an automated test environment.");
        }

        var receiver = _codeReceiver ?? new LocalServerCodeReceiver();

        string effectiveUserKey = _user;
        if (_dataStoreOverride == null && Directory.Exists(_tokenStorageDir))
        {
            if (!Directory.EnumerateFiles(_tokenStorageDir, $"*{_user}*").Any())
            {
                var fallbackFile = Directory.EnumerateFiles(_tokenStorageDir, "*TokenResponse*").FirstOrDefault();
                if (fallbackFile != null)
                {
                    string fileName = Path.GetFileName(fallbackFile);
                    int dashIdx = fileName.LastIndexOf('-');
                    if (dashIdx >= 0 && dashIdx < fileName.Length - 1)
                    {
                        effectiveUserKey = fileName[(dashIdx + 1)..];
                    }
                }
            }
        }

        _cachedCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets,
            _scopes,
            effectiveUserKey,
            cancellationToken,
            dataStore,
            receiver);

        return _cachedCredential;
    }

    /// <summary>
    /// Deletes stored token files for this user.
    /// </summary>
    public async Task ClearTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_dataStoreOverride != null)
        {
            await _dataStoreOverride.ClearAsync();
            _cachedCredential = null;
            return;
        }

        if (Directory.Exists(_tokenStorageDir))
        {
            var userFiles = Directory.EnumerateFiles(_tokenStorageDir, $"*{_user}*").ToList();
            foreach (var file in userFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore deletion failures for in-use files
                }
            }
        }
        _cachedCredential = null;
    }
}
