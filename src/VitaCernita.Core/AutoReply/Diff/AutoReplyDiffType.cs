namespace VitaCernita.Core.AutoReply.Diff;

/// <summary>
/// Categorizes the difference between existing and desired AutoReply (VacationSettings) configurations.
/// </summary>
public enum AutoReplyDiffType
{
    /// <summary>
    /// Existing and desired configurations match.
    /// </summary>
    Unchanged,

    /// <summary>
    /// Auto-reply is enabled in desired settings where it was previously disabled or absent.
    /// </summary>
    Added,

    /// <summary>
    /// Auto-reply is disabled in desired settings where it was previously enabled.
    /// </summary>
    Disabled,

    /// <summary>
    /// Auto-reply settings (subject, body, date ranges, restrictions) differ.
    /// </summary>
    Modified
}
