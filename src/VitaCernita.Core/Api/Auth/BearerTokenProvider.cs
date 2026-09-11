using System;
using System.Threading;
using System.Threading.Tasks;

namespace VitaCernita.Core.Api.Auth;

/// <summary>
/// Supplies a static or environment-sourced OAuth 2.0 Bearer access token.
/// </summary>
public sealed class BearerTokenProvider : IGmailTokenProvider
{
    public const string DefaultEnvVar = "GMAIL_ACCESS_TOKEN";
    private readonly string? _token;
    private readonly string? _envVarName;

    public BearerTokenProvider(string? token = null, string? envVarName = DefaultEnvVar)
    {
        _token = token;
        _envVarName = envVarName;
    }

    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_token))
        {
            return Task.FromResult<string?>(_token);
        }

        if (!string.IsNullOrWhiteSpace(_envVarName))
        {
            string? envToken = Environment.GetEnvironmentVariable(_envVarName);
            if (!string.IsNullOrWhiteSpace(envToken))
            {
                return Task.FromResult<string?>(envToken);
            }
        }

        return Task.FromResult<string?>(null);
    }
}
