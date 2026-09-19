using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VitaCernita.Core.Actions;
using VitaCernita.Core.Api.Fakes;
using AutoReplyModel = VitaCernita.Core.AutoReply.AutoReply;
using VitaCernita.Core.Diffing;
using VitaCernita.Core.Diffing.AutoReply;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Diffing.Filters;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Diffing.Labels;
using VitaCernita.Core.Queries;
using VitaCernita.Core.Sources;
using VitaCernita.Core.Sync;
using VitaCernita.Core.Sync.Commands;
using Xunit;

namespace VitaCernita.Tests;

public class GmailSyncPlannerTests
{
    [Fact]
    public void BuildPlan_WithAlteredFilterMatchedById_CreatesUpdateFilterCommand()
    {
        // Primary user requirement:
        // "matching the filters by id allows us to know if it has been altered, so if that particular
        // filter has been altered editing it match is sufficient to resolve the diff"
        var currentFilter = new GmailFilter(
            id: "filter_id_42",
            query: new FieldCondition("from", "bob@example.com"),
            action: new GmailAction().Star());

        var desiredFilter = new GmailFilter(
            id: "filter_id_42",
            query: new FieldCondition("from", "bob@example.com"),
            action: new GmailAction().Star().Archive());

        var filterDiff = GmailFilterDiffer.DiffSets(new[] { currentFilter }, new[] { desiredFilter });
        Assert.Equal(1, filterDiff.TotalModifications);

        var plan = GmailSyncPlanner.BuildPlan(null, filterDiff, null);

        Assert.Single(plan.Commands);
        var cmd = Assert.IsType<UpdateFilterCommand>(plan.Commands[0]);
        Assert.Equal("filter_id_42", cmd.FilterId);
        Assert.Equal(SyncResourceType.Filter, cmd.ResourceType);
        Assert.Equal(SyncActionType.Update, cmd.ActionType);
        Assert.Single(cmd.FieldDifferences);
        Assert.Equal("action", cmd.FieldDifferences[0].FieldName);
        Assert.Contains("action", cmd.DetailedDescription);
    }

    [Fact]
    public void BuildPlan_WithNewFilter_CreatesCreateFilterCommand()
    {
        var desiredFilter = new GmailFilter(
            id: "new_filter",
            query: new FieldCondition("from", "newsletter@domain.com"),
            action: new GmailAction().Archive());

        var filterDiff = GmailFilterDiffer.DiffSets(Array.Empty<GmailFilter>(), new[] { desiredFilter });
        var plan = GmailSyncPlanner.BuildPlan(null, filterDiff, null);

        Assert.Single(plan.Commands);
        var cmd = Assert.IsType<CreateFilterCommand>(plan.Commands[0]);
        Assert.Equal(SyncResourceType.Filter, cmd.ResourceType);
        Assert.Equal(SyncActionType.Create, cmd.ActionType);
        Assert.NotNull(cmd.Payload);
    }

    [Fact]
    public void BuildPlan_WithObsoleteFilter_CreatesDeleteFilterCommand()
    {
        var currentFilter = new GmailFilter(
            id: "filter_to_delete",
            query: new FieldCondition("from", "spam@spammer.com"),
            action: new GmailAction().Delete());

        var filterDiff = GmailFilterDiffer.DiffSets(new[] { currentFilter }, Array.Empty<GmailFilter>());
        var plan = GmailSyncPlanner.BuildPlan(null, filterDiff, null);

        Assert.Single(plan.Commands);
        var cmd = Assert.IsType<DeleteFilterCommand>(plan.Commands[0]);
        Assert.Equal("filter_to_delete", cmd.FilterId);
        Assert.Equal(SyncResourceType.Filter, cmd.ResourceType);
        Assert.Equal(SyncActionType.Delete, cmd.ActionType);
    }

