using System;
using System.Collections.Generic;

namespace VitaCernita.Core.Labels.Validation;

public static class LabelValidator
{
    public static readonly IReadOnlySet<string> ReservedSystemLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "INBOX", "UNREAD", "STARRED", "TRASH", "SPAM", "DRAFT", "DRAFTS", "SENT", "IMPORTANT", "CHAT", "CHATS",
        "CATEGORY_PERSONAL", "CATEGORY_SOCIAL", "CATEGORY_PROMOTIONS", "CATEGORY_UPDATES", "CATEGORY_FORUMS", "CATEGORY_PURCHASES"
    };

    public static string ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new LabelValidationException("Label name cannot be empty or whitespace.");
        }

        string trimmed = name.Trim();
        if (ReservedSystemLabels.Contains(trimmed))
        {
            throw new LabelValidationException(
                $"Label name '{name}' is a reserved Gmail system label and cannot be configured as a user label.");
        }

        return trimmed;
    }

    public static string? ValidateMessageListVisibility(string? visibility)
    {
        if (visibility == null) return null;
        return MessageListVisibility.Normalize(visibility);
    }

    public static string? ValidateLabelListVisibility(string? visibility)
    {
        if (visibility == null) return null;
        return LabelListVisibility.Normalize(visibility);
    }

    public static void Validate(GmailLabel label)
    {
        if (label == null)
        {
            throw new LabelValidationException("Label cannot be null.");
        }

        ValidateName(label.Name);

        if (label.MessageListVisibility != null)
        {
            ValidateMessageListVisibility(label.MessageListVisibility);
        }

        if (label.LabelListVisibility != null)
        {
            ValidateLabelListVisibility(label.LabelListVisibility);
        }
    }
}
