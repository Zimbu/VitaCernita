using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Labels.Diff;

namespace VitaCernita.Tests;

public class GmailLabelDiffTests
{
    private static GmailLabel MakeLabel(
        string name,
        string? id = null,
        string? messageListVisibility = null,
        string? labelListVisibility = null,
        string? textColor = null,
        string? bgColor = null)
    {
        LabelColor? color = (textColor != null && bgColor != null)
            ? new LabelColor(textColor, bgColor)
            : null;

        return new GmailLabel(name, id, messageListVisibility, labelListVisibility, color);
    }

    [Fact]
    public void Diff_BothNull_ReturnsUnchanged()
    {
        GmailLabel? current = null;
        GmailLabel? desired = null;
        var diff = GmailLabelDiffer.Diff(current, desired);
        Assert.Equal(LabelDiffType.Unchanged, diff.DiffType);
        Assert.False(diff.HasChanges);
        Assert.Empty(diff.FieldDifferences);
    }

    [Fact]
    public void Diff_CurrentNull_ReturnsAddedWithCreatePayload()
    {
        var desired = MakeLabel("Receipts", messageListVisibility: "show", textColor: "#ffffff", bgColor: "#000000");
        var diff = GmailLabelDiffer.Diff(null, desired);

        Assert.Equal(LabelDiffType.Added, diff.DiffType);
        Assert.True(diff.HasChanges);
        Assert.Equal("Receipts", diff.Name);
        Assert.Null(diff.CurrentLabel);
        Assert.Same(desired, diff.DesiredLabel);

        var payload = diff.GetCreatePayload();
        Assert.NotNull(payload);
        Assert.Equal("Receipts", payload["name"]);
        Assert.Equal("show", payload["messageListVisibility"]);
        Assert.True(payload.ContainsKey("color"));
    }

    [Fact]
    public void Diff_DesiredNull_ReturnsRemovedWithDeleteId()
    {
        var current = MakeLabel("OldLabel", id: "Label_123");
        var diff = GmailLabelDiffer.Diff(current, null);

        Assert.Equal(LabelDiffType.Removed, diff.DiffType);
        Assert.True(diff.HasChanges);
        Assert.Equal("OldLabel", diff.Name);
        Assert.Equal("Label_123", diff.GetDeleteId());
        Assert.Same(current, diff.CurrentLabel);
        Assert.Null(diff.DesiredLabel);
    }

    [Fact]
    public void Diff_IdenticalLabels_ReturnsUnchanged()
    {
        var current = MakeLabel("Work", id: "Label_1", "show", "labelShow", "#ffffff", "#000000");
        var desired = MakeLabel("Work", id: "Label_1", "show", "labelShow", "#ffffff", "#000000");

        var diff = GmailLabelDiffer.Diff(current, desired);

        Assert.Equal(LabelDiffType.Unchanged, diff.DiffType);
        Assert.False(diff.HasChanges);
        Assert.Empty(diff.FieldDifferences);
        Assert.Empty(diff.GetPatchPayload());
    }

    [Fact]
    public void Diff_ModifiedVisibility_ReturnsModifiedWithPatchPayload()
    {
        var current = MakeLabel("Projects", id: "Label_2", messageListVisibility: "show", labelListVisibility: "labelShow");
        var desired = MakeLabel("Projects", id: "Label_2", messageListVisibility: "hide", labelListVisibility: "labelHide");

        var diff = GmailLabelDiffer.Diff(current, desired);

        Assert.Equal(LabelDiffType.Modified, diff.DiffType);
        Assert.True(diff.HasChanges);
        Assert.Equal(2, diff.FieldDifferences.Count);

        var patch = diff.GetPatchPayload();
        Assert.Equal(2, patch.Count);
        Assert.Equal("hide", patch["messageListVisibility"]);
        Assert.Equal("labelHide", patch["labelListVisibility"]);
        Assert.False(patch.ContainsKey("name"));
        Assert.False(patch.ContainsKey("color"));
    }

