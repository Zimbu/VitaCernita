using System;
using System.Collections.Generic;
using System.Text.Json;
using VitaCernita.Core.AutoReply.Validation;

namespace VitaCernita.Core.AutoReply;

/// <summary>
/// Represents vacation responder settings matching the Google Workspace Gmail API VacationSettings resource.
/// API Resource: https://developers.google.com/workspace/gmail/api/reference/rest/v1/VacationSettings
/// </summary>
public class AutoReply : IEquatable<AutoReply>
{
    /// <summary>
    /// Flag that controls whether Gmail automatically replies to messages.
    /// </summary>
    public bool EnableAutoReply { get; set; }

    /// <summary>
    /// Optional text to prepend to the subject line in vacation responses.
    /// In order to enable auto-replies, either the response subject or the response body must be nonempty.
    /// </summary>
    public string? ResponseSubject { get; set; }

    /// <summary>
    /// Response body in plain text format. If both responseBodyPlainText and responseBodyHtml are specified,
    /// responseBodyHtml will be used by Gmail.
    /// </summary>
    public string? ResponseBodyPlainText { get; set; }

    /// <summary>
    /// Response body in HTML format. Gmail will sanitize the HTML before storing it.
    /// If both responseBodyPlainText and responseBodyHtml are specified, responseBodyHtml will be used by Gmail.
    /// </summary>
    public string? ResponseBodyHtml { get; set; }

    /// <summary>
    /// Flag that determines whether responses are sent only to recipients who are in the user's list of contacts.
    /// </summary>
    public bool RestrictToContacts { get; set; }

    /// <summary>
    /// Flag that determines whether responses are sent only to recipients within the user's domain.
    /// Note: This feature is only available for Google Workspace users (not standard @gmail.com accounts).
    /// </summary>
    public bool RestrictToDomain { get; set; }

    /// <summary>
    /// An optional start time for sending auto-replies (epoch ms).
    /// When specified, Gmail will automatically reply only to messages received after this time.
    /// </summary>
    public long? StartTime { get; set; }

    /// <summary>
    /// An optional end time for sending auto-replies (epoch ms).
    /// When specified, Gmail will automatically reply only to messages received before this time.
    /// </summary>
    public long? EndTime { get; set; }

    /// <summary>
    /// Convenient DateTimeOffset representation of StartTime in UTC.
    /// </summary>
    public DateTimeOffset? StartDateTime =>
        StartTime.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(StartTime.Value) : null;

    /// <summary>
    /// Convenient DateTimeOffset representation of EndTime in UTC.
    /// </summary>
    public DateTimeOffset? EndDateTime =>
        EndTime.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(EndTime.Value) : null;

    /// <summary>
    /// Returns true if any response content (subject, plain body, or HTML body) is defined.
    /// </summary>
    public bool HasContent =>
        !string.IsNullOrWhiteSpace(ResponseSubject) ||
        !string.IsNullOrWhiteSpace(ResponseBodyPlainText) ||
        !string.IsNullOrWhiteSpace(ResponseBodyHtml);

    public AutoReply()
    {
    }

    public AutoReply(
        bool enableAutoReply = false,
        string? responseSubject = null,
        string? responseBodyPlainText = null,
        string? responseBodyHtml = null,
        bool restrictToContacts = false,
        bool restrictToDomain = false,
        long? startTime = null,
        long? endTime = null,
        bool validate = false)
    {
        EnableAutoReply = enableAutoReply;
        ResponseSubject = responseSubject;
        ResponseBodyPlainText = responseBodyPlainText;
        ResponseBodyHtml = responseBodyHtml;
        RestrictToContacts = restrictToContacts;
        RestrictToDomain = restrictToDomain;
        StartTime = startTime;
        EndTime = endTime;

        if (validate)
        {
            AutoReplyValidator.Validate(this);
        }
    }

    /// <summary>
    /// Serializes this instance to a dictionary adhering to the Gmail API VacationSettings schema.
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        var dict = new Dictionary<string, object>
        {
            ["enableAutoReply"] = EnableAutoReply,
            ["restrictToContacts"] = RestrictToContacts,
            ["restrictToDomain"] = RestrictToDomain
        };

        if (ResponseSubject != null)
        {
            dict["responseSubject"] = ResponseSubject;
        }

        if (ResponseBodyPlainText != null)
        {
            dict["responseBodyPlainText"] = ResponseBodyPlainText;
        }

