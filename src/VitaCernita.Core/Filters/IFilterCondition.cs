namespace VitaCernita.Core.Filters;

/// <summary>
/// Represents a condition in a Gmail filter query.
/// </summary>
public interface IFilterCondition
{
    /// <summary>
    /// Formats this condition into Gmail search query syntax.
    /// </summary>
    /// <param name="explicitAnd">If true, uses 'AND' keyword between terms; if false, uses space (Gmail default).</param>
    string ToGmailQuery(bool explicitAnd = false);
}