    [Fact]
    public void BuildPlan_WithLabels_GeneratesCreateUpdateDeleteCommands()
    {
        var currentLabels = new List<GmailLabel>
        {
            new("Work", id: "Label_1", messageListVisibility: "show"),
            new("OldTag", id: "Label_99")
        };

        var desiredLabels = new List<GmailLabel>
        {
            // Work modified
            new("Work", id: "Label_1", messageListVisibility: "hide"),
            // Receipts added
            new("Receipts", messageListVisibility: "show")
            // OldTag omitted -> deleted
        };

        var labelDiff = GmailLabelDiffer.DiffSets(currentLabels, desiredLabels);
        var plan = GmailSyncPlanner.BuildPlan(labelDiff, null, null);

        Assert.Equal(3, plan.TotalCommands);
        Assert.Single(plan.Creations);
        Assert.Single(plan.Updates);
        Assert.Single(plan.Deletions);

        Assert.IsType<CreateLabelCommand>(plan.Commands[0]);
        Assert.Equal("Receipts", plan.Commands[0].TargetIdentifier);

        Assert.IsType<UpdateLabelCommand>(plan.Commands[1]);
        Assert.Equal("Work", plan.Commands[1].TargetIdentifier);
        Assert.Equal("Label_1", plan.Commands[1].TargetId);

        Assert.IsType<DeleteLabelCommand>(plan.Commands[2]);
        Assert.Equal("OldTag", plan.Commands[2].TargetIdentifier);
        Assert.Equal("Label_99", plan.Commands[2].TargetId);
    }

    [Fact]
    public void BuildPlan_WithAutoReply_GeneratesUpdateAutoReplyCommand()
    {
        var currentAr = new AutoReplyModel { EnableAutoReply = false };
        var desiredAr = new AutoReplyModel
        {
            EnableAutoReply = true,
            ResponseSubject = "Out of Office",
            ResponseBodyPlainText = "I am on vacation"
        };

        var arDiff = AutoReplyDiffer.Diff(currentAr, desiredAr);
        var plan = GmailSyncPlanner.BuildPlan(null, null, arDiff);

        Assert.Single(plan.Commands);
        var cmd = Assert.IsType<UpdateAutoReplyCommand>(plan.Commands[0]);
        Assert.Equal(SyncResourceType.AutoReply, cmd.ResourceType);
        Assert.True(cmd.IsEnabling);
        Assert.NotNull(cmd.Payload);
        Assert.Contains("Enable Auto-Reply", cmd.Description);
    }

    [Fact]
    public void BuildPlan_DependencySafeOrdering_LabelsCreatedBeforeFilters_LabelsDeletedAfterFilters()
    {
        // When synchronizing, creating a label must precede creating/updating a filter that references it.
        // Deleting a filter must precede deleting a label it might reference.
        var currentLabels = new[] { new GmailLabel("ObsoleteLabel", id: "L_old") };
        var desiredLabels = new[] { new GmailLabel("NewLabel", messageListVisibility: "show") };

        var currentFilters = new[]
        {
            new GmailFilter("F_mod", new FieldCondition("from", "a@x.com"), new GmailAction().Star()),
            new GmailFilter("F_old", new FieldCondition("from", "b@x.com"), new GmailAction().Delete())
        };
        var desiredFilters = new[]
        {
            new GmailFilter("F_mod", new FieldCondition("from", "a@x.com"), new GmailAction().Star().Archive()),
            new GmailFilter("F_new", new FieldCondition("from", "c@x.com"), new GmailAction().Archive())
        };

        var currentAr = new AutoReplyModel { EnableAutoReply = false };
        var desiredAr = new AutoReplyModel { EnableAutoReply = true, ResponseSubject = "OOO" };

        var labelDiff = GmailLabelDiffer.DiffSets(currentLabels, desiredLabels);
        var filterDiff = GmailFilterDiffer.DiffSets(currentFilters, desiredFilters);
        var arDiff = AutoReplyDiffer.Diff(currentAr, desiredAr);

        var plan = GmailSyncPlanner.BuildPlan(labelDiff, filterDiff, arDiff);

        var types = plan.Commands.Select(c => (c.ResourceType, c.ActionType)).ToList();

        // 1. Create label
        Assert.Equal((SyncResourceType.Label, SyncActionType.Create), types[0]);
        // 2. Update filter
        Assert.Equal((SyncResourceType.Filter, SyncActionType.Update), types[1]);
        // 3. Create filter
        Assert.Equal((SyncResourceType.Filter, SyncActionType.Create), types[2]);
        // 4. Delete filter
        Assert.Equal((SyncResourceType.Filter, SyncActionType.Delete), types[3]);
        // 5. Delete label (after filter deletion)
        Assert.Equal((SyncResourceType.Label, SyncActionType.Delete), types[4]);
        // 6. Auto-reply update
        Assert.Equal((SyncResourceType.AutoReply, SyncActionType.Create), types[5]);
    }

