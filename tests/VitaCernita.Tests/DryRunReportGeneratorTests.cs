using System;
using System.Collections.Generic;
using VitaCernita.Core.Actions;
using VitaCernita.Core.AutoReply;
using VitaCernita.Core.Diffing;
using VitaCernita.Core.Diffing.AutoReply;
using VitaCernita.Core.Diffing.Filters;
using VitaCernita.Core.Diffing.Labels;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Queries;
using VitaCernita.Core.Reporting;
using VitaCernita.Core.Sync;
using VitaCernita.Core.Sync.Commands;
using Xunit;
using AutoReplyModel = VitaCernita.Core.AutoReply.AutoReply;

namespace VitaCernita.Tests;

public class DryRunReportGeneratorTests
{
    #region Unified CreateReport Tests

    [Fact]
    public void CreateReport_WithAllComponents_GeneratesCombinedReport()
    {
        // 1. Labels
        var currentLabels = new List<GmailLabel> { new("Receipts", id: "L_REC") };
        var desiredLabels = new List<GmailLabel> { new("Receipts", id: "L_REC"), new("Work") };
        var labelDiff = GmailLabelDiffer.DiffSets(currentLabels, desiredLabels);

        // 2. Filters
        var currentFilters = new List<GmailFilter>
        {
            new("f_1", new FieldCondition("from", "old@test.com"), new GmailAction().Archive())
        };
        var desiredFilters = new List<GmailFilter>
        {
            new("f_1", new FieldCondition("from", "old@test.com"), new GmailAction().Star())
        };
        var filterDiff = GmailFilterDiffer.DiffSets(currentFilters, desiredFilters);

        // 3. AutoReply
        var desiredAutoReply = new AutoReplyModel(true, "Vacation", "Away on leave");
        var autoReplyDiff = AutoReplyDiffer.Diff(null, desiredAutoReply);

        // 4. SyncPlan
        var plan = new SyncPlan(new ISyncCommand[]
        {
            new CreateLabelCommand(new GmailLabel("Work")),
            new UpdateFilterCommand("f_1", currentFilters[0], desiredFilters[0], new[]
            {
                new FieldDiff("action", "archive", "star")
            })
        });

        // Act
        string report = DryRunReportGenerator.CreateReport(labelDiff, filterDiff, autoReplyDiff, plan);

        // Assert - contains all 4 headers and details
        Assert.Contains("VitaCernita Label Diff Report (Dry Run)", report);
        Assert.Contains("+ 'Work'", report);

        Assert.Contains("VitaCernita Filter Diff Report (Dry Run)", report);
        Assert.Contains("[~] Update (1):", report);

        Assert.Contains("Auto-Reply (Vacation Responder) Diff:", report);
        Assert.Contains("+ Enable Auto-Reply", report);
        Assert.Contains("Vacation", report);

        Assert.Contains("VitaCernita Synchronization Plan (Dry Run)", report);
        Assert.Contains("1. [+] Create Label 'Work'", report);
    }

    [Fact]
    public void CreateReport_AllNull_ReturnsEmptyString()
    {
        string report = DryRunReportGenerator.CreateReport(null, null, null, null);
        Assert.Equal(string.Empty, report);
    }

    [Fact]
    public void CreateReport_AutoReplyUnchanged_OmitsAutoReplySection()
    {
        var current = new AutoReplyModel(true, "Vacation", "Away");
        var desired = new AutoReplyModel(true, "Vacation", "Away");
        var autoReplyDiff = AutoReplyDiffer.Diff(current, desired);

        var labelDiff = GmailLabelDiffer.DiffSets(
            new List<GmailLabel>(),
            new List<GmailLabel> { new("WorkTasks") });

        string report = DryRunReportGenerator.CreateReport(labelDiff, null, autoReplyDiff, null);

        Assert.Contains("VitaCernita Label Diff Report (Dry Run)", report);
        Assert.DoesNotContain("Auto-Reply", report);
    }

    [Fact]
    public void CreateReport_AutoReplyAccountError_IncludesAutoReplySection()
    {
        var current = new AutoReplyModel(false);
        var desired = new AutoReplyModel(true, "Subject", "Body", restrictToDomain: true);
        var options = new AutoReplyDiffOptions { TargetAccount = "user@gmail.com" };
        var diff = AutoReplyDiffer.Diff(current, desired, options);

        string report = DryRunReportGenerator.CreateReport(null, null, diff, null);

        Assert.Contains("Auto-Reply (Vacation Responder) Diff:", report);
        Assert.Contains("[ERROR]", report);
        Assert.Contains("only valid for Google Workspace accounts", report);
    }

