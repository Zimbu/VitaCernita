using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VitaCernita.Cli.Commands;
using VitaCernita.Core.Actions;
using VitaCernita.Core.Api.Fakes;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Filters.Diff;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Queries;
using VitaCernita.Core.Serialization;
using VitaCernita.Core.Sources;
using VitaCernita.Core.Sync;
using VitaCernita.Core.Sync.Commands;
using Xunit;

namespace VitaCernita.Tests;

public class ActionLabelResolutionTests
{
    private readonly LuaConfigSerializer _serializer = LuaConfigSerializer.Default;
    private readonly GmailFilterLoader _loader = new();

    private readonly List<GmailLabel> _knownLabels = new()
    {
        new GmailLabel("Receipts", id: "Label_101", messageListVisibility: "show", labelListVisibility: "labelShow"),
        new GmailLabel("Work", id: "Label_102", messageListVisibility: "show", labelListVisibility: "labelShow"),
        new GmailLabel("Projects/Vita", id: "Label_103", messageListVisibility: "show", labelListVisibility: "labelShow")
    };

    // =========================================================================
    // 1. Concise DSL Command Preferences for System Labels
    // =========================================================================

    [Fact]
    public void FormatAction_PrefersConciseCommands_ForSystemLabels()
    {
        var action = new GmailAction();
        action.RemoveLabelIds.Add(SystemLabels.Inbox);
        action.RemoveLabelIds.Add(SystemLabels.Unread);
        action.AddLabelIds.Add(SystemLabels.Starred);
        action.AddLabelIds.Add(SystemLabels.Trash);
        action.AddLabelIds.Add(SystemLabels.Important);
        action.AddLabelIds.Add(SystemLabels.CategoryPromotions);

        string lua = _serializer.SerializeAction(action);

        Assert.Contains("archive", lua);
        Assert.Contains("mark_read", lua);
        Assert.Contains("star", lua);
        Assert.Contains("delete", lua);
        Assert.Contains("mark_important", lua);
        Assert.Contains("add_category(\"Promotions\")", lua);

        // Never emit remove_label or add_label for system labels
        Assert.DoesNotContain("remove_label(\"INBOX\")", lua, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("remove_label(\"UNREAD\")", lua, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("add_label(\"STARRED\")", lua, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("add_label(\"TRASH\")", lua, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("add_label(\"IMPORTANT\")", lua, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("add_label(\"CATEGORY_PROMOTIONS\")", lua, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // 2. Label ID -> Name Resolution in Serializer
    // =========================================================================

    [Fact]
    public void FormatAction_ResolvesCustomLabelIdsToNames_WhenKnownLabelsProvided()
    {
        var action = new GmailAction();
        action.Archive();
        action.AddLabelIds.Add("Label_101");
        action.RemoveLabelIds.Add("Label_102");

        var options = new LuaSerializerOptions { KnownLabels = _knownLabels };
        string lua = _serializer.SerializeAction(action, options);

        Assert.Contains("archive", lua);
        Assert.Contains("add_label(\"Receipts\")", lua);
        Assert.Contains("remove_label(\"Work\")", lua);
        Assert.DoesNotContain("Label_101", lua);
        Assert.DoesNotContain("Label_102", lua);
    }

    [Fact]
    public void FormatAction_FallsBackToId_WhenLabelNotKnown()
    {
        var action = new GmailAction();
        action.AddLabelIds.Add("Unknown_Label_999");
        action.RemoveLabelIds.Add("Unknown_Label_888");

        var options = new LuaSerializerOptions { KnownLabels = _knownLabels };
        string lua = _serializer.SerializeAction(action, options);

        Assert.Contains("add_label(\"Unknown_Label_999\")", lua);
        Assert.Contains("remove_label(\"Unknown_Label_888\")", lua);
    }

    // =========================================================================
    // 3. GmailAction and GmailFilter Label Resolution Helpers
    // =========================================================================

    [Fact]
    public void GmailAction_WithResolvedLabels_ToId_MapsNamesToIds()
    {
        var action = new GmailAction()
            .Archive()
            .AddCustomLabel("Receipts")
            .RemoveCustomLabel("Work");

        var resolved = action.WithResolvedLabels(_knownLabels, toId: true);

        Assert.True(resolved.IsArchive);
        Assert.Contains("Label_101", resolved.AddLabelIds);
        Assert.Contains("Label_102", resolved.RemoveLabelIds);
        Assert.DoesNotContain("Receipts", resolved.AddLabelIds);
        Assert.DoesNotContain("Work", resolved.RemoveLabelIds);
    }

    [Fact]
    public void GmailAction_WithResolvedLabels_ToName_MapsIdsToNames()
    {
        var action = new GmailAction();
        action.Archive();
        action.AddLabelIds.Add("Label_101");
        action.RemoveLabelIds.Add("Label_102");

        var resolved = action.WithResolvedLabels(_knownLabels, toId: false);

        Assert.True(resolved.IsArchive);
        Assert.Contains("Receipts", resolved.AddLabelIds);
        Assert.Contains("Work", resolved.RemoveLabelIds);
        Assert.DoesNotContain("Label_101", resolved.AddLabelIds);
        Assert.DoesNotContain("Label_102", resolved.RemoveLabelIds);
    }

    [Fact]
    public void GmailFilter_ToDictionary_ResolvesNamesToIds_ForApiPayload()
    {
        var filter = new GmailFilter(
            id: "f_01",
            query: new FieldCondition("from", "orders@store.com"),
            action: new GmailAction().Archive().AddCustomLabel("Receipts").RemoveCustomLabel("Work"),
            name: "Order Processing");

        var dict = filter.ToDictionary(_knownLabels);

        Assert.True(dict.ContainsKey("action"));
        var actDict = Assert.IsAssignableFrom<Dictionary<string, object>>(dict["action"]);

        var addIds = Assert.IsAssignableFrom<List<string>>(actDict["addLabelIds"]);
        var remIds = Assert.IsAssignableFrom<List<string>>(actDict["removeLabelIds"]);

        Assert.Contains("Label_101", addIds);
        Assert.Contains(SystemLabels.Inbox, remIds);
        Assert.Contains("Label_102", remIds);
    }

    // =========================================================================
    // 4. Diffing: Compares Equal Across Label IDs (API) and Names (Config)
    // =========================================================================

    [Fact]
    public void GmailFilterDiffer_Diff_MatchesEqual_WhenApiHasIdsAndConfigHasNames()
    {
        // Current filter from Gmail API: has Label_101 and INBOX
        var currentAction = new GmailAction();
        currentAction.RemoveLabelIds.Add(SystemLabels.Inbox);
        currentAction.AddLabelIds.Add("Label_101");
        var current = new GmailFilter("f_1", new FieldCondition("from", "billing@vendor.com"), currentAction);

        // Desired filter from Lua configuration: has archive and Receipts name
        var desiredAction = new GmailAction().Archive().AddCustomLabel("Receipts");
        var desired = new GmailFilter("f_1", new FieldCondition("from", "billing@vendor.com"), desiredAction);

        var options = new FilterDiffOptions
        {
            KnownLabels = _knownLabels
        };

        var diff = GmailFilterDiffer.Diff(current, desired, options);

        Assert.False(diff.HasChanges);
        Assert.Equal(FilterDiffType.Unchanged, diff.DiffType);
    }

    [Fact]
    public void GmailFilterDiffer_Diff_ShowsHumanReadableNames_WhenActionsDiffer()
    {
        var currentAction = new GmailAction();
        currentAction.AddLabelIds.Add("Label_101"); // Receipts
        var current = new GmailFilter("f_1", new FieldCondition("from", "a@b.com"), currentAction);

        var desiredAction = new GmailAction().AddCustomLabel("Work"); // Work (Label_102)
        var desired = new GmailFilter("f_1", new FieldCondition("from", "a@b.com"), desiredAction);

        var options = new FilterDiffOptions
        {
            KnownLabels = _knownLabels
        };

        var diff = GmailFilterDiffer.Diff(current, desired, options);

        Assert.True(diff.HasChanges);
        Assert.Equal(FilterDiffType.Modified, diff.DiffType);

        var actionDiff = diff.FieldDifferences.FirstOrDefault(d => d.FieldName == "action");
        Assert.NotNull(actionDiff);
        Assert.Contains("label:Receipts", actionDiff.CurrentValue?.ToString());
        Assert.Contains("label:Work", actionDiff.DesiredValue?.ToString());
    }

    [Fact]
    public async Task GmailSourceDiffer_DiffFiltersAsync_ResolvesLabelsAcrossSources()
    {
        // Source A (e.g. Gmail API) returns label with ID
        var sourceA = new InMemoryGmailSource(
            labels: _knownLabels,
            filters: new[]
            {
                new GmailFilter(
                    id: "api_f1",
                    query: new FieldCondition("from", "boss@corp.com"),
                    action: new GmailAction().Archive().AddCustomLabel("Label_102")) // Label_102 is Work
            });

        // Source B (e.g. Lua config) returns filter using label Name
        var sourceB = new InMemoryGmailSource(
            labels: _knownLabels,
            filters: new[]
            {
                new GmailFilter(
                    id: "api_f1",
                    query: new FieldCondition("from", "boss@corp.com"),
                    action: new GmailAction().Archive().AddCustomLabel("Work"))
            });

        var diffSet = await GmailSourceDiffer.DiffFiltersAsync(sourceA, sourceB);

        Assert.False(diffSet.HasDifferences);
    }

    // =========================================================================
    // 5. Sync Planner: Commands Use Resolved Label IDs in Payloads
    // =========================================================================

    [Fact]
    public void GmailSyncPlanner_CreateFilterCommand_ResolvesLabelNamesToIds()
    {
        var desiredFilter = new GmailFilter(
            id: null,
            query: new FieldCondition("from", "client@corp.com"),
            action: new GmailAction().Archive().AddCustomLabel("Receipts").RemoveCustomLabel("Work"),
            name: "Client Invoices");

        var filterDiff = new FilterSetDiff(new[]
        {
            new FilterDiff(null, "Client Invoices", FilterDiffType.Added, null, desiredFilter)
        });

        var plan = GmailSyncPlanner.BuildPlan(
            labelDiff: null,
            filterDiff: filterDiff,
            autoReplyDiff: null,
            options: new SyncPlanOptions { KnownLabels = _knownLabels });

        var createCmd = Assert.Single(plan.Commands.OfType<CreateFilterCommand>());
        Assert.NotNull(createCmd.Payload);

        var payloadDict = Assert.IsAssignableFrom<Dictionary<string, object>>(createCmd.Payload);
        var actDict = Assert.IsAssignableFrom<Dictionary<string, object>>(payloadDict["action"]);

        var addIds = Assert.IsAssignableFrom<List<string>>(actDict["addLabelIds"]);
        var remIds = Assert.IsAssignableFrom<List<string>>(actDict["removeLabelIds"]);

        Assert.Contains("Label_101", addIds);
        Assert.Contains("Label_102", remIds);
        Assert.Contains(SystemLabels.Inbox, remIds);
    }

    // =========================================================================
    // 6. InitializeCommand: Full Integration Test
    // =========================================================================

    [Fact]
    public async Task Initialize_FromFakeAccount_EmitsConciseDslAndLabelNames_AndRoundTrips()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"vitacernita_init_labels_{Guid.NewGuid():N}.lua");
        try
        {
            var fakeClient = new FakeGmailApiClient();
            fakeClient.AddLabel(new GmailLabel("Receipts", id: "L_REC"));
            fakeClient.AddLabel(new GmailLabel("Work", id: "L_WRK"));

            // Filter in API has raw label IDs and INBOX/UNREAD removals
            var apiAction = new GmailAction();
            apiAction.RemoveLabelIds.Add(SystemLabels.Inbox);
            apiAction.RemoveLabelIds.Add(SystemLabels.Unread);
            apiAction.RemoveLabelIds.Add("L_WRK");
            apiAction.AddLabelIds.Add(SystemLabels.Starred);
            apiAction.AddLabelIds.Add("L_REC");

            fakeClient.AddFilter(new GmailFilter(
                id: "filter_init_01",
                query: new FieldCondition("from", "billing@vendor.com"),
                action: apiAction,
                name: "Vendor Filter"));

            var cmd = new InitializeCommand(apiClientOverride: fakeClient);
            int exitCode = await cmd.ExecuteAsync(new[]
            {
                "--output", tempPath,
                "--account", "testuser@example.com"
            });

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(tempPath));

            string luaContent = await File.ReadAllTextAsync(tempPath);

            // Assert concise commands preferred
            Assert.Contains("archive", luaContent);
            Assert.Contains("mark_read", luaContent);
            Assert.Contains("star", luaContent);

            // Assert label IDs resolved to human-readable names
            Assert.Contains("add_label(\"Receipts\")", luaContent);
            Assert.Contains("remove_label(\"Work\")", luaContent);

            // Assert raw IDs and system labels never appear in add_label/remove_label
            Assert.DoesNotContain("add_label(\"L_REC\")", luaContent);
            Assert.DoesNotContain("remove_label(\"L_WRK\")", luaContent);
            Assert.DoesNotContain("remove_label(\"INBOX\")", luaContent, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("remove_label(\"UNREAD\")", luaContent, StringComparison.OrdinalIgnoreCase);

            // Assert the file reloads back successfully
            var config = await _loader.LoadConfigurationFromFileAsync(tempPath);
            Assert.Equal(2, config.Labels.Count);
            var loadedFilter = Assert.Single(config.Filters);

            Assert.NotNull(loadedFilter.Action);
            Assert.True(loadedFilter.Action.IsArchive);
            Assert.True(loadedFilter.Action.IsMarkUnread);
            Assert.True(loadedFilter.Action.IsStarred);
            Assert.Contains("Receipts", loadedFilter.Action.CustomLabels);
            Assert.Contains("Work", loadedFilter.Action.CustomRemoveLabels);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }
}
