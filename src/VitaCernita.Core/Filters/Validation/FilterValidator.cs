using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace VitaCernita.Core.Filters.Validation;

public static class FilterValidator
{
    private static readonly string[] DefaultDateFormats =
    [
        "MM/dd/yyyy",
        "yyyy/MM/dd",
        "MM-dd-yyyy",
        "yyyy-MM-dd",
        "MM.dd.yyyy",
        "yyyy.MM.dd",
        "M/d/yyyy",
        "yyyy/M/d",
        "M-d-yyyy",
        "yyyy-M-d",
        "M.d.yyyy",
        "yyyy.M.d"
    ];

    private static readonly Regex DurationRegex = new(@"^([1-9]\d*)([dmyDMY])$", RegexOptions.Compiled);
    private static readonly Regex EmailDisplayNameRegex = new(@"^([^<>]*)\s*<([^<>]+)>$", RegexOptions.Compiled);
    private static readonly Regex ValidEmailCharsRegex = new(@"^[a-zA-Z0-9._+%@-]+$", RegexOptions.Compiled);

    public static void ValidateNonEmpty(string field, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FilterValidationException($"Field '{field}' cannot be empty or contain only whitespace.");
        }
    }

    public static void ValidateEmailAddressOrFragment(string field, string value)
    {
        ValidateNonEmpty(field, value);

        string addressPart = value.Trim();

        // If enclosed in brackets (e.g. "Display Name <user@example.com>")
        if (value.Contains('<') || value.Contains('>'))
        {
            var match = EmailDisplayNameRegex.Match(value.Trim());
            if (!match.Success)
            {
                throw new FilterValidationException($"Malformed email address format in field '{field}': '{value}'.");
            }
            addressPart = match.Groups[2].Value.Trim();
        }

        // Validate at most one '@'
        int atCount = addressPart.Count(c => c == '@');
        if (atCount > 1)
        {
            throw new FilterValidationException($"Email field '{field}' cannot contain more than one '@' character. Found: '{value}'.");
        }

        // Validate unsupported characters (only standard email local/domain fragment chars allowed)
        if (!ValidEmailCharsRegex.IsMatch(addressPart))
        {
            throw new FilterValidationException($"Email field '{field}' contains invalid or unsupported characters in address '{addressPart}'.");
        }

        // Check for invalid consecutive dots
        if (addressPart.Contains(".."))
        {
            throw new FilterValidationException($"Email field '{field}' cannot contain consecutive dots: '{addressPart}'.");
        }
    }

    public static DateTime ValidateAndParseDate(string op, string rawDate, string? customDateFormat)
    {
        ValidateNonEmpty(op, rawDate);

        string trimmed = rawDate.Trim();

        // Enforce no time components
        if (trimmed.Contains(':') || trimmed.Contains('T') ||
            trimmed.IndexOf("am", StringComparison.OrdinalIgnoreCase) >= 0 ||
            trimmed.IndexOf("pm", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            throw new FilterValidationException($"Date for operator '{op}' contains time components: '{rawDate}'. Only date values without times are permitted.");
        }

        // If custom format is specified, parse strictly with that format
        if (!string.IsNullOrWhiteSpace(customDateFormat))
        {
            string normalizedFormat = NormalizeDateFormat(customDateFormat.Trim());
            if (!DateTime.TryParseExact(trimmed, normalizedFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedCustom))
            {
                throw new FilterValidationException($"Date '{rawDate}' does not match the configured date format '{customDateFormat}'.");
            }
            return parsedCustom;
        }

        // Otherwise use default accepted formats
        if (!DateTime.TryParseExact(trimmed, DefaultDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDefault))
        {
            throw new FilterValidationException($"Date '{rawDate}' for '{op}' is not in a valid format. Expected MM/dd/yyyy or yyyy/MM/dd.");
        }

        return parsedDefault;
    }

    public static string ValidateAndNormalizeDuration(string op, string rawDuration)
    {
        ValidateNonEmpty(op, rawDuration);

        string trimmed = rawDuration.Trim();
        var match = DurationRegex.Match(trimmed);
        if (!match.Success)
        {
            throw new FilterValidationException($"Invalid duration '{rawDuration}' for '{op}'. Expected a positive integer followed by d (days), m (months), or y (years), e.g. '2d', '1m', '1y'.");
        }

        string count = match.Groups[1].Value;
        string unit = match.Groups[2].Value.ToLowerInvariant();
        return $"{count}{unit}";
    }

    public static string NormalizeDateFormat(string format)
    {
        return format
            .Replace("YYYY", "yyyy")
            .Replace("YY", "yy")
            .Replace("DD", "dd");
    }
}