    [Fact]
    public void Diff_ModifiedColor_GeneratesColorPatch()
    {
        var current = MakeLabel("Urgent", id: "Label_3", textColor: "#ffffff", bgColor: "#000000");
        var desired = MakeLabel("Urgent", id: "Label_3", textColor: "#000000", bgColor: "#ffffff");

        var diff = GmailLabelDiffer.Diff(current, desired);

        Assert.Equal(LabelDiffType.Modified, diff.DiffType);
        Assert.Single(diff.FieldDifferences);
        Assert.Equal("color", diff.FieldDifferences[0].FieldName);

        var patch = diff.GetPatchPayload();
        Assert.Single(patch);
        Assert.True(patch.ContainsKey("color"));
        var colorDict = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(patch["color"]);
        Assert.Equal("#000000", colorDict["textColor"]);
        Assert.Equal("#ffffff", colorDict["backgroundColor"]);
    }

    [Fact]
    public void Diff_ModifiedName_WhenMatchedById()
    {
        var current = MakeLabel("OldName", id: "Label_4");
        var desired = MakeLabel("NewName", id: "Label_4");

        var diff = GmailLabelDiffer.Diff(current, desired);

        Assert.Equal(LabelDiffType.Modified, diff.DiffType);
        Assert.Single(diff.FieldDifferences);
        Assert.Equal("name", diff.FieldDifferences[0].FieldName);
        Assert.Equal("OldName", diff.FieldDifferences[0].CurrentValue);
        Assert.Equal("NewName", diff.FieldDifferences[0].DesiredValue);

        var patch = diff.GetPatchPayload();
        Assert.Equal("NewName", patch["name"]);
    }

    [Fact]
    public void Diff_IgnoreUnsetDesiredFields_IgnoresNullPropertiesInDesired()
    {
        var current = MakeLabel("Personal", id: "Label_5", messageListVisibility: "show", labelListVisibility: "labelShow", textColor: "#ffffff", bgColor: "#000000");
        // Desired only specifies name, leaves visibility and color null
        var desired = MakeLabel("Personal");

        var options = new LabelDiffOptions { IgnoreUnsetDesiredFields = true };
        var diff = GmailLabelDiffer.Diff(current, desired, options);

        Assert.Equal(LabelDiffType.Unchanged, diff.DiffType);
        Assert.False(diff.HasChanges);
        Assert.Empty(diff.FieldDifferences);
    }

    [Fact]
    public void Diff_WithoutIgnoreUnsetDesiredFields_DetectsUnsetAsChanges()
    {
        var current = MakeLabel("Personal", id: "Label_5", messageListVisibility: "show", textColor: "#ffffff", bgColor: "#000000");
        var desired = MakeLabel("Personal");

        var options = new LabelDiffOptions { IgnoreUnsetDesiredFields = false };
        var diff = GmailLabelDiffer.Diff(current, desired, options);

        Assert.Equal(LabelDiffType.Modified, diff.DiffType);
        Assert.Equal(2, diff.FieldDifferences.Count);
    }

    [Fact]
    public void Diff_FieldsToCompare_OnlyComparesSpecifiedFields()
    {
        var current = MakeLabel("Finance", id: "Label_6", messageListVisibility: "show", textColor: "#ffffff", bgColor: "#000000");
        var desired = MakeLabel("Finance", id: "Label_6", messageListVisibility: "hide", textColor: "#000000", bgColor: "#ffffff");

        // Only compare visibility, ignoring color differences
        var options = new LabelDiffOptions
        {
            FieldsToCompare = new HashSet<string> { "messageListVisibility" }
        };
        var diff = GmailLabelDiffer.Diff(current, desired, options);

        Assert.Equal(LabelDiffType.Modified, diff.DiffType);
        Assert.Single(diff.FieldDifferences);
        Assert.Equal("messageListVisibility", diff.FieldDifferences[0].FieldName);

        var patch = diff.GetPatchPayload();
        Assert.Single(patch);
        Assert.Equal("hide", patch["messageListVisibility"]);
    }

    [Fact]
    public void Diff_FieldsToIgnore_IgnoresSpecifiedFields()
    {
        var current = MakeLabel("Finance", id: "Label_6", messageListVisibility: "show", textColor: "#ffffff", bgColor: "#000000");
        var desired = MakeLabel("Finance", id: "Label_6", messageListVisibility: "hide", textColor: "#000000", bgColor: "#ffffff");

        // Ignore color
        var options = new LabelDiffOptions
        {
            FieldsToIgnore = new HashSet<string> { "color" }
        };
        var diff = GmailLabelDiffer.Diff(current, desired, options);

        Assert.Equal(LabelDiffType.Modified, diff.DiffType);
        Assert.Single(diff.FieldDifferences);
        Assert.Equal("messageListVisibility", diff.FieldDifferences[0].FieldName);
    }

