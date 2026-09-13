namespace VitaCernita.Core.Api.Auth;

/// <summary>
/// Holds Google OAuth 2.0 Client credentials (Client ID and Client Secret).
/// </summary>
public record ClientCredentials(string ClientId, string ClientSecret)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