    [Fact]
    public void BuildPlan_AllowDeletionsFalse_SkipsAllDeletions()
    {
        var currentLabels = new[] { new GmailLabel("ObsoleteLabel", id: "L_old") };
        var desiredLabels = Array.Empty<GmailLabel>();

        var currentFilters = new[] { new GmailFilter("F_old", new FieldCondition("from", "b@x.com"), new GmailAction().Delete()) };
        var desiredFilters = Array.Empty<GmailFilter>();

        var currentAr = new AutoReplyModel { EnableAutoReply = true, ResponseSubject = "Active" };
        var desiredAr = new AutoReplyModel { EnableAutoReply = false };

        var labelDiff = GmailLabelDiffer.DiffSets(currentLabels, desiredLabels);
        var filterDiff = GmailFilterDiffer.DiffSets(currentFilters, desiredFilters);
        var arDiff = AutoReplyDiffer.Diff(currentAr, desiredAr);

        var plan = GmailSyncPlanner.BuildPlan(labelDiff, filterDiff, arDiff, new SyncPlanOptions
        {
            AllowDeletions = false
        });

        Assert.Empty(plan.Commands);
        Assert.Equal(0, plan.TotalDeletions);
    }

    [Fact]
    public async Task BuildPlanAsync_MakeRightMatchLeft_MutatesRightTargetToMatchLeft()
    {
        // Left = Desired specification
        var leftSource = new InMemoryGmailSource(
            labels: new[] { new GmailLabel("InboxZero", messageListVisibility: "show") },
            filters: new[] { new GmailFilter("f_1", new FieldCondition("from", "boss@co.com"), new GmailAction().Star().MarkImportant()) },
            autoReply: new AutoReplyModel { EnableAutoReply = true, ResponseSubject = "On Leave" },
            name: "Left (Desired)");

        // Right = Target account (currently has different state)
        var rightSource = new InMemoryGmailSource(
            labels: new[] { new GmailLabel("OldLabel", id: "L_99") },
            filters: new[] { new GmailFilter("f_1", new FieldCondition("from", "boss@co.com"), new GmailAction().Star()) },
            autoReply: new AutoReplyModel { EnableAutoReply = false },
            name: "Right (Target)");

        var plan = await GmailSyncPlanner.BuildPlanAsync(leftSource, rightSource, new SyncPlanOptions
        {
            Direction = SyncDirection.MakeRightMatchLeft
        });

        Assert.Equal(SyncDirection.MakeRightMatchLeft, plan.Direction);
        Assert.Equal(4, plan.TotalCommands);

        // 1. Create label 'InboxZero'
        var createLabel = Assert.IsType<CreateLabelCommand>(plan.Commands[0]);
        Assert.Equal("InboxZero", createLabel.LabelName);

        // 2. Update filter 'f_1' (matched by ID, action altered!)
        var updateFilter = Assert.IsType<UpdateFilterCommand>(plan.Commands[1]);
        Assert.Equal("f_1", updateFilter.FilterId);

        // 3. Delete label 'OldLabel'
        var deleteLabel = Assert.IsType<DeleteLabelCommand>(plan.Commands[2]);
        Assert.Equal("OldLabel", deleteLabel.LabelName);

        // 4. Update auto-reply (enable)
        var updateAr = Assert.IsType<UpdateAutoReplyCommand>(plan.Commands[3]);
        Assert.True(updateAr.IsEnabling);
    }

