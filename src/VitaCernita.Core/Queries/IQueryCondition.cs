namespace VitaCernita.Core.Queries;

/// <summary>
/// Represents a condition in a Gmail search query.
/// </summary>
public interface IQueryCondition
{
    /// <summary>
    /// Formats this condition into Gmail search query syntax.
    /// </summary>
    /// <param name="explicitAnd">If true, uses 'AND' keyword between terms; if false, uses space (Gmail default).</param>
    string ToGmailQuery(bool explicitAnd = false);
}
