using System;
using VitaCernita.Core.Labels.Validation;

namespace VitaCernita.Core.Labels;

/// <summary>
/// Controls the visibility of messages with this label in the message list in the Gmail web interface.
/// Allowed values: 'show', 'hide'.
/// </summary>
public static class MessageListVisibility
{
    public const string Show = "show";
    public const string Hide = "hide";

    public static bool IsValid(string? value)
    {
        if (value == null) return false;
        return string.Equals(value, Show, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, Hide, StringComparison.OrdinalIgnoreCase);
    }

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new LabelValidationException("messageListVisibility cannot be empty.");
        }

        string trimmed = value.Trim();
        if (string.Equals(trimmed, Show, StringComparison.OrdinalIgnoreCase)) return Show;
        if (string.Equals(trimmed, Hide, StringComparison.OrdinalIgnoreCase)) return Hide;

        throw new LabelValidationException(
            $"Invalid messageListVisibility '{value}'. Allowed values are '{Show}' or '{Hide}'.");
    }

    public static string FromBoolean(bool isVisible) => isVisible ? Show : Hide;
}
