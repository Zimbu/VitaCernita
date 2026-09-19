using System;
using System.Collections.Generic;
using VitaCernita.Core.AutoReply.Validation;
using AutoReplyModel = VitaCernita.Core.AutoReply.AutoReply;

namespace VitaCernita.Core.Diffing.AutoReply;

/// <summary>
/// Compares current Gmail AutoReply (VacationSettings) configurations against desired configurations.
/// Implements pure model comparison without Gmail API transport or execution dependencies.
/// </summary>
public static class AutoReplyDiffer
{
    /// <summary>
    /// Computes the difference between an existing AutoReply setting and a desired AutoReply specification.
    /// </summary>
    public static AutoReplyDiff Diff(
        AutoReplyModel? current,
        AutoReplyModel? desired,
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
            return new AutoReplyDiff(DiffKind.Unchanged, null, null, accountError: accountError);
        }

        if (current == null)
        {
            var diffType = desired!.EnableAutoReply ? DiffKind.Added : DiffKind.Unchanged;
            return new AutoReplyDiff(diffType, null, desired, accountError: accountError);
        }

        if (desired == null)
        {
            var diffType = current.EnableAutoReply ? DiffKind.Disabled : DiffKind.Unchanged;
            return new AutoReplyDiff(diffType, current, null, accountError: accountError);
        }

        var fieldDiffs = new List<FieldDiff>();

        // 1. EnableAutoReply
        if (options.ShouldCompareField("enableAutoReply"))
        {
            if (current.EnableAutoReply != desired.EnableAutoReply)
            {
                fieldDiffs.Add(new FieldDiff("enableAutoReply", current.EnableAutoReply, desired.EnableAutoReply));
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
                    fieldDiffs.Add(new FieldDiff("responseSubject", current.ResponseSubject, desired.ResponseSubject));
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
                    fieldDiffs.Add(new FieldDiff("responseBodyPlainText", current.ResponseBodyPlainText, desired.ResponseBodyPlainText));
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
                    fieldDiffs.Add(new FieldDiff("responseBodyHtml", current.ResponseBodyHtml, desired.ResponseBodyHtml));
                }
            }
        }

        // 5. RestrictToContacts
        if (options.ShouldCompareField("restrictToContacts"))
        {
            if (current.RestrictToContacts != desired.RestrictToContacts)
            {
                fieldDiffs.Add(new FieldDiff("restrictToContacts", current.RestrictToContacts, desired.RestrictToContacts));
            }
        }

        // 6. RestrictToDomain
        if (options.ShouldCompareField("restrictToDomain"))
        {
            if (current.RestrictToDomain != desired.RestrictToDomain)
            {
                fieldDiffs.Add(new FieldDiff("restrictToDomain", current.RestrictToDomain, desired.RestrictToDomain));
            }
        }

        // 7. StartTime
        if (options.ShouldCompareField("startTime"))
        {
            if (!(options.IgnoreUnsetDesiredFields && desired.StartTime == null))
            {
                if (current.StartTime != desired.StartTime)
                {
                    fieldDiffs.Add(new FieldDiff("startTime", current.StartTime, desired.StartTime));
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
                    fieldDiffs.Add(new FieldDiff("endTime", current.EndTime, desired.EndTime));
                }
            }
        }

        DiffKind overallDiff;
        if (!current.EnableAutoReply && desired.EnableAutoReply)
        {
            overallDiff = DiffKind.Added;
        }
        else if (current.EnableAutoReply && !desired.EnableAutoReply)
        {
            overallDiff = DiffKind.Disabled;
        }
        else if (fieldDiffs.Count > 0)
        {
            overallDiff = DiffKind.Modified;
        }
        else
        {
            overallDiff = DiffKind.Unchanged;
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
        var current = currentDict != null ? AutoReplyModel.FromDictionary(currentDict) : null;
        var desired = desiredDict != null ? AutoReplyModel.FromDictionary(desiredDict) : null;
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
        var current = !string.IsNullOrWhiteSpace(currentJson) ? AutoReplyModel.FromJson(currentJson) : null;
        var desired = !string.IsNullOrWhiteSpace(desiredJson) ? AutoReplyModel.FromJson(desiredJson) : null;
        return Diff(current, desired, options);
    }
}
