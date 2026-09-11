using System;
using System.Collections.Generic;

namespace VitaCernita.Core.AutoReply.Diff;

/// <summary>
/// Configuration options controlling how AutoReply settings are compared.
/// </summary>
public class AutoReplyDiffOptions
{
    /// <summary>
    /// Optional target account email or user ID (e.g. "user@gmail.com").
    /// If provided, used to detect Google Workspace-only restrictions like restrictToDomain.
    /// </summary>
    public string? TargetAccount { get; set; }

    /// <summary>
    /// If true, throws an AutoReplyValidationException when an invalid account setting
    /// (such as restrictToDomain on @gmail.com) is encountered, rather than recording it as AccountError.
    /// </summary>
    public bool StrictAccountValidation { get; set; } = false;

    /// <summary>
    /// If true, fields that are null/unset in the desired AutoReply are ignored during diffing.
    /// </summary>
    public bool IgnoreUnsetDesiredFields { get; set; } = false;

    /// <summary>
    /// Set of field names to exclude from comparison (case-insensitive).
    /// </summary>
    public ISet<string> IgnoredFields { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool ShouldCompareField(string fieldName)
    {
        return !IgnoredFields.Contains(fieldName);
    }
}
