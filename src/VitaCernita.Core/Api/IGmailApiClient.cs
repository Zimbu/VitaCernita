using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VitaCernita.Core.AutoReply;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;

namespace VitaCernita.Core.Api;

/// <summary>
/// Defines client operations for interacting with the Google Gmail API.
/// </summary>
public interface IGmailApiClient
{
    /// <summary>
    /// Lists labels from the specified user mailbox (users.labels.list).
    /// </summary>
    /// <param name="userId">The user's email address or the special value 'me'.</param>
    /// <param name="onlyUserLabels">If true, filters out system labels like INBOX, TRASH, etc.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<GmailLabel>> ListLabelsAsync(
        string userId = "me",
        bool onlyUserLabels = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific label by ID (users.labels.get).
    /// Returns null if the label does not exist (404).
    /// </summary>
    Task<GmailLabel?> GetLabelAsync(
        string labelId,
        string userId = "me",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists search filters from the specified user mailbox (users.settings.filters.list).
    /// </summary>
    Task<IReadOnlyList<GmailFilter>> ListFiltersAsync(
        string userId = "me",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific search filter by ID (users.settings.filters.get).
    /// Returns null if the filter does not exist (404).
    /// </summary>
    Task<GmailFilter?> GetFilterAsync(
        string filterId,
        string userId = "me",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves vacation responder settings (users.settings.getVacation).
    /// Returns null if settings could not be found (404).
    /// </summary>
    Task<AutoReply.AutoReply?> GetAutoReplyAsync(
        string userId = "me",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates vacation responder settings (users.settings.updateVacation).
    /// </summary>
    Task<AutoReply.AutoReply> UpdateAutoReplyAsync(
        AutoReply.AutoReply autoReply,
        string userId = "me",
        CancellationToken cancellationToken = default);
}
