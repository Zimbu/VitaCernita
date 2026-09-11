using System;
using System.Globalization;
using Lua;
using VitaCernita.Core.AutoReply.Validation;

namespace VitaCernita.Core.AutoReply;

/// <summary>
/// Parses Lua tables and values into AutoReply domain model instances.
/// </summary>
public static class AutoReplyParser
{
    private static readonly string[] StandardDateFormats =
    {
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "MM/dd/yyyy",
        "yyyy-MM-ddTHH:mm:ssZ",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.fffZ",
        "yyyy-MM-ddTHH:mm:ss.fff"
    };

    public static bool IsAutoReplyTable(LuaTable table)
    {
        if (table.TryGetValue("type", out var typeVal))
        {
            string t = typeVal.ToString();
            if (t is "auto_reply" or "autoreply" or "auto_reply_builder" or "vacation" or "vacation_settings")
            {
                return true;
            }
        }

        if (table.TryGetValue("_data", out var dataVal) && dataVal.TryRead<LuaTable>(out var dataTable))
        {
            return IsAutoReplyTable(dataTable);
        }

        // If it defines filters, rules, labels, query, or action, it is a root config table, rule, or filter
        if (table.TryGetValue("filters", out _) || table.TryGetValue("rules", out _) ||
            table.TryGetValue("labels", out _) || table.TryGetValue("query", out _) ||
            table.TryGetValue("action", out _) || table.TryGetValue("criteria", out _))
        {
            return false;
        }

        // Check for presence of characteristic vacation settings keys or DSL aliases
        return table.TryGetValue("enable_auto_reply", out _) ||
               table.TryGetValue("enableAutoReply", out _) ||
               table.TryGetValue("response_subject", out _) ||
               table.TryGetValue("responseSubject", out _) ||
               table.TryGetValue("response_body_plain_text", out _) ||
               table.TryGetValue("responseBodyPlainText", out _) ||
               table.TryGetValue("response_body_html", out _) ||
               table.TryGetValue("responseBodyHtml", out _) ||
               table.TryGetValue("restrict_to_contacts", out _) ||
               table.TryGetValue("restrictToContacts", out _) ||
               table.TryGetValue("restrict_to_domain", out _) ||
               table.TryGetValue("restrictToDomain", out _) ||
               table.TryGetValue("contacts_only", out _) ||
               table.TryGetValue("domain_only", out _) ||
               (table.TryGetValue("subject", out _) && (table.TryGetValue("body", out _) || table.TryGetValue("enabled", out _) || table.TryGetValue("plain_text", out _) || table.TryGetValue("html", out _))) ||
               ((table.TryGetValue("body", out _) || table.TryGetValue("html", out _)) && table.TryGetValue("enabled", out _));
    }

