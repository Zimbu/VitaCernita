using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using VitaCernita.Core.Api.Auth;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;

namespace VitaCernita.Core.Api;

/// <summary>
/// Native Core .NET HttpClient implementation of IGmailApiClient for the Google Gmail REST API.
/// </summary>
public class HttpGmailApiClient : IGmailApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IGmailTokenProvider? _tokenProvider;
    private readonly string _baseUrl;

    public const string DefaultBaseUrl = "https://gmail.googleapis.com/gmail/v1";

    public HttpGmailApiClient(HttpClient httpClient, IGmailTokenProvider? tokenProvider = null, string baseUrl = DefaultBaseUrl)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tokenProvider = tokenProvider;
        _baseUrl = (baseUrl ?? DefaultBaseUrl).TrimEnd('/');
    }

    public async Task<IReadOnlyList<GmailLabel>> ListLabelsAsync(
        string userId = "me",
        bool onlyUserLabels = true,
        CancellationToken cancellationToken = default)
    {
        string endpoint = $"{_baseUrl}/users/{Uri.EscapeDataString(userId)}/labels";
        string json = await SendGetRequestAsync(endpoint, cancellationToken);
        return GmailLabel.FromApiListResponse(json, onlyUserLabels);
    }

    public async Task<GmailLabel?> GetLabelAsync(
        string labelId,
        string userId = "me",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(labelId))
            throw new ArgumentNullException(nameof(labelId));

        string endpoint = $"{_baseUrl}/users/{Uri.EscapeDataString(userId)}/labels/{Uri.EscapeDataString(labelId)}";
        string? json = await SendGetRequestAllowNotFoundAsync(endpoint, cancellationToken);
        return json != null ? GmailLabel.FromJson(json) : null;
    }

    public async Task<IReadOnlyList<GmailFilter>> ListFiltersAsync(
        string userId = "me",
        CancellationToken cancellationToken = default)
    {
        string endpoint = $"{_baseUrl}/users/{Uri.EscapeDataString(userId)}/settings/filters";
        string json = await SendGetRequestAsync(endpoint, cancellationToken);
        return GmailFilter.FromApiListResponse(json);
    }

    public async Task<GmailFilter?> GetFilterAsync(
        string filterId,
        string userId = "me",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filterId))
            throw new ArgumentNullException(nameof(filterId));

        string endpoint = $"{_baseUrl}/users/{Uri.EscapeDataString(userId)}/settings/filters/{Uri.EscapeDataString(filterId)}";
        string? json = await SendGetRequestAllowNotFoundAsync(endpoint, cancellationToken);
        return json != null ? GmailFilter.FromJson(json) : null;
    }

    private async Task<string> SendGetRequestAsync(string endpoint, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        await ApplyHeadersAsync(request, cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new GmailApiException(
                $"Gmail API request failed with status {(int)response.StatusCode} ({response.StatusCode}): {responseBody}",
                response.StatusCode,
                endpoint,
                responseBody);
        }

        return responseBody;
    }

    private async Task<string?> SendGetRequestAllowNotFoundAsync(string endpoint, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        await ApplyHeadersAsync(request, cancellationToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new GmailApiException(
                $"Gmail API request failed with status {(int)response.StatusCode} ({response.StatusCode}): {responseBody}",
                response.StatusCode,
                endpoint,
                responseBody);
        }

        return responseBody;
    }

    private async Task ApplyHeadersAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.ParseAdd("VitaCernita/1.0");

        if (_tokenProvider != null)
        {
            string? token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
    }
}