    #endregion

    #region Label Report Tests

    [Fact]
    public void CreateLabelReport_FormatsSummaryAndDetailsProperly()
    {
        var current = new List<GmailLabel>
        {
            new("Updates", id: "L_UPD", messageListVisibility: "show"),
            new("DeprecatedTag", id: "L_DEP")
        };

        var desired = new List<GmailLabel>
        {
            new("Updates", messageListVisibility: "hide"),
            new("FreshTag", messageListVisibility: "show", labelListVisibility: "labelShow",
                color: new LabelColor("#ffffff", "#000000"))
        };

        var diffSet = GmailLabelDiffer.DiffSets(current, desired);
        string report = DryRunReportGenerator.CreateLabelReport(diffSet);

        Assert.Contains("VitaCernita Label Diff Report (Dry Run)", report);
        Assert.Contains("Summary: 1 to create, 1 to update, 1 to delete, 0 unchanged.", report);

        Assert.Contains("[+] Create (1):", report);
        Assert.Contains("+ 'FreshTag' (MessageList: show, LabelList: labelShow, Color: [#ffffff / #000000])", report);

        Assert.Contains("[~] Update (1):", report);
        Assert.Contains("~ 'Updates' (ID: L_UPD):", report);
        Assert.Contains("messageListVisibility: show -> hide", report);

        Assert.Contains("[-] Delete (1):", report);
        Assert.Contains("- 'DeprecatedTag' (ID: L_DEP)", report);
    }

    [Fact]
    public void CreateLabelReport_UnchangedLabels_FormatsProperly()
    {
        var labels = new List<GmailLabel> { new("InboxTag", id: "L_INB") };
        var diffSet = GmailLabelDiffer.DiffSets(labels, labels);

        string report = DryRunReportGenerator.CreateLabelReport(diffSet);
        Assert.Contains("[=] Unchanged (1):", report);
        Assert.Contains("= 'InboxTag' (ID: L_INB)", report);
    }

    #endregion

    #region Filter Report Tests

    [Fact]
    public void CreateFilterReport_OutputsFormattedSummary()
    {
        var currentFilters = new List<GmailFilter>
        {
            new("f_1", new FieldCondition("from", "a@example.com"), new GmailAction().Star()),
            new("f_del", new FieldCondition("from", "old@example.com"), new GmailAction().Archive())
        };

        var desiredFilters = new List<GmailFilter>
        {
            new("f_1", new FieldCondition("from", "a@example.com"), new GmailAction().Star().Archive()),
            new("f_new", new FieldCondition("from", "new@example.com"), new GmailAction().Delete())
        };

        var setDiff = GmailFilterDiffer.DiffSets(currentFilters, desiredFilters);
        string report = DryRunReportGenerator.CreateFilterReport(setDiff);

        Assert.Contains("VitaCernita Filter Diff Report (Dry Run)", report);
        Assert.Contains("Summary: 1 to create, 1 to update, 1 to delete, 0 unchanged.", report);
        Assert.Contains("[+] Create (1):", report);
        Assert.Contains("f_new", report);
        Assert.Contains("Query : from:new@example.com", report);
        Assert.Contains("Action: delete", report);

        Assert.Contains("[~] Update (1):", report);
        Assert.Contains("f_1", report);

        Assert.Contains("[-] Delete (1):", report);
        Assert.Contains("f_del", report);
    }

    [Fact]
    public void CreateFilterReport_UnchangedFilters_FormatsProperly()
    {
        var filters = new List<GmailFilter>
        {
            new("f_same", new FieldCondition("from", "same@example.com"), new GmailAction().Star(), name: "SameFilter")
        };
        var setDiff = GmailFilterDiffer.DiffSets(filters, filters);

        string report = DryRunReportGenerator.CreateFilterReport(setDiff);
        Assert.Contains("[=] Unchanged (1):", report);
        Assert.Contains("= Filter 'SameFilter' (ID: f_same)", report);
    }

    #endregion

    #region AutoReply Report Tests

    [Fact]
    public void CreateAutoReplyReport_Added_OutputsReadableSummary()
    {
        var start = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 12, 10, 0, 0, 0, DateTimeKind.Utc);
        var desired = new AutoReplyModel(
            enableAutoReply: true,
            responseSubject: "Holiday",
            responseBodyPlainText: "Away until next week",
            responseBodyHtml: "<b>Away</b> until next week",
            restrictToContacts: true,
            restrictToDomain: false,
            startTime: new DateTimeOffset(start).ToUnixTimeMilliseconds(),
            endTime: new DateTimeOffset(end).ToUnixTimeMilliseconds());