        if (ResponseBodyHtml != null)
        {
            dict["responseBodyHtml"] = ResponseBodyHtml;
        }

        if (StartTime.HasValue)
        {
            dict["startTime"] = StartTime.Value.ToString();
        }

        if (EndTime.HasValue)
        {
            dict["endTime"] = EndTime.Value.ToString();
        }

        return dict;
    }

    /// <summary>
    /// Serializes this instance to a JSON string matching the Gmail API VacationSettings format.
    /// </summary>
    public string ToJson(bool writeIndented = false)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = writeIndented
        };
        return JsonSerializer.Serialize(ToDictionary(), options);
    }

    /// <summary>
    /// Parses a JSON string representing a Gmail API VacationSettings resource.
    /// </summary>
    public static AutoReply FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return FromJsonElement(doc.RootElement);
    }

    /// <summary>
    /// Parses a JsonElement representing a Gmail API VacationSettings resource.
    /// </summary>
    public static AutoReply FromJsonElement(JsonElement element)
    {
        var autoReply = new AutoReply();

        if (element.TryGetProperty("enableAutoReply", out var earProp) &&
            (earProp.ValueKind == JsonValueKind.True || earProp.ValueKind == JsonValueKind.False))
        {
            autoReply.EnableAutoReply = earProp.GetBoolean();
        }

        if (element.TryGetProperty("responseSubject", out var rsProp) && rsProp.ValueKind == JsonValueKind.String)
        {
            autoReply.ResponseSubject = rsProp.GetString();
        }

        if (element.TryGetProperty("responseBodyPlainText", out var rbpProp) && rbpProp.ValueKind == JsonValueKind.String)
        {
            autoReply.ResponseBodyPlainText = rbpProp.GetString();
        }

        if (element.TryGetProperty("responseBodyHtml", out var rbhProp) && rbhProp.ValueKind == JsonValueKind.String)
        {
            autoReply.ResponseBodyHtml = rbhProp.GetString();
        }

        if (element.TryGetProperty("restrictToContacts", out var rtcProp) &&
            (rtcProp.ValueKind == JsonValueKind.True || rtcProp.ValueKind == JsonValueKind.False))
        {
            autoReply.RestrictToContacts = rtcProp.GetBoolean();
        }

        if (element.TryGetProperty("restrictToDomain", out var rtdProp) &&
            (rtdProp.ValueKind == JsonValueKind.True || rtdProp.ValueKind == JsonValueKind.False))
        {
            autoReply.RestrictToDomain = rtdProp.GetBoolean();
        }

        if (element.TryGetProperty("startTime", out var stProp))
        {
            if (stProp.ValueKind == JsonValueKind.String && long.TryParse(stProp.GetString(), out long st))
            {
                autoReply.StartTime = st;
            }
            else if (stProp.ValueKind == JsonValueKind.Number && stProp.TryGetInt64(out long stNum))
            {
                autoReply.StartTime = stNum;
            }
        }

        if (element.TryGetProperty("endTime", out var etProp))
        {
            if (etProp.ValueKind == JsonValueKind.String && long.TryParse(etProp.GetString(), out long et))
            {
                autoReply.EndTime = et;
            }
            else if (etProp.ValueKind == JsonValueKind.Number && etProp.TryGetInt64(out long etNum))
            {
                autoReply.EndTime = etNum;
            }
        }

        return autoReply;
    }

    /// <summary>
    /// Parses a dictionary representing a Gmail API VacationSettings resource or DSL definition.
    /// </summary>
    public static AutoReply FromDictionary(IReadOnlyDictionary<string, object?> dict)
    {
        if (dict == null) throw new ArgumentNullException(nameof(dict));

        var autoReply = new AutoReply();

        autoReply.EnableAutoReply = GetBool(dict, "enableAutoReply", "enable_auto_reply", "enabled", "enable") ?? false;
        autoReply.ResponseSubject = GetString(dict, "responseSubject", "response_subject", "subject");
        autoReply.ResponseBodyPlainText = GetString(dict, "responseBodyPlainText", "response_body_plain_text", "body_plain", "body_text", "plain_text", "body", "text");
        autoReply.ResponseBodyHtml = GetString(dict, "responseBodyHtml", "response_body_html", "body_html", "html");
        autoReply.RestrictToContacts = GetBool(dict, "restrictToContacts", "restrict_to_contacts", "contacts_only", "contactsOnly", "restrict_contacts") ?? false;
        autoReply.RestrictToDomain = GetBool(dict, "restrictToDomain", "restrict_to_domain", "domain_only", "domainOnly", "restrict_domain", "workspace_only") ?? false;

        autoReply.StartTime = GetLong(dict, "startTime", "start_time", "start_date", "startDate", "start");
        autoReply.EndTime = GetLong(dict, "endTime", "end_time", "end_date", "endDate", "end");

        return autoReply;
    }

    private static string? GetString(IReadOnlyDictionary<string, object?> dict, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (dict.TryGetValue(key, out var val) && val != null)
            {
                if (val is JsonElement je)
                {
                    return je.ValueKind == JsonValueKind.String ? je.GetString() : je.ToString();
                }
                return val.ToString();
            }
        }
        return null;
    }

    private static bool? GetBool(IReadOnlyDictionary<string, object?> dict, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (dict.TryGetValue(key, out var val) && val != null)
            {
                if (val is bool b) return b;
                if (val is JsonElement je && (je.ValueKind == JsonValueKind.True || je.ValueKind == JsonValueKind.False))
                {
                    return je.GetBoolean();
                }
                if (bool.TryParse(val.ToString(), out bool parsed)) return parsed;
            }
        }
        return null;
    }

    private static long? GetLong(IReadOnlyDictionary<string, object?> dict, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (dict.TryGetValue(key, out var val) && val != null)
            {
                if (val is long l) return l;
                if (val is int i) return i;
                if (val is double d) return (long)d;
                if (val is JsonElement je)
                {
                    if (je.ValueKind == JsonValueKind.Number && je.TryGetInt64(out long jNum)) return jNum;
                    if (je.ValueKind == JsonValueKind.String && long.TryParse(je.GetString(), out long jStr)) return jStr;
                }
                if (long.TryParse(val.ToString(), out long parsed)) return parsed;
            }
        }
        return null;
    }

    public bool Equals(AutoReply? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return EnableAutoReply == other.EnableAutoReply &&
               string.Equals(ResponseSubject, other.ResponseSubject, StringComparison.Ordinal) &&
               string.Equals(ResponseBodyPlainText, other.ResponseBodyPlainText, StringComparison.Ordinal) &&
               string.Equals(ResponseBodyHtml, other.ResponseBodyHtml, StringComparison.Ordinal) &&
               RestrictToContacts == other.RestrictToContacts &&
               RestrictToDomain == other.RestrictToDomain &&
               StartTime == other.StartTime &&
               EndTime == other.EndTime;
    }

    public override bool Equals(object? obj) => obj is AutoReply other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(EnableAutoReply);
        hash.Add(ResponseSubject);
        hash.Add(ResponseBodyPlainText);
        hash.Add(ResponseBodyHtml);
        hash.Add(RestrictToContacts);
        hash.Add(RestrictToDomain);
        hash.Add(StartTime);
        hash.Add(EndTime);
        return hash.ToHashCode();
    }

    public override string ToString()
    {
        var parts = new List<string>
        {
            $"Enabled: {EnableAutoReply}"
        };

        if (!string.IsNullOrWhiteSpace(ResponseSubject))
        {
            parts.Add($"Subject: '{ResponseSubject}'");
        }

        if (!string.IsNullOrWhiteSpace(ResponseBodyPlainText))
        {
            string snippet = ResponseBodyPlainText.Length > 30 ? ResponseBodyPlainText[..27] + "..." : ResponseBodyPlainText;
            parts.Add($"Plain: '{snippet.Replace("\n", " ")}'");
        }

        if (!string.IsNullOrWhiteSpace(ResponseBodyHtml))
        {
            string snippet = ResponseBodyHtml.Length > 30 ? ResponseBodyHtml[..27] + "..." : ResponseBodyHtml;
            parts.Add($"Html: '{snippet.Replace("\n", " ")}'");
        }

        if (RestrictToContacts) parts.Add("ContactsOnly: true");
        if (RestrictToDomain) parts.Add("DomainOnly: true");
        if (StartTime.HasValue) parts.Add($"Start: {StartDateTime:yyyy-MM-dd HH:mm:ss} UTC");
        if (EndTime.HasValue) parts.Add($"End: {EndDateTime:yyyy-MM-dd HH:mm:ss} UTC");

        return $"AutoReply({string.Join(", ", parts)})";
    }
}
