using System.Threading;
using System.Threading.Tasks;

namespace VitaCernita.Core.Api.Auth;

/// <summary>
/// Provides OAuth 2.0 access tokens for authenticating requests against the Gmail API.
/// </summary>
public interface IGmailTokenProvider
{
    /// <summary>
    /// Obtains a valid OAuth 2.0 bearer access token.
    /// </summary>
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
