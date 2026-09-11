using System;
using System.Collections.Generic;
using VitaCernita.Core.AutoReply.Validation;

namespace VitaCernita.Core.AutoReply.Diff;

/// <summary>
/// Compares current Gmail AutoReply (VacationSettings) configurations against desired configurations.
/// </summary>
public static class AutoReplyDiffer
{
    /// <summary>
    /// Computes the difference between an existing AutoReply setting and a desired AutoReply specification.
    /// </summary>
    public static AutoReplyDiff Diff(
        AutoReply? current,
        AutoReply? desired,
        AutoReplyDiffOptions? options = null)
    {
        options ??= new AutoReplyDiffOptions();

        string? accountError = null;

        // Check Google Workspace vs standard Gmail account domain restriction
        if (desired != null && desired.RestrictToDomain && !string.IsNullOrWhiteSpace(options.TargetAccount))
        {
            if (AutoReplyValidator.IsStandardGmailAccount(options.TargetAccount))
            {
                if (options.StrictAccountValidation)
                {
                    AutoReplyValidator.ValidateForAccount(desired, options.TargetAccount);
                }
                else
                {
                    accountError = $"The 'restrictToDomain' setting is only valid for Google Workspace accounts, but target account '{options.TargetAccount}' is a standard Gmail account (@gmail.com).";
                }
            }
        }

        if (current == null && desired == null)
        {
            return new AutoReplyDiff(AutoReplyDiffType.Unchanged, null, null, accountError: accountError);
        }

        if (current == null)
        {
            var diffType = desired!.EnableAutoReply ? AutoReplyDiffType.Added : AutoReplyDiffType.Unchanged;
            return new AutoReplyDiff(diffType, null, desired, accountError: accountError);
        }

        if (desired == null)
        {
            var diffType = current.EnableAutoReply ? AutoReplyDiffType.Disabled : AutoReplyDiffType.Unchanged;
            return new AutoReplyDiff(diffType, current, null, accountError: accountError);
        }

        var fieldDiffs = new List<AutoReplyFieldDiff>();

        // 1. EnableAutoReply
        if (options.ShouldCompareField("enableAutoReply"))
        {
            if (current.EnableAutoReply != desired.EnableAutoReply)
            {
                fieldDiffs.Add(new AutoReplyFieldDiff("enableAutoReply", current.EnableAutoReply, desired.EnableAutoReply));
            }
        }

        // 2. ResponseSubject
        if (options.ShouldCompareField("responseSubject"))
        {
            if (!(options.IgnoreUnsetDesiredFields && desired.ResponseSubject == null))
            {
                string curSub = current.ResponseSubject ?? string.Empty;
                string desSub = desired.ResponseSubject ?? string.Empty;
                if (!string.Equals(curSub, desSub, StringComparison.Ordinal))
                {
                    fieldDiffs.Add(new AutoReplyFieldDiff("responseSubject", current.ResponseSubject, desired.ResponseSubject));
                }
            }
        }

        // 3. ResponseBodyPlainText
        if (options.ShouldCompareField("responseBodyPlainText"))
        {
            if (!(options.IgnoreUnsetDesiredFields && desired.ResponseBodyPlainText == null))
            {
                string curPlain = current.ResponseBodyPlainText ?? string.Empty;
                string desPlain = desired.ResponseBodyPlainText ?? string.Empty;
                if (!string.Equals(curPlain, desPlain, StringComparison.Ordinal))
                {
                    fieldDiffs.Add(new AutoReplyFieldDiff("responseBodyPlainText", current.ResponseBodyPlainText, desired.ResponseBodyPlainText));
                }
            }
        }

        // 4. ResponseBodyHtml
        if (options.ShouldCompareField("responseBodyHtml"))
        {
            if (!(options.IgnoreUnsetDesiredFields && desired.ResponseBodyHtml == null))
            {
                string curHtml = current.ResponseBodyHtml ?? string.Empty;
                string desHtml = desired.ResponseBodyHtml ?? string.Empty;
                if (!string.Equals(curHtml, desHtml, StringComparison.Ordinal))
                {
                    fieldDiffs.Add(new AutoReplyFieldDiff("responseBodyHtml", current.ResponseBodyHtml, desired.ResponseBodyHtml));
                }
            }
        }

        // 5. RestrictToContacts
        if (options.ShouldCompareField("restrictToContacts"))
        {
            if (current.RestrictToContacts != desired.RestrictToContacts)
            {
                fieldDiffs.Add(new AutoReplyFieldDiff("restrictToContacts", current.RestrictToContacts, desired.RestrictToContacts));
            }
        }

        // 6. RestrictToDomain
        if (options.ShouldCompareField("restrictToDomain"))
        {
            if (current.RestrictToDomain != desired.RestrictToDomain)
            {
                fieldDiffs.Add(new AutoReplyFieldDiff("restrictToDomain", current.RestrictToDomain, desired.RestrictToDomain));
            }
        }

        // 7. StartTime
        if (options.ShouldCompareField("startTime"))
        {
            if (!(options.IgnoreUnsetDesiredFields && desired.StartTime == null))
            {
                if (current.StartTime != desired.StartTime)
                {
                    fieldDiffs.Add(new AutoReplyFieldDiff("startTime", current.StartTime, desired.StartTime));
                }
            }
        }

        // 8. EndTime
        if (options.ShouldCompareField("endTime"))
        {
            if (!(options.IgnoreUnsetDesiredFields && desired.EndTime == null))
            {
                if (current.EndTime != desired.EndTime)
                {
                    fieldDiffs.Add(new AutoReplyFieldDiff("endTime", current.EndTime, desired.EndTime));
                }
            }
        }

        AutoReplyDiffType overallDiff;
        if (!current.EnableAutoReply && desired.EnableAutoReply)
        {
            overallDiff = AutoReplyDiffType.Added;
        }
        else if (current.EnableAutoReply && !desired.EnableAutoReply)
        {
            overallDiff = AutoReplyDiffType.Disabled;
        }
        else if (fieldDiffs.Count > 0)
        {
            overallDiff = AutoReplyDiffType.Modified;
        }
        else
        {
            overallDiff = AutoReplyDiffType.Unchanged;
        }

        return new AutoReplyDiff(overallDiff, current, desired, fieldDiffs, accountError: accountError);
    }

    /// <summary>
    /// Overload for comparing dictionary representations directly.
    /// </summary>
    public static AutoReplyDiff DiffDictionaries(
        IReadOnlyDictionary<string, object?>? currentDict,
        IReadOnlyDictionary<string, object?>? desiredDict,
        AutoReplyDiffOptions? options = null)
    {
        var current = currentDict != null ? AutoReply.FromDictionary(currentDict) : null;
        var desired = desiredDict != null ? AutoReply.FromDictionary(desiredDict) : null;
        return Diff(current, desired, options);
    }

    /// <summary>
    /// Overload for comparing JSON string representations directly.
    /// </summary>
    public static AutoReplyDiff DiffJson(
        string? currentJson,
        string? desiredJson,
        AutoReplyDiffOptions? options = null)
    {
        var current = !string.IsNullOrWhiteSpace(currentJson) ? AutoReply.FromJson(currentJson) : null;
        var desired = !string.IsNullOrWhiteSpace(desiredJson) ? AutoReply.FromJson(desiredJson) : null;
        return Diff(current, desired, options);
    }
}