    public static AutoReply ParseAutoReply(LuaTable table, string? customDateFormat = null)
    {
        if (table.TryGetValue("_data", out var dataVal) && dataVal.TryRead<LuaTable>(out var dataTable))
        {
            return ParseAutoReply(dataTable, customDateFormat);
        }

        var autoReply = new AutoReply();

        // 1. Enable Auto Reply
        if (TryGetBool(table, out bool enabled, "enable_auto_reply", "enableAutoReply", "enabled", "enable"))
        {
            autoReply.EnableAutoReply = enabled;
        }
        else if (table.TryGetValue("type", out var tVal) && tVal.ToString() is "auto_reply" or "autoreply" or "vacation")
        {
            // If explicitly declared via auto_reply { ... } without explicit enabled flag, default to enabled if content is present
            autoReply.EnableAutoReply = true;
        }

        // 2. Response Subject
        if (TryGetString(table, out string? subject, "response_subject", "responseSubject", "subject"))
        {
            autoReply.ResponseSubject = subject;
        }

        // 3. Plain Text Body
        if (TryGetString(table, out string? plainText, "response_body_plain_text", "responseBodyPlainText", "body_plain", "body_text", "plain_text", "body", "text"))
        {
            autoReply.ResponseBodyPlainText = plainText;
        }

        // 4. HTML Body
        if (TryGetString(table, out string? html, "response_body_html", "responseBodyHtml", "body_html", "html"))
        {
            autoReply.ResponseBodyHtml = html;
        }

        // 5. Restrict to Contacts
        if (TryGetBool(table, out bool contactsOnly, "restrict_to_contacts", "restrictToContacts", "contacts_only", "contactsOnly", "restrict_contacts"))
        {
            autoReply.RestrictToContacts = contactsOnly;
        }

        // 6. Restrict to Domain (Workspace accounts only)
        if (TryGetBool(table, out bool domainOnly, "restrict_to_domain", "restrictToDomain", "domain_only", "domainOnly", "restrict_domain", "workspace_only"))
        {
            autoReply.RestrictToDomain = domainOnly;
        }

        // 7. Start Time
        if (TryGetTimestamp(table, customDateFormat, out long? startTime, "start_time", "startTime", "start_date", "startDate", "start"))
        {
            autoReply.StartTime = startTime;
        }

        // 8. End Time
        if (TryGetTimestamp(table, customDateFormat, out long? endTime, "end_time", "endTime", "end_date", "endDate", "end"))
        {
            autoReply.EndTime = endTime;
        }

        // Validate basic rules (e.g. subject/body required if enabled, startTime <= endTime)
        AutoReplyValidator.Validate(autoReply);

        return autoReply;
    }

    private static bool TryGetString(LuaTable table, out string? result, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (table.TryGetValue(key, out var val) && val.Type != LuaValueType.Nil)
            {
                result = val.ToString();
                return true;
            }
        }
        result = null;
        return false;
    }

    private static bool TryGetBool(LuaTable table, out bool result, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (table.TryGetValue(key, out var val) && val.Type != LuaValueType.Nil)
            {
                if (val.TryRead<bool>(out bool b))
                {
                    result = b;
                    return true;
                }
                if (bool.TryParse(val.ToString(), out bool parsed))
                {
                    result = parsed;
                    return true;
                }
            }
        }
        result = false;
        return false;
    }

    private static bool TryGetTimestamp(LuaTable table, string? customDateFormat, out long? result, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (table.TryGetValue(key, out var val) && val.Type != LuaValueType.Nil)
            {
                result = ParseTimestamp(val, customDateFormat);
                return true;
            }
        }
        result = null;
        return false;
    }

    public static long ParseTimestamp(LuaValue val, string? customDateFormat)
    {
        // 1. Number (epoch milliseconds or seconds)
        if (val.TryRead<double>(out double num))
        {
            long l = (long)num;
            // If less than 10 billion (e.g. 1760000000 seconds), assume seconds and convert to ms
            if (l > 0 && l < 10_000_000_000)
            {
                return l * 1000;
            }
            return l;
        }

        // 2. String representation
        string raw = val.ToString().Trim();

        // 2a. All digits numeric string (epoch ms or seconds)
        if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long epochNum))
        {
            if (epochNum > 0 && epochNum < 10_000_000_000)
            {
                return epochNum * 1000;
            }
            return epochNum;
        }

        // 2b. Custom date format if configured
        if (!string.IsNullOrWhiteSpace(customDateFormat))
        {
            if (DateTime.TryParseExact(raw, customDateFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime customDt))
            {
                return new DateTimeOffset(customDt, TimeSpan.Zero).ToUnixTimeMilliseconds();
            }
        }

        // 2c. Standard date formats
        if (DateTime.TryParseExact(raw, StandardDateFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime parsedExact))
        {
            return new DateTimeOffset(parsedExact, TimeSpan.Zero).ToUnixTimeMilliseconds();
        }

        // 2d. General DateTimeOffset parse
        if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset parsedDto))
        {
            return parsedDto.ToUnixTimeMilliseconds();
        }

        throw new AutoReplyValidationException(
            $"Unable to parse date/time value '{raw}' for auto-reply. Expected epoch ms, YYYY-MM-DD, or ISO 8601 format.");
    }
}