        var diff = AutoReplyDiffer.Diff(null, desired);
        string report = DryRunReportGenerator.CreateAutoReplyReport(diff);

        Assert.Contains("Auto-Reply (Vacation Responder) Diff:", report);
        Assert.Contains("Status: + Enable Auto-Reply (Create / Turn On)", report);
        Assert.Contains("Subject     : 'Holiday'", report);
        Assert.Contains("Body (Plain): 'Away until next week'", report);
        Assert.Contains("Body (HTML) : '<b>Away</b> until next week'", report);
        Assert.Contains("ContactsOnly: True", report);
        Assert.Contains("DomainOnly  : False", report);
        Assert.Contains("Start Time  : 2026-12-01 00:00:00 UTC", report);
        Assert.Contains("End Time    : 2026-12-10 00:00:00 UTC", report);
    }

    [Fact]
    public void CreateAutoReplyReport_Modified_OutputsChanges()
    {
        var current = new AutoReplyModel(true, "Old Subject", "Old Body");
        var desired = new AutoReplyModel(true, "New Subject", "New Body");
        var diff = AutoReplyDiffer.Diff(current, desired);

        string report = DryRunReportGenerator.CreateAutoReplyReport(diff);

        Assert.Contains("Auto-Reply (Vacation Responder) Diff:", report);
        Assert.Contains("Status: ~ Update Auto-Reply (2 change(s)):", report);
        Assert.Contains("responseSubject: Old Subject -> New Subject", report);
        Assert.Contains("responseBodyPlainText: Old Body -> New Body", report);
    }

    [Fact]
    public void CreateAutoReplyReport_Disabled_OutputsPreviousSubject()
    {
        var current = new AutoReplyModel(true, "Old Subject", "Body");
        var desired = new AutoReplyModel(false);
        var diff = AutoReplyDiffer.Diff(current, desired);

        string report = DryRunReportGenerator.CreateAutoReplyReport(diff);

        Assert.Contains("Auto-Reply (Vacation Responder) Diff:", report);
        Assert.Contains("Status: - Turn Off Auto-Reply", report);
        Assert.Contains("Previous Subject: 'Old Subject'", report);
    }

    [Fact]
    public void CreateAutoReplyReport_Unchanged_OutputsActiveSubject()
    {
        var current = new AutoReplyModel(true, "Active Vacation", "Body");
        var diff = AutoReplyDiffer.Diff(current, current);

        string report = DryRunReportGenerator.CreateAutoReplyReport(diff);

        Assert.Contains("Auto-Reply (Vacation Responder) Diff:", report);
        Assert.Contains("Status: Unchanged", report);
        Assert.Contains("Active Subject: 'Active Vacation'", report);
    }

    #endregion

    #region SyncPlan Report Tests

    [Fact]
    public void CreateSyncPlanReport_FormatsCleanReadableReport()
    {
        var label = new GmailLabel("Receipts", messageListVisibility: "show");
        var cmd1 = new CreateLabelCommand(label);

        var currentFilter = new GmailFilter("f_1", new FieldCondition("from", "a@b.com"), new GmailAction().Star());
        var desiredFilter = new GmailFilter("f_1", new FieldCondition("from", "a@b.com"), new GmailAction().Star().Archive());
        var cmd2 = new UpdateFilterCommand("f_1", currentFilter, desiredFilter, new[]
        {
            new FieldDiff("action", "star", "archive, star")
        });

        var plan = new SyncPlan(new ISyncCommand[] { cmd1, cmd2 });
        string report = DryRunReportGenerator.CreateSyncPlanReport(plan);

        Assert.Contains("VitaCernita Synchronization Plan (Dry Run)", report);
        Assert.Contains("Sync Plan (Make Right match Left): 2 commands (1 create, 1 update, 0 delete).", report);
        Assert.Contains("Planned Execution Steps (Dependency-Safe Order):", report);
        Assert.Contains("1. [+] Create Label 'Receipts'", report);
        Assert.Contains("2. [~] Update Filter (ID: f_1): 1 change(s)", report);
        Assert.Contains("action: 'star' -> 'archive, star'", report);
    }

    [Fact]
    public void CreateSyncPlanReport_EmptyPlan_OutputsInSyncMessage()
    {
        var plan = new SyncPlan(Array.Empty<ISyncCommand>());
        string report = DryRunReportGenerator.CreateSyncPlanReport(plan);

        Assert.Contains("VitaCernita Synchronization Plan (Dry Run)", report);
        Assert.Contains("No changes required. Configurations are completely in sync.", report);
    }

    #endregion
}