    [Fact]
    public void Diff_GranularColorFiltering_TextColorOnly()
    {
        var current = MakeLabel("Test", textColor: "#ffffff", bgColor: "#000000");
        // Only bgColor changed
        var desired = MakeLabel("Test", textColor: "#ffffff", bgColor: "#434343");

        var options = new LabelDiffOptions
        {
            FieldsToCompare = new HashSet<string> { "textColor" }
        };
        var diff = GmailLabelDiffer.Diff(current, desired, options);

        // Since only bgColor changed and we're only comparing textColor, should be unchanged
        Assert.Equal(LabelDiffType.Unchanged, diff.DiffType);
    }

    [Fact]
    public void DiffSets_MatchByName_IdentifiesCreationsDeletionsModificationsAndUnchanged()
    {
        var current = new List<GmailLabel>
        {
            MakeLabel("Inbox_Custom", id: "L1", messageListVisibility: "show"),
            MakeLabel("Deprecated", id: "L2"),
            MakeLabel("Stable", id: "L3", messageListVisibility: "show")
        };

        var desired = new List<GmailLabel>
        {
            MakeLabel("Inbox_Custom", messageListVisibility: "hide"), // Modified
            MakeLabel("Stable", messageListVisibility: "show"),       // Unchanged
            MakeLabel("BrandNew", messageListVisibility: "show")       // Added
        };

        var diffSet = GmailLabelDiffer.DiffSets(current, desired);

        Assert.True(diffSet.HasDifferences);
        Assert.Equal(1, diffSet.TotalCreations);
        Assert.Equal(1, diffSet.TotalDeletions);
        Assert.Equal(1, diffSet.TotalModifications);
        Assert.Equal(1, diffSet.TotalUnchanged);

        Assert.Equal("BrandNew", diffSet.Creations[0].Name);
        Assert.Equal("Deprecated", diffSet.Deletions[0].Name);
        Assert.Equal("L2", diffSet.Deletions[0].GetDeleteId());
        Assert.Equal("Inbox_Custom", diffSet.Modifications[0].Name);
        Assert.Equal("L1", diffSet.Modifications[0].Id);
        Assert.Equal("Stable", diffSet.Unchanged[0].Name);
    }

    [Fact]
    public void DiffSets_CaseInsensitiveNameMatching_MatchesRegardlessOfCase()
    {
        var current = new List<GmailLabel>
        {
            MakeLabel("receipts", id: "L10", messageListVisibility: "show")
        };

        var desired = new List<GmailLabel>
        {
            MakeLabel("Receipts", messageListVisibility: "show")
        };

        var diffSet = GmailLabelDiffer.DiffSets(current, desired, new LabelDiffOptions
        {
            CaseInsensitiveNameMatch = true
        });

        Assert.False(diffSet.HasDifferences);
        Assert.Equal(1, diffSet.TotalUnchanged);
    }

    [Fact]
    public void DiffSets_MatchById_PairsByServerId()
    {
        var current = new List<GmailLabel>
        {
            MakeLabel("OldName", id: "Label_100", messageListVisibility: "show")
        };

        var desired = new List<GmailLabel>
        {
            MakeLabel("RenamedLabel", id: "Label_100", messageListVisibility: "show")
        };

        var diffSet = GmailLabelDiffer.DiffSets(current, desired, new LabelDiffOptions
        {
            MatchBy = LabelMatchKey.Id
        });

        Assert.True(diffSet.HasDifferences);
        Assert.Equal(1, diffSet.TotalModifications);
        Assert.Equal(0, diffSet.TotalCreations);
        Assert.Equal(0, diffSet.TotalDeletions);
        Assert.Equal("RenamedLabel", diffSet.Modifications[0].Name);
        Assert.Equal("Label_100", diffSet.Modifications[0].Id);
        Assert.Equal("name", diffSet.Modifications[0].FieldDifferences[0].FieldName);
    }

