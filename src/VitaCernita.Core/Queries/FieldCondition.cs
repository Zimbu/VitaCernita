using System;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Queries.Validation;

namespace VitaCernita.Core.Queries;

/// <summary>
/// Condition matching a specific email field (e.g. 'from', 'to', 'cc', 'bcc', 'subject', 'list', 'filename', 'label', etc.).
/// </summary>
public sealed class FieldCondition : IQueryCondition, IFilterCondition
{
    public string Field { get; }
    public string Value { get; }

    public FieldCondition(string field, string value)
    {
        if (field == null) throw new ArgumentNullException(nameof(field));
        Field = NormalizeField(field);

        if (IsEmailField(Field))
        {
            QueryValidator.ValidateEmailAddressOrFragment(Field, value);
        }
        else
        {
            QueryValidator.ValidateNonEmpty(Field, value);
        }

        Value = value.Trim();
    }

    private static bool IsEmailField(string field) =>
        field is "from" or "to" or "cc" or "bcc" or "deliveredto";

    public static string NormalizeField(string field)
    {
        string f = field.Trim().ToLowerInvariant();
        return f switch
        {
            "delivered-to" or "delivered_to" => "deliveredto",
            "rfc-822-msg-id" or "msgid" => "rfc822msgid",
            _ => f
        };
    }

    public string ToGmailQuery(bool explicitAnd = false)
    {
        if (Field == "header")
        {
            return FormatHeaderQuery(Value);
        }

        string formattedValue = FormatValue(Value);
        return $"{Field}:{formattedValue}";
    }

    private static string FormatHeaderQuery(string headerVal)
    {
        int colonIdx = headerVal.IndexOf(':');
        if (colonIdx > 0)
        {
            string headerName = headerVal[..colonIdx].Trim();
            string rawValue = headerVal[(colonIdx + 1)..].Trim();

            // Strip enclosing quotes if present
            if (rawValue.Length >= 2 && rawValue.StartsWith('"') && rawValue.EndsWith('"'))
            {
                rawValue = rawValue[1..^1];
            }

            if (rawValue.Contains(' ') || rawValue.Contains('\t') || rawValue.Contains('"'))
            {
                string escaped = rawValue.Replace("\"", "\\\"");
                return $"header:{headerName}:\"{escaped}\"";
            }
            return $"header:{headerName}:{rawValue}";
        }

        return $"header:{FormatValue(headerVal)}";
    }

    private static string FormatValue(string val)
    {
        if (string.IsNullOrEmpty(val))
        {
            return "\"\"";
        }

        // If wrapped in matching quotes already, strip them
        if (val.Length >= 2 && val.StartsWith('"') && val.EndsWith('"'))
        {
            val = val[1..^1];
        }

        // Quote if value contains whitespace or quotes
        if (val.Contains(' ') || val.Contains('\t') || val.Contains('"'))
        {
            string escaped = val.Replace("\"", "\\\"");
            return $"\"{escaped}\"";
        }

        return val;
    }

    public override string ToString() => ToGmailQuery();
}
