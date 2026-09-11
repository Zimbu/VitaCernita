using System;
using System.Collections.Generic;
using System.Linq;
using VitaCernita.Core.Filters.Validation;

namespace VitaCernita.Core.Actions.Validation;

/// <summary>
/// Validates action parameters, labels, categories, and forwarding addresses.
/// </summary>
public static class ActionValidator
{
    private static readonly Dictionary<string, string> CategoryLabelMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "primary", SystemLabels.CategoryPersonal },
        { "personal", SystemLabels.CategoryPersonal },
        { "category_personal", SystemLabels.CategoryPersonal },
        { "category_primary", SystemLabels.CategoryPersonal },

        { "purchases", SystemLabels.CategoryPurchases },
        { "purchase", SystemLabels.CategoryPurchases },
        { "category_purchases", SystemLabels.CategoryPurchases },
        { "category_purchase", SystemLabels.CategoryPurchases },

        { "social", SystemLabels.CategorySocial },
        { "category_social", SystemLabels.CategorySocial },

        { "updates", SystemLabels.CategoryUpdates },
        { "update", SystemLabels.CategoryUpdates },
        { "category_updates", SystemLabels.CategoryUpdates },
        { "category_update", SystemLabels.CategoryUpdates },

        { "forums", SystemLabels.CategoryForums },
        { "forum", SystemLabels.CategoryForums },
        { "category_forums", SystemLabels.CategoryForums },
        { "category_forum", SystemLabels.CategoryForums },

        { "promotions", SystemLabels.CategoryPromotions },
        { "promotion", SystemLabels.CategoryPromotions },
        { "category_promotions", SystemLabels.CategoryPromotions },
        { "category_promotion", SystemLabels.CategoryPromotions }
    };

    public static readonly string[] SupportedCategories =
    [
        "Primary",
        "Purchases",
        "Social",
        "Updates",
        "Forums",
        "Promotions"
    ];

    public static string ValidateAndNormalizeCategory(string rawCategory)
    {
        if (string.IsNullOrWhiteSpace(rawCategory))
        {
            throw new ActionValidationException("Category name cannot be empty or whitespace.");
        }

        string trimmed = rawCategory.Trim();
        if (CategoryLabelMap.TryGetValue(trimmed, out var systemLabelId))
        {
            return systemLabelId;
        }

        throw new ActionValidationException(
            $"Invalid category '{rawCategory}' for action 'add_category'. " +
            $"Supported categories are: {string.Join(", ", SupportedCategories)}.");
    }

    public static string ValidateLabel(string rawLabel)
    {
        if (string.IsNullOrWhiteSpace(rawLabel))
        {
            throw new ActionValidationException("Label name cannot be empty or whitespace.");
        }

        string trimmed = rawLabel.Trim();
        if (SystemLabels.IsSystemLabel(trimmed))
        {
            throw new ActionValidationException(
                $"Cannot add system label '{trimmed}' via add_label. " +
                "System labels must be managed using designated action operations " +
                "(archive, star, delete, mark_important, mark_unread, add_category).");
        }

        return trimmed;
    }

    public static string ValidateForwardEmail(string rawEmail)
    {
        if (string.IsNullOrWhiteSpace(rawEmail))
        {
            throw new ActionValidationException("Forwarding email address cannot be empty or whitespace.");
        }

        string trimmed = rawEmail.Trim();
        try
        {
            QueryValidator.ValidateEmailAddressOrFragment("forward", trimmed);
        }
        catch (QueryValidationException ex)
        {
            throw new ActionValidationException($"Invalid forwarding email address '{rawEmail}': {ex.Message}", ex);
        }

        int atIndex = trimmed.IndexOf('@');
        if (atIndex <= 0 || atIndex == trimmed.Length - 1)
        {
            throw new ActionValidationException($"Forwarding address '{rawEmail}' must be a complete email address (e.g. user@example.com).");
        }

        return trimmed;
    }
}
