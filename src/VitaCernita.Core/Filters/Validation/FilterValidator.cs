using System;
using System.Collections.Generic;
using VitaCernita.Core.Queries.Validation;

namespace VitaCernita.Core.Filters.Validation;

/// <summary>
/// Legacy validator facade preserved for backward compatibility.
/// Delegates all validation operations to <see cref="QueryValidator"/>.
/// </summary>
public static class FilterValidator
{
    public static readonly HashSet<string> CanonicalStarsAndIcons = QueryValidator.CanonicalStarsAndIcons;
    public static readonly HashSet<string> CanonicalHasTargets = QueryValidator.CanonicalHasTargets;
    public static readonly HashSet<string> CanonicalIsTargets = QueryValidator.CanonicalIsTargets;
    public static readonly HashSet<string> CanonicalInTargets = QueryValidator.CanonicalInTargets;
    public static readonly HashSet<string> CanonicalCategoryTargets = QueryValidator.CanonicalCategoryTargets;

    public static void ValidateNonEmpty(string field, string value) => QueryValidator.ValidateNonEmpty(field, value);

    public static void ValidateEmailAddressOrFragment(string field, string value) => QueryValidator.ValidateEmailAddressOrFragment(field, value);

    public static DateTime ValidateAndParseDate(string op, string rawDate, string? customDateFormat) => QueryValidator.ValidateAndParseDate(op, rawDate, customDateFormat);

    public static string ValidateAndNormalizeDuration(string op, string rawDuration) => QueryValidator.ValidateAndNormalizeDuration(op, rawDuration);

    public static string ValidateAndNormalizeHasTarget(string op, string rawValue) => QueryValidator.ValidateAndNormalizeHasTarget(op, rawValue);

    public static string ValidateAndNormalizeStar(string op, string rawValue) => QueryValidator.ValidateAndNormalizeStar(op, rawValue);

    public static string ValidateAndNormalizeIsTarget(string op, string rawValue) => QueryValidator.ValidateAndNormalizeIsTarget(op, rawValue);

    public static string ValidateAndNormalizeInTarget(string op, string rawValue) => QueryValidator.ValidateAndNormalizeInTarget(op, rawValue);

    public static string ValidateAndNormalizeCategoryTarget(string op, string rawValue) => QueryValidator.ValidateAndNormalizeCategoryTarget(op, rawValue);

    public static string ValidateAndNormalizeSize(string op, string rawSize) => QueryValidator.ValidateAndNormalizeSize(op, rawSize);

    public static string ValidateAndNormalizeSizeOperator(string op) => QueryValidator.ValidateAndNormalizeSizeOperator(op);

    public static string NormalizeDateFormat(string format) => QueryValidator.NormalizeDateFormat(format);
}
