using System;

namespace VitaCernita.Core.AutoReply.Validation;

/// <summary>
/// Provides validation logic for Gmail AutoReply (VacationSettings) configurations.
/// Adheres to the Google Gmail REST API users.settings.vacation resource specification.
/// </summary>
public static class AutoReplyValidator
{
    /// <summary>
    /// Validates an AutoReply instance according to standard Gmail API rules.
    /// Does not universally reject restrictToDomain, since Google Workspace accounts support it.
    /// </summary>
    public static void Validate(AutoReply autoReply)
    {
        if (autoReply == null)
        {
            throw new ArgumentNullException(nameof(autoReply));
        }

        // When auto-reply is enabled, Google requires either subject or body to be non-empty
        if (autoReply.EnableAutoReply)
        {
            bool hasSubject = !string.IsNullOrWhiteSpace(autoReply.ResponseSubject);
            bool hasPlain = !string.IsNullOrWhiteSpace(autoReply.ResponseBodyPlainText);
            bool hasHtml = !string.IsNullOrWhiteSpace(autoReply.ResponseBodyHtml);

            if (!hasSubject && !hasPlain && !hasHtml)
            {
                throw new AutoReplyValidationException(
                    "In order to enable auto-replies, either the response subject or the response body must be nonempty.");
            }
        }

        // Validate epoch millisecond time ranges
        if (autoReply.StartTime.HasValue && autoReply.StartTime.Value < 0)
        {
            throw new AutoReplyValidationException("startTime cannot be negative.");
        }

        if (autoReply.EndTime.HasValue && autoReply.EndTime.Value < 0)
        {
            throw new AutoReplyValidationException("endTime cannot be negative.");
        }

        if (autoReply.StartTime.HasValue && autoReply.EndTime.HasValue)
        {
            if (autoReply.StartTime.Value > autoReply.EndTime.Value)
            {
                throw new AutoReplyValidationException(
                    $"startTime ({autoReply.StartTime.Value}) must precede or equal endTime ({autoReply.EndTime.Value}).");
            }
        }
    }

    /// <summary>
    /// Validates an AutoReply instance against a specific Gmail account or user ID.
    /// Detects and rejects restrictToDomain when targeting a standard @gmail.com account.
    /// </summary>
    public static void ValidateForAccount(AutoReply autoReply, string? accountOrUserId)
    {
        Validate(autoReply);

        if (autoReply.RestrictToDomain && IsStandardGmailAccount(accountOrUserId))
        {
            throw new AutoReplyValidationException(
                $"The 'restrictToDomain' option is only valid for Google Workspace users, but the target account '{accountOrUserId}' is a standard Gmail account (@gmail.com).");
        }
    }

    /// <summary>
    /// Checks whether an email address or user ID represents a standard, consumer Gmail account (@gmail.com or @googlemail.com).
    /// </summary>
    public static bool IsStandardGmailAccount(string? emailOrUserId)
    {
        if (string.IsNullOrWhiteSpace(emailOrUserId)) return false;

        string trimmed = emailOrUserId.Trim();
        if (trimmed.Equals("me", StringComparison.OrdinalIgnoreCase)) return false;

        return trimmed.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase) ||
               trimmed.EndsWith("@googlemail.com", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks whether an email address likely represents a Google Workspace organization account (non-@gmail.com email).
    /// </summary>
    public static bool IsGoogleWorkspaceAccount(string? emailOrUserId)
    {
        if (string.IsNullOrWhiteSpace(emailOrUserId)) return false;

        string trimmed = emailOrUserId.Trim();
        if (trimmed.Equals("me", StringComparison.OrdinalIgnoreCase)) return false;

        return trimmed.Contains('@') && !IsStandardGmailAccount(trimmed);
    }
}
