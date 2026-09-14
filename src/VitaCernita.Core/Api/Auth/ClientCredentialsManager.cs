using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using VitaCernita.Core.Configuration;

namespace VitaCernita.Core.Api.Auth;

/// <summary>
/// Manages loading, saving, and parsing Google OAuth 2.0 client credentials.
/// Supports Google Cloud Console's downloaded client_secret_xxx.json ("installed" or "web"),
/// flat credentials.json, and environment variables (GMAIL_CLIENT_ID, GMAIL_CLIENT_SECRET).
/// </summary>
public static class ClientCredentialsManager
{
    public const string ClientIdEnvVar = "GMAIL_CLIENT_ID";
    public const string ClientSecretEnvVar = "GMAIL_CLIENT_SECRET";

    /// <summary>
    /// Attempts to load client credentials in priority order:
    /// 1. From an explicit file path (if provided and exists)
    /// 2. From cached credentials.json in configDir
    /// 3. From environment variables (GMAIL_CLIENT_ID, GMAIL_CLIENT_SECRET)
    /// Returns null if not found.
    /// </summary>
    public static async Task<ClientCredentials?> LoadCredentialsAsync(
        string? explicitFilePath = null,
        string? configDir = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitFilePath))
        {
            string expanded = ConfigPathResolver.ExpandHome(explicitFilePath.Trim());
            if (!File.Exists(expanded))
            {
                throw new FileNotFoundException($"Google OAuth credentials file not found: {explicitFilePath}", expanded);
            }

            string content = await File.ReadAllTextAsync(expanded);
            var parsed = ParseCredentialsJson(content);
            if (parsed == null || !parsed.IsValid)
            {
                throw new FormatException($"Invalid or unparseable client credentials format in: {explicitFilePath}");
            }
            return parsed;
        }

        if (ConfigPathResolver.IsTestEnvironment && string.IsNullOrWhiteSpace(configDir))
        {
            // In automated test execution, do not fall back to ambient machine files
            string? testEnvId = Environment.GetEnvironmentVariable(ClientIdEnvVar);
            string? testEnvSecret = Environment.GetEnvironmentVariable(ClientSecretEnvVar);
            if (!string.IsNullOrWhiteSpace(testEnvId) && !string.IsNullOrWhiteSpace(testEnvSecret))
            {
                return new ClientCredentials(testEnvId.Trim(), testEnvSecret.Trim());
            }
            return null;
        }

        string defaultPath = ConfigPathResolver.GetCredentialsPath(configDir);
        if (File.Exists(defaultPath))
        {
            string content = await File.ReadAllTextAsync(defaultPath);
            var parsed = ParseCredentialsJson(content);
            if (parsed != null && parsed.IsValid)
            {
                return parsed;
            }
        }

        string? envId = Environment.GetEnvironmentVariable(ClientIdEnvVar);
        string? envSecret = Environment.GetEnvironmentVariable(ClientSecretEnvVar);
        if (!string.IsNullOrWhiteSpace(envId) && !string.IsNullOrWhiteSpace(envSecret))
        {
            return new ClientCredentials(envId.Trim(), envSecret.Trim());
        }

        return null;
    }

    /// <summary>
    /// Synchronous version of <see cref="LoadCredentialsAsync"/>.
    /// </summary>
    public static ClientCredentials? LoadCredentials(
        string? explicitFilePath = null,
        string? configDir = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitFilePath))
        {
            string expanded = ConfigPathResolver.ExpandHome(explicitFilePath.Trim());
            if (!File.Exists(expanded))
            {
                throw new FileNotFoundException($"Google OAuth credentials file not found: {explicitFilePath}", expanded);
            }

            string content = File.ReadAllText(expanded);
            var parsed = ParseCredentialsJson(content);
            if (parsed == null || !parsed.IsValid)
            {
                throw new FormatException($"Invalid or unparseable client credentials format in: {explicitFilePath}");
            }
            return parsed;
        }

        if (ConfigPathResolver.IsTestEnvironment && string.IsNullOrWhiteSpace(configDir))
        {
            // In automated test execution, do not fall back to ambient machine files
            string? testEnvId = Environment.GetEnvironmentVariable(ClientIdEnvVar);
            string? testEnvSecret = Environment.GetEnvironmentVariable(ClientSecretEnvVar);
            if (!string.IsNullOrWhiteSpace(testEnvId) && !string.IsNullOrWhiteSpace(testEnvSecret))
            {
                return new ClientCredentials(testEnvId.Trim(), testEnvSecret.Trim());
            }
            return null;
        }

        string defaultPath = ConfigPathResolver.GetCredentialsPath(configDir);
        if (File.Exists(defaultPath))
        {
            string content = File.ReadAllText(defaultPath);
            var parsed = ParseCredentialsJson(content);
            if (parsed != null && parsed.IsValid)
            {
                return parsed;
            }
        }

        string? envId = Environment.GetEnvironmentVariable(ClientIdEnvVar);
        string? envSecret = Environment.GetEnvironmentVariable(ClientSecretEnvVar);
        if (!string.IsNullOrWhiteSpace(envId) && !string.IsNullOrWhiteSpace(envSecret))
        {
            return new ClientCredentials(envId.Trim(), envSecret.Trim());
        }

        return null;
    }

    /// <summary>
    /// Saves client credentials to disk (formatted as Google's standard installed client JSON),
    /// and sets user-only (0600) permissions on Unix-like operating systems.
    /// </summary>
    public static async Task SaveCredentialsAsync(
        ClientCredentials credentials,
        string? configDir = null,
        string? explicitPath = null)
    {
        if (credentials == null) throw new ArgumentNullException(nameof(credentials));
        if (!credentials.IsValid) throw new ArgumentException("Client credentials must have non-empty ClientId and ClientSecret.", nameof(credentials));

        string targetPath = !string.IsNullOrWhiteSpace(explicitPath)
            ? ConfigPathResolver.ExpandHome(explicitPath.Trim())
            : ConfigPathResolver.GetCredentialsPath(configDir);

        ConfigPathResolver.EnsureDirectoryExists(targetPath);

        var data = new
        {
            installed = new
            {
                client_id = credentials.ClientId,
                client_secret = credentials.ClientSecret
            }
        };

        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(targetPath, json);

        SecureFilePermissions(targetPath);
    }

    /// <summary>
    /// Parses client credentials from JSON text.
    /// Handles Google Cloud Console client_secret.json ("installed" or "web") as well as flat JSON.
    /// </summary>
    public static ClientCredentials? ParseCredentialsJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // 1. Google standard format: {"installed": { "client_id": "...", "client_secret": "..." }}
            if (root.TryGetProperty("installed", out var installed))
            {
                string? id = GetStringProp(installed, "client_id");
                string? secret = GetStringProp(installed, "client_secret");
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(secret))
                {
                    return new ClientCredentials(id, secret);
                }
            }

            // 2. Google web format: {"web": { "client_id": "...", "client_secret": "..." }}
            if (root.TryGetProperty("web", out var web))
            {
                string? id = GetStringProp(web, "client_id");
                string? secret = GetStringProp(web, "client_secret");
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(secret))
                {
                    return new ClientCredentials(id, secret);
                }
            }

            // 3. Flat format: {"client_id": "...", "client_secret": "..."}
            string? flatId = GetStringProp(root, "client_id") ?? GetStringProp(root, "ClientId");
            string? flatSecret = GetStringProp(root, "client_secret") ?? GetStringProp(root, "ClientSecret");
            if (!string.IsNullOrEmpty(flatId) && !string.IsNullOrEmpty(flatSecret))
            {
                return new ClientCredentials(flatId, flatSecret);
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static string? GetStringProp(JsonElement element, string propName)
    {
        if (element.TryGetProperty(propName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        return null;
    }

    /// <summary>
    /// Applies owner-only permissions on Unix/Linux/macOS platforms:
    /// - For directories: 0700 (read, write, execute/traverse)
    /// - For files: 0600 (read, write)
    /// </summary>
    public static void SecureFilePermissions(string path, bool? isDirectory = null)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                bool isDir = isDirectory ?? Directory.Exists(path);
                var mode = isDir
                    ? UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                    : UnixFileMode.UserRead | UnixFileMode.UserWrite;

                File.SetUnixFileMode(path, mode);
            }
            catch
            {
                // Silently ignore if file system does not support Unix modes
            }
        }
    }
}