    [Fact]
    public async Task BuildPlanAsync_MakeLeftMatchRight_MutatesLeftTargetToMatchRight()
    {
        var leftSource = new InMemoryGmailSource(
            labels: new[] { new GmailLabel("LeftOld", id: "L_left") },
            name: "Left");

        var rightSource = new InMemoryGmailSource(
            labels: new[] { new GmailLabel("RightDesired", id: "L_right") },
            name: "Right");

        var plan = await GmailSyncPlanner.BuildPlanAsync(leftSource, rightSource, new SyncPlanOptions
        {
            Direction = SyncDirection.MakeLeftMatchRight
        });

        Assert.Equal(SyncDirection.MakeLeftMatchRight, plan.Direction);
        Assert.Equal(2, plan.TotalCommands);

        // Create 'RightDesired' and Delete 'LeftOld'
        Assert.Contains(plan.Commands, c => c.ActionType == SyncActionType.Create && c.TargetIdentifier == "RightDesired");
        Assert.Contains(plan.Commands, c => c.ActionType == SyncActionType.Delete && c.TargetIdentifier == "LeftOld");
    }

    [Fact]
    public async Task BuildPlanFromScriptAsync_GeneratesPlanFromLuaScript()
    {
        var fakeClient = new FakeGmailApiClient();
        fakeClient.AddLabel(new GmailLabel("OldTag", id: "L_old"));
        fakeClient.AddFilter(new GmailFilter("f_1", new FieldCondition("from", "client@corp.com"), new GmailAction().Star()));

        string lua = """
        return {
            labels = {
                label { name = "Receipts", message_list_visibility = "show" }
            },
            rules = {
                filter {
                    id = "f_1",
                    query = From("client@corp.com"),
                    action = actions(star, archive)
                }
            }
        }
        """;

        var plan = await GmailSyncPlanner.BuildPlanFromScriptAsync(fakeClient, lua);

        Assert.Equal(3, plan.TotalCommands);

        // Create Receipts, Update f_1, Delete OldTag
        Assert.Contains(plan.Commands, c => c is CreateLabelCommand && c.TargetIdentifier == "Receipts");
        Assert.Contains(plan.Commands, c => c is UpdateFilterCommand && c.TargetId == "f_1");
        Assert.Contains(plan.Commands, c => c is DeleteLabelCommand && c.TargetIdentifier == "OldTag");
    }

    [Fact]
    public void ToDryRunReport_FormatsCleanReadableReport()
    {
        var label = new GmailLabel("Receipts", messageListVisibility: "show");
        var cmd1 = new CreateLabelCommand(label);

        var currentFilter = new GmailFilter("f_1", new FieldCondition("from", "a@b.com"), new GmailAction().Star());
        var desiredFilter = new GmailFilter("f_1", new FieldCondition("from", "a@b.com"), new GmailAction().Star().Archive());
        var cmd2 = new UpdateFilterCommand("f_1", currentFilter, desiredFilter, new[]
        {
            new FilterFieldDiff("action", "star", "archive, star")
        });

        var plan = new SyncPlan(new ISyncCommand[] { cmd1, cmd2 });
        string report = plan.ToDryRunReport();

        Assert.Contains("VitaCernita Synchronization Plan (Dry Run)", report);
        Assert.Contains("Planned Execution Steps (Dependency-Safe Order):", report);
        Assert.Contains("1. [+] Create Label 'Receipts'", report);
        Assert.Contains("2. [~] Update Filter (ID: f_1): 1 change(s)", report);
        Assert.Contains("action: 'star' -> 'archive, star'", report);
    }
}