    [Fact]
    public void DiffSets_MatchByIdThenName_FallsBackToNameWhenIdOmitted()
    {
        var current = new List<GmailLabel>
        {
            MakeLabel("Alpha", id: "L_ALPHA", messageListVisibility: "show"),
            MakeLabel("Beta", id: "L_BETA", messageListVisibility: "show")
        };

        var desired = new List<GmailLabel>
        {
            MakeLabel("AlphaRenamed", id: "L_ALPHA", messageListVisibility: "show"), // Matched by ID
            MakeLabel("Beta", messageListVisibility: "hide")                         // Matched by Name fallback
        };

        var diffSet = GmailLabelDiffer.DiffSets(current, desired, new LabelDiffOptions
        {
            MatchBy = LabelMatchKey.IdThenName
        });

        Assert.Equal(2, diffSet.TotalModifications);
        Assert.Equal(0, diffSet.TotalCreations);
        Assert.Equal(0, diffSet.TotalDeletions);

        var alphaMod = Assert.Single(diffSet.Modifications, m => m.Id == "L_ALPHA");
        Assert.Equal("AlphaRenamed", alphaMod.Name);

        var betaMod = Assert.Single(diffSet.Modifications, m => m.Name == "Beta");
        Assert.Equal("L_BETA", betaMod.Id);
    }

    [Fact]
    public void DiffSets_IncludeUnchangedFalse_ExcludesUnchangedFromDifferences()
    {
        var current = new List<GmailLabel> { MakeLabel("Same", id: "L1") };
        var desired = new List<GmailLabel> { MakeLabel("Same") };

        var diffSet = GmailLabelDiffer.DiffSets(current, desired, new LabelDiffOptions
        {
            IncludeUnchanged = false
        });

        Assert.Empty(diffSet.Differences);
        Assert.Equal(0, diffSet.TotalUnchanged);
    }

    [Fact]
    public void Diff_DictionaryOverload_ComparesCorrectly()
    {
        var currentDict = new Dictionary<string, object?>
        {
            ["id"] = "L99",
            ["name"] = "Receipts",
            ["messageListVisibility"] = "show"
        };

        var desiredDict = new Dictionary<string, object?>
        {
            ["name"] = "Receipts",
            ["messageListVisibility"] = "hide"
        };

        var diff = GmailLabelDiffer.Diff(currentDict, desiredDict);

        Assert.Equal(LabelDiffType.Modified, diff.DiffType);
        Assert.Equal("L99", diff.Id);
        Assert.Equal("hide", diff.GetPatchPayload()["messageListVisibility"]);
    }

    [Fact]
    public void Diff_JsonSingleLabelOverload_ComparesCorrectly()
    {
        string currentJson = @"{ ""id"": ""L55"", ""name"": ""News"", ""messageListVisibility"": ""show"" }";
        string desiredJson = @"{ ""name"": ""News"", ""messageListVisibility"": ""hide"" }";

        var diff = GmailLabelDiffer.DiffJson(currentJson, desiredJson);

        Assert.Equal(LabelDiffType.Modified, diff.DiffType);
        Assert.Equal("L55", diff.Id);
        Assert.Equal("hide", diff.GetPatchPayload()["messageListVisibility"]);
    }

    [Fact]
    public void DiffApiListResponse_FiltersOutSystemLabelsByDefault()
    {
        string apiJson = @"{
            ""labels"": [
                { ""id"": ""INBOX"", ""name"": ""INBOX"", ""type"": ""system"" },
                { ""id"": ""SENT"", ""name"": ""SENT"", ""type"": ""system"" },
                { ""id"": ""TRASH"", ""name"": ""TRASH"", ""type"": ""system"" },
                { ""id"": ""Label_1"", ""name"": ""Receipts"", ""type"": ""user"", ""messageListVisibility"": ""show"" },
                { ""id"": ""Label_2"", ""name"": ""OldProject"", ""type"": ""user"" }
            ]
        }";

        var desired = new List<GmailLabel>
        {
            MakeLabel("Receipts", messageListVisibility: "show"),
            MakeLabel("NewProject")
        };

        var diffSet = GmailLabelDiffer.DiffApiListResponse(apiJson, desired);

