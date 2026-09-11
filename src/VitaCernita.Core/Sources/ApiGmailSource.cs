using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VitaCernita.Core.Api;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;

namespace VitaCernita.Core.Sources;

/// <summary>
/// An IGmailSource implementation backed by an IGmailApiClient (e.g. live Gmail REST API or FakeGmailApiClient).
/// Fetches remote mailbox resources and exposes them as in-memory IQueryable collections.
/// </summary>
public class ApiGmailSource : IGmailSource
{
    private readonly IGmailApiClient _client;
    private readonly string _userId;
    private readonly bool _onlyUserLabels;

    public string Name { get; }

    public ApiGmailSource(
        IGmailApiClient client,
        string userId = "me",
        bool onlyUserLabels = true,
        string? name = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _userId = userId ?? "me";
        _onlyUserLabels = onlyUserLabels;
        Name = name ?? $"Gmail API ({_userId})";
    }

    public async Task<IQueryable<GmailLabel>> GetLabelsAsync(CancellationToken cancellationToken = default)
    {
        var labels = await _client.ListLabelsAsync(_userId, _onlyUserLabels, cancellationToken);
        return labels.AsQueryable();
    }

    public async Task<IQueryable<GmailFilter>> GetFiltersAsync(CancellationToken cancellationToken = default)
    {
        var filters = await _client.ListFiltersAsync(_userId, cancellationToken);
        return filters.AsQueryable();
    }
}
