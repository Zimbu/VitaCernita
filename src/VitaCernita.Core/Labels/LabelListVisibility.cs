using System;
using VitaCernita.Core.Labels.Validation;

namespace VitaCernita.Core.Labels;

/// <summary>
/// Controls the visibility of the label in the label list in the Gmail web interface.
/// Allowed values: 'labelShow', 'labelShowIfUnread', 'labelHide'.
/// </summary>
public static class LabelListVisibility
{
    public const string LabelShow = "labelShow";
    public const string LabelShowIfUnread = "labelShowIfUnread";
    public const string LabelHide = "labelHide";

    public static bool IsValid(string? value)
    {
        if (value == null) return false;
        return string.Equals(value, LabelShow, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "show", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, LabelShowIfUnread, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "showIfUnread", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "show_if_unread", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, LabelHide, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "hide", StringComparison.OrdinalIgnoreCase);
    }

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new LabelValidationException("labelListVisibility cannot be empty.");
        }

        string trimmed = value.Trim();
        if (string.Equals(trimmed, LabelShow, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "show", StringComparison.OrdinalIgnoreCase))
        {
            return LabelShow;
        }

        if (string.Equals(trimmed, LabelShowIfUnread, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "showIfUnread", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "show_if_unread", StringComparison.OrdinalIgnoreCase))
        {
            return LabelShowIfUnread;
        }

        if (string.Equals(trimmed, LabelHide, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "hide", StringComparison.OrdinalIgnoreCase))
        {
            return LabelHide;
        }

        throw new LabelValidationException(
            $"Invalid labelListVisibility '{value}'. Allowed values are '{LabelShow}', '{LabelShowIfUnread}', '{LabelHide}' (or 'show', 'show_if_unread', 'hide').");
    }
}