        // Receipts should be unchanged, NewProject created, OldProject deleted.
        // System labels (INBOX, SENT, TRASH) must NOT be marked for deletion!
        Assert.Equal(1, diffSet.TotalCreations);
        Assert.Equal("NewProject", diffSet.Creations[0].Name);

        Assert.Equal(1, diffSet.TotalDeletions);
        Assert.Equal("OldProject", diffSet.Deletions[0].Name);
        Assert.Equal("Label_2", diffSet.Deletions[0].GetDeleteId());

        Assert.Equal(1, diffSet.TotalUnchanged);
        Assert.Equal("Receipts", diffSet.Unchanged[0].Name);
    }

    [Fact]
    public async Task DiffApiListResponse_IntegratedWithLuaLoader()
    {
        var loader = new GmailLabelLoader();
        string luaScript = @"
            return {
                label {
                    name = 'Receipts',
                    message_list_visibility = 'show',
                    label_list_visibility = 'labelShow',
                    color = { text = 'white', background = 'black' }
                },
                label {
                    name = 'Archive/Tax2024',
                    message_list_visibility = 'hide',
                    label_list_visibility = 'show_if_unread'
                }
            }
        ";

        var desiredLabels = await loader.LoadLabelsFromScriptAsync(luaScript);

        string apiJson = @"{
            ""labels"": [
                { ""id"": ""INBOX"", ""name"": ""INBOX"", ""type"": ""system"" },
                { ""id"": ""L_REC"", ""name"": ""Receipts"", ""type"": ""user"", ""messageListVisibility"": ""hide"", ""labelListVisibility"": ""labelHide"" }
            ]
        }";

        var diffSet = GmailLabelDiffer.DiffApiListResponse(apiJson, desiredLabels);

        Assert.Equal(1, diffSet.TotalCreations);
        Assert.Equal("Archive/Tax2024", diffSet.Creations[0].Name);

        Assert.Equal(1, diffSet.TotalModifications);
        Assert.Equal("Receipts", diffSet.Modifications[0].Name);
        Assert.Equal("L_REC", diffSet.Modifications[0].Id);

        var patch = diffSet.Modifications[0].GetPatchPayload();
        Assert.Equal("show", patch["messageListVisibility"]);
        Assert.Equal("labelShow", patch["labelListVisibility"]);
        Assert.True(patch.ContainsKey("color"));
    }

    [Fact]
    public void DryRunReport_FormatsSummaryAndDetailsProperly()
    {
        var current = new List<GmailLabel>
        {
            MakeLabel("Updates", id: "L_UPD", messageListVisibility: "show"),
            MakeLabel("DeprecatedTag", id: "L_DEP")
        };

        var desired = new List<GmailLabel>
        {
            MakeLabel("Updates", messageListVisibility: "hide"),
            MakeLabel("FreshTag", messageListVisibility: "show", textColor: "#ffffff", bgColor: "#000000")
        };

        var diffSet = GmailLabelDiffer.DiffSets(current, desired);

        string summary = diffSet.ToSummaryString();
        Assert.Equal("Summary: 1 to create, 1 to update, 1 to delete, 0 unchanged.", summary);

        string report = diffSet.ToDryRunReport();
        Assert.Contains("VitaCernita Label Diff Report (Dry Run)", report);
        Assert.Contains("[+] Create (1):", report);
        Assert.Contains("+ 'FreshTag'", report);
        Assert.Contains("[~] Update (1):", report);
        Assert.Contains("~ 'Updates' (ID: L_UPD):", report);
        Assert.Contains("messageListVisibility: show -> hide", report);
        Assert.Contains("[-] Delete (1):", report);
        Assert.Contains("- 'DeprecatedTag' (ID: L_DEP)", report);
    }

    [Fact]
    public void DiffSets_NullCollections_ReturnsEmptyDiff()
    {
        var diffSet = GmailLabelDiffer.DiffSets((IEnumerable<GmailLabel>?)null, (IEnumerable<GmailLabel>?)null);
        Assert.False(diffSet.HasDifferences);
        Assert.Empty(diffSet.Differences);
        Assert.Equal(0, diffSet.TotalCreations);
        Assert.Equal(0, diffSet.TotalDeletions);
        Assert.Equal(0, diffSet.TotalModifications);
        Assert.Equal(0, diffSet.TotalUnchanged);
    }

    [Fact]
    public void DiffSets_DictionaryCollections_WorksProperly()
    {
        var currentDicts = new List<Dictionary<string, object?>>
        {
            new() { ["id"] = "L1", ["name"] = "Receipts", ["messageListVisibility"] = "show" },
            new() { ["id"] = "L2", ["name"] = "OldTag" }
        };

        var desiredDicts = new List<Dictionary<string, object?>>
        {
            new() { ["name"] = "Receipts", ["messageListVisibility"] = "hide" },
            new() { ["name"] = "NewTag" }
        };

        var diffSet = GmailLabelDiffer.DiffSets(currentDicts, desiredDicts);

        Assert.Equal(1, diffSet.TotalCreations);
        Assert.Equal("NewTag", diffSet.Creations[0].Name);

        Assert.Equal(1, diffSet.TotalModifications);
        Assert.Equal("Receipts", diffSet.Modifications[0].Name);
        Assert.Equal("L1", diffSet.Modifications[0].Id);

        Assert.Equal(1, diffSet.TotalDeletions);
        Assert.Equal("OldTag", diffSet.Deletions[0].Name);
        Assert.Equal("L2", diffSet.Deletions[0].GetDeleteId());
    }

    [Fact]
    public void LabelFieldDiff_ToString_FormatsCorrectly()
    {
        var diff1 = new LabelFieldDiff("messageListVisibility", "show", "hide");
        Assert.Equal("messageListVisibility: show -> hide", diff1.ToString());

        var diff2 = new LabelFieldDiff("color", null, new LabelColor("#ffffff", "#000000"));
        Assert.Equal("color: <unset> -> [#ffffff / #000000]", diff2.ToString());

        var diff3 = new LabelFieldDiff("color", new LabelColor("#ffffff", "#000000"), null);
        Assert.Equal("color: [#ffffff / #000000] -> <unset>", diff3.ToString());
    }

    [Fact]
    public void LabelFieldDiff_EqualsAndHashCode()
    {
        var d1 = new LabelFieldDiff("name", "A", "B");
        var d2 = new LabelFieldDiff("name", "A", "B");
        var d3 = new LabelFieldDiff("name", "A", "C");

        Assert.Equal(d1, d2);
        Assert.NotEqual(d1, d3);
        Assert.Equal(d1.GetHashCode(), d2.GetHashCode());
    }

    [Fact]
    public void LabelDiff_ToString_FormatsAllTypes()
    {
        var added = new LabelDiff("TagA", null, LabelDiffType.Added, null, MakeLabel("TagA"));
        Assert.Contains("+ Label 'TagA' (Create)", added.ToString());

        var removed = new LabelDiff("TagB", "ID_B", LabelDiffType.Removed, MakeLabel("TagB", id: "ID_B"), null);
        Assert.Contains("- Label 'TagB' (Delete, ID: ID_B)", removed.ToString());

        var modified = new LabelDiff("TagC", "ID_C", LabelDiffType.Modified, MakeLabel("TagC", id: "ID_C"), MakeLabel("TagC"), new[]
        {
            new LabelFieldDiff("messageListVisibility", "show", "hide")
        });
        Assert.Contains("~ Label 'TagC'", modified.ToString());
        Assert.Contains("messageListVisibility: show -> hide", modified.ToString());

        var unchanged = new LabelDiff("TagD", "ID_D", LabelDiffType.Unchanged, MakeLabel("TagD", id: "ID_D"), MakeLabel("TagD", id: "ID_D"));
        Assert.Contains("Label 'TagD' (Unchanged)", unchanged.ToString());
    }

    [Fact]
    public void Diff_CaseInsensitiveNameMatchFalse_DetectsCaseDifference()
    {
        var current = MakeLabel("receipts", id: "L1");
        var desired = MakeLabel("Receipts");

        var options = new LabelDiffOptions { CaseInsensitiveNameMatch = false };
        var diff = GmailLabelDiffer.Diff(current, desired, options);

        Assert.Equal(LabelDiffType.Modified, diff.DiffType);
        Assert.Single(diff.FieldDifferences);
        Assert.Equal("name", diff.FieldDifferences[0].FieldName);
    }
}
