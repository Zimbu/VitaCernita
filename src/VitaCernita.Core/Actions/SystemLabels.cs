using System;
using System.Collections.Generic;

namespace VitaCernita.Core.Actions;

/// <summary>
/// Constants and definitions for Gmail system labels.
/// Reference: https://developers.google.com/workspace/gmail/api/guides/labels
/// </summary>
public static class SystemLabels
{
    public const string Inbox = "INBOX";
    public const string Spam = "SPAM";
    public const string Trash = "TRASH";
    public const string Unread = "UNREAD";
    public const string Starred = "STARRED";
    public const string Important = "IMPORTANT";
    public const string Sent = "SENT";
    public const string Draft = "DRAFT";

    // Categories
    public const string CategoryPersonal = "CATEGORY_PERSONAL";
    public const string CategorySocial = "CATEGORY_SOCIAL";
    public const string CategoryPromotions = "CATEGORY_PROMOTIONS";
    public const string CategoryUpdates = "CATEGORY_UPDATES";
    public const string CategoryForums = "CATEGORY_FORUMS";
    public const string CategoryPurchases = "CATEGORY_PURCHASES";

    public static readonly HashSet<string> AllSystemLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        Inbox,
        Spam,
        Trash,
        Unread,
        Starred,
        Important,
        Sent,
        Draft,
        CategoryPersonal,
        CategorySocial,
        CategoryPromotions,
        CategoryUpdates,
        CategoryForums,
        CategoryPurchases
    };

    public static bool IsSystemLabel(string labelId) => AllSystemLabels.Contains(labelId);
}
