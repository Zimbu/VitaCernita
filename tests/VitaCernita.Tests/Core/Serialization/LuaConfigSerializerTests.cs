using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VitaCernita.Core.Actions;
using VitaCernita.Core.AutoReply;
using VitaCernita.Core.Diffing;
using VitaCernita.Core.Diffing.AutoReply;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Diffing.Filters;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Diffing.Labels;
using VitaCernita.Core.Queries;
using VitaCernita.Core.Serialization;
using VitaCernita.Core.Sources;
using Xunit;

namespace VitaCernita.Tests.Core.Serialization;

public class LuaConfigSerializerTests
{
    private readonly GmailFilterLoader _loader = new();
    private readonly LuaConfigSerializer _serializer = LuaConfigSerializer.Default;

    // =========================================================================
    // 1. Label Serialization & Round-Trip Tests
    // =========================================================================

    [Fact]
    public async Task RoundTrip_SingleLabel_MatchesDiff()
    {
        var original = new GmailLabel(
            name: "Finance/Invoices",
            id: "lbl_fin_01",
            messageListVisibility: MessageListVisibility.Show,
            labelListVisibility: LabelListVisibility.LabelShow,
            color: new LabelColor("#ffffff", "#43d692"));

        string lua = _serializer.SerializeLabel(original);

        Assert.Contains("return label {", lua);
        Assert.Contains("id = \"lbl_fin_01\"", lua);
        Assert.Contains("name = \"Finance/Invoices\"", lua);
        Assert.Contains("message_list_visibility = \"show\"", lua);
        Assert.Contains("label_list_visibility = \"labelShow\"", lua);
        Assert.Contains("color = color(\"white\", \"#43d692\")", lua);

        var reloaded = await _loader.LoadLabelFromScriptAsync(lua);
        var diff = GmailLabelDiffer.Diff(original, reloaded);

        Assert.False(diff.HasChanges);
        Assert.Equal(DiffKind.Unchanged, diff.DiffType);
    }

    [Fact]
    public async Task RoundTrip_MultipleLabels_PreservesAllAttributes()
    {
        var labels = new List<GmailLabel>
        {
            new("Urgent", "lbl_urg", MessageListVisibility.Show, LabelListVisibility.LabelShow, new LabelColor("#ffffff", "#fb4c2f")),
            new("Archive/2026", "lbl_arch", MessageListVisibility.Hide, LabelListVisibility.LabelHide, null),
            new("Read Later", "lbl_rl", null, LabelListVisibility.LabelShowIfUnread, new LabelColor("#000000", "#efefef")),
            new("Projects/Alpha", null, null, null, new LabelColor("#4a86e8", "#c9daf8"))
        };

        string lua = _serializer.SerializeLabels(labels);

        Assert.Contains("labels = {", lua);

        var reloadedLabels = await _loader.LoadLabelsFromScriptAsync(lua);
        var diff = GmailLabelDiffer.DiffSets(labels, reloadedLabels);

        Assert.False(diff.HasDifferences);
        Assert.Equal(4, reloadedLabels.Count);
    }

    [Fact]
    public async Task RoundTrip_Labels_WithoutColorAliases_PreservesHex()
    {
        var label = new GmailLabel("CustomColor", "lbl_01", color: new LabelColor("#ffffff", "#000000"));

        var options = new LuaSerializerOptions { PreferColorAliases = false };
        string lua = _serializer.SerializeLabel(label, options);

        Assert.Contains("color = color(\"#ffffff\", \"#000000\")", lua);

        var reloaded = await _loader.LoadLabelFromScriptAsync(lua);
        var diff = GmailLabelDiffer.Diff(label, reloaded);

        Assert.False(diff.HasChanges);
    }

    // =========================================================================
    // 2. Filter Serialization & Round-Trip Tests
    // =========================================================================

    [Fact]
    public async Task RoundTrip_SingleFilter_BasicFieldQueryAndAction()
    {
        var filter = new GmailFilter(
            id: "f_basic",
            name: "GitHub Notifications",
            query: new FieldCondition("from", "notifications@github.com"),
            action: new GmailAction().Archive().Star().AddCustomLabel("GitHub"));

        string lua = _serializer.SerializeFilter(filter);

        Assert.Contains("return filter {", lua);
        Assert.Contains("id = \"f_basic\"", lua);
        Assert.Contains("name = \"GitHub Notifications\"", lua);
        Assert.Contains("query = From(\"notifications@github.com\")", lua);
        Assert.Contains("action = actions(archive, star, add_label(\"GitHub\"))", lua);

        var reloaded = await _loader.LoadFilterFromScriptAsync(lua);
        var diff = GmailFilterDiffer.Diff(filter, reloaded);

        Assert.False(diff.HasChanges);
        Assert.Equal(DiffKind.Unchanged, diff.DiffType);
    }

    [Fact]
    public async Task RoundTrip_Filter_WithAddAndRemoveLabel_PreservesBoth()
    {
        var filter = new GmailFilter(
            id: "f_labels",
            name: "Label Migrations",
            query: new FieldCondition("from", "team@example.com"),
            action: new GmailAction()
                .Archive()
                .AddCustomLabel("NewProject")
                .RemoveCustomLabel("OldProject"));

        string lua = _serializer.SerializeFilter(filter);

        Assert.Contains("action = actions(archive, add_label(\"NewProject\"), remove_label(\"OldProject\"))", lua);

        var reloaded = await _loader.LoadFilterFromScriptAsync(lua);
        var diff = GmailFilterDiffer.Diff(filter, reloaded);

        Assert.False(diff.HasChanges);
        Assert.Equal(DiffKind.Unchanged, diff.DiffType);
        Assert.Contains("NewProject", reloaded.Action!.CustomLabels);
        Assert.Contains("OldProject", reloaded.Action!.CustomRemoveLabels);
    }

    [Fact]
    public async Task RoundTrip_ComplexBooleanQueryFilter()
    {
        var complexQuery = new AndCondition(new IQueryCondition[]
        {
            new OrCondition(new IQueryCondition[]
            {
                new FieldCondition("from", "billing@stripe.com"),
                new FieldCondition("from", "invoices@aws.amazon.com")
            }),
            new FieldCondition("filename", "invoice.pdf"),
            new NotCondition(new FieldCondition("subject", "draft")),
            new ExactMatchCondition("paid in full")
        });

        var action = new GmailAction()
            .Archive()
            .MarkImportant()
            .AddCategory(SystemLabels.CategoryPurchases)
            .AddCustomLabel("Receipts")
            .SetForward("accounting@company.com");

        var filter = new GmailFilter("f_complex", complexQuery, action, "Vendor Invoices");

        string lua = _serializer.SerializeFilter(filter);

        Assert.Contains("query = And(", lua);
        Assert.Contains("Or(", lua);
        Assert.Contains("From(\"billing@stripe.com\")", lua);
        Assert.Contains("Filename(\"invoice.pdf\")", lua);
        Assert.Contains("Not(Subject(\"draft\"))", lua);
        Assert.Contains("match(\"paid in full\")", lua);
        Assert.Contains("add_category(\"Purchases\")", lua);
        Assert.Contains("forward_message(\"accounting@company.com\")", lua);

        var reloaded = await _loader.LoadFilterFromScriptAsync(lua);
        var diff = GmailFilterDiffer.Diff(filter, reloaded);

        Assert.False(diff.HasChanges);
    }

    [Fact]
    public async Task RoundTrip_AllFieldConditionTypes()
    {
        var conditions = new List<IQueryCondition>
        {
            new FieldCondition("from", "user@test.com"),
            new FieldCondition("to", "team@test.com"),
            new FieldCondition("cc", "audit@test.com"),
            new FieldCondition("bcc", "shadow@test.com"),
            new FieldCondition("subject", "Important Update"),
            new FieldCondition("list", "devs@lists.test.com"),
            new FieldCondition("filename", "report.xlsx"),
            new FieldCondition("deliveredto", "inbox@company.com"),
            new FieldCondition("rfc822msgid", "msg-12345@test.com"),
            new FieldCondition("header", "X-Priority:High")
        };

        var filter = new GmailFilter("f_fields", new AndCondition(conditions), new GmailAction().Archive());

        string lua = _serializer.SerializeFilter(filter);
        var reloaded = await _loader.LoadFilterFromScriptAsync(lua);
        var diff = GmailFilterDiffer.Diff(filter, reloaded);

        Assert.False(diff.HasChanges);
    }

    [Fact]
    public async Task RoundTrip_StarIcons_MetadataAndLocationConditions()
    {
        var conditions = new List<IQueryCondition>
        {
            new HasCondition("yellow-star"),
            new HasCondition("red-bang"),
            new HasCondition("attachment"),
            new HasCondition("drive"),
            new HasCondition("userlabels"),
            new IsCondition("starred"),
            new IsCondition("unread"),
            new IsCondition("important"),
            new InCondition("inbox"),
            new InCondition("archive"),
            new CategoryCondition("promotions"),
            new CategoryCondition("purchases"),
            new SizeCondition("larger", "10M"),
            new DateCondition("after", new DateTime(2026, 1, 15)),
            new DurationCondition("older_than", "14d")
        };

        var filter = new GmailFilter("f_meta", new AndCondition(conditions), new GmailAction().Star());

        string lua = _serializer.SerializeFilter(filter);
        var reloaded = await _loader.LoadFilterFromScriptAsync(lua);
        var diff = GmailFilterDiffer.Diff(filter, reloaded);

        Assert.False(diff.HasChanges);
    }

    [Fact]
    public async Task RoundTrip_RawQueryCondition_PreservesExactQuery()
    {
        var rawCondition = new RawQueryCondition("from:service@paypal.com has:attachment larger:5M");
        var filter = new GmailFilter("f_raw", rawCondition, new GmailAction().Delete());

        string lua = _serializer.SerializeFilter(filter);
        Assert.Contains("raw_query(\"from:service@paypal.com has:attachment larger:5M\")", lua);

        var reloaded = await _loader.LoadFilterFromScriptAsync(lua);
        var diff = GmailFilterDiffer.Diff(filter, reloaded);

        Assert.False(diff.HasChanges);
    }

    [Fact]
    public async Task RoundTrip_MultipleFilters_PreservesSetFidelity()
    {
        var filters = new List<GmailFilter>
        {
            new("f_01", new FieldCondition("from", "alerts@bank.com"), new GmailAction().Star().MarkImportant(), "Banking Alerts"),
            new("f_02", new FieldCondition("subject", "unsubscribe"), new GmailAction().Archive(), "Newsletters"),
            new("f_03", new ExactMatchCondition("confidential"), new GmailAction().AddCustomLabel("Sensitive"), "Security")
        };

        string lua = _serializer.SerializeFilters(filters);
        var reloaded = await _loader.LoadFiltersFromScriptAsync(lua);
        var diff = GmailFilterDiffer.DiffSets(filters, reloaded);

        Assert.False(diff.HasDifferences);
        Assert.Equal(3, reloaded.Count);
    }

    // =========================================================================
    // 3. AutoReply Serialization & Round-Trip Tests
    // =========================================================================

    [Fact]
    public async Task RoundTrip_AutoReply_EnabledWithDatesAndRestrictions()
    {
        var autoReply = new AutoReply
        {
            EnableAutoReply = true,
            ResponseSubject = "Out of Office: Summer Vacation",
            ResponseBodyPlainText = "I will be away from the office with limited email access.",
            ResponseBodyHtml = "<p>I will be away from the office with limited email access.</p>",
            RestrictToContacts = true,
            RestrictToDomain = false,
            StartTime = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(),
            EndTime = new DateTimeOffset(2026, 7, 15, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds()
        };

        string lua = _serializer.SerializeAutoReply(autoReply);

        Assert.Contains("auto_reply = auto_reply {", lua);
        Assert.Contains("enabled = true", lua);
        Assert.Contains("subject = \"Out of Office: Summer Vacation\"", lua);
        Assert.Contains("contacts_only = true", lua);
        Assert.Contains("start_date = \"2026-07-01\"", lua);
        Assert.Contains("end_date = \"2026-07-15\"", lua);

        var reloaded = await _loader.LoadAutoReplyFromScriptAsync(lua);
        Assert.NotNull(reloaded);

        var diff = AutoReplyDiffer.Diff(autoReply, reloaded);
        Assert.False(diff.HasChanges);
        Assert.Equal(DiffKind.Unchanged, diff.DiffType);
    }

    [Fact]
    public async Task RoundTrip_AutoReply_DisabledState()
    {
        var autoReply = new AutoReply
        {
            EnableAutoReply = false,
            ResponseSubject = "Previously Configured Subject",
            ResponseBodyPlainText = "Previous body"
        };

        string lua = _serializer.SerializeAutoReply(autoReply);
        Assert.Contains("enabled = false", lua);

        var reloaded = await _loader.LoadAutoReplyFromScriptAsync(lua);
        Assert.NotNull(reloaded);

        var diff = AutoReplyDiffer.Diff(autoReply, reloaded);
        Assert.False(diff.HasChanges);
    }

    [Fact]
    public async Task RoundTrip_AutoReply_TimestampWithTimeComponent()
    {
        var startDto = new DateTimeOffset(2026, 10, 1, 8, 30, 0, TimeSpan.Zero);
        var autoReply = new AutoReply
        {
            EnableAutoReply = true,
            ResponseSubject = "Conference",
            ResponseBodyPlainText = "At conference",
            StartTime = startDto.ToUnixTimeMilliseconds()
        };

        string lua = _serializer.SerializeAutoReply(autoReply);
        Assert.Contains("start_time = \"2026-10-01T08:30:00Z\"", lua);

        var reloaded = await _loader.LoadAutoReplyFromScriptAsync(lua);
        Assert.NotNull(reloaded);

        var diff = AutoReplyDiffer.Diff(autoReply, reloaded);
        Assert.False(diff.HasChanges);
    }

    // =========================================================================
    // 4. Unified IGmailSource & Full Config Round-Trip Tests
    // =========================================================================

    [Fact]
    public async Task RoundTrip_UnifiedGmailSource_FullConfiguration()
    {
        var labels = new List<GmailLabel>
        {
            new("Work/Projects", "lbl_proj", MessageListVisibility.Show, LabelListVisibility.LabelShow, new LabelColor("#ffffff", "#16a766")),
            new("Receipts", "lbl_rec", MessageListVisibility.Show, LabelListVisibility.LabelShow, new LabelColor("#ffffff", "#43d692"))
        };

        var filters = new List<GmailFilter>
        {
            new("f_001",
                new AndCondition(new IQueryCondition[]
                {
                    new FieldCondition("from", "billing@stripe.com"),
                    new FieldCondition("filename", "invoice.pdf")
                }),
                new GmailAction().Archive().Star().AddCategory(SystemLabels.CategoryPurchases).AddCustomLabel("Receipts"),
                "Stripe Invoices"),
            new("f_002",
                new FieldCondition("from", "urgent@company.com"),
                new GmailAction().MarkImportant().AddCustomLabel("Work/Projects"),
                "Urgent Work")
        };

        var autoReply = new AutoReply
        {
            EnableAutoReply = true,
            ResponseSubject = "Auto-Reply: Away",
            ResponseBodyPlainText = "I am away until Monday.",
            RestrictToContacts = true,
            StartTime = new DateTimeOffset(2026, 12, 20, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(),
            EndTime = new DateTimeOffset(2026, 12, 28, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds()
        };

        var originalSource = new InMemoryGmailSource(labels, filters, autoReply, "Original In-Memory");

        // Serialize the abstract IGmailSource to Lua string
        string luaScript = await originalSource.ToLuaAsync();

        // Create a LuaGmailSource from the generated script
        var reloadedSource = LuaGmailSource.FromScript(luaScript, "Reloaded Lua Source");

        // Diff all three components using the source differ
        var labelDiff = await GmailSourceDiffer.DiffLabelsAsync(originalSource, reloadedSource);
        var filterDiff = await GmailSourceDiffer.DiffFiltersAsync(originalSource, reloadedSource);
        var autoReplyDiff = await GmailSourceDiffer.DiffAutoReplyAsync(originalSource, reloadedSource);

        Assert.False(labelDiff.HasDifferences, $"Labels had diffs: {labelDiff}");
        Assert.False(filterDiff.HasDifferences, $"Filters had diffs: {filterDiff}");
        Assert.False(autoReplyDiff.HasChanges, $"AutoReply had diffs: {autoReplyDiff}");
    }

    [Fact]
    public async Task RoundTrip_EmptySource_ProducesEmptyConfigWithoutDifferences()
    {
        var emptySource = new InMemoryGmailSource(name: "Empty");

        string lua = await emptySource.ToLuaAsync();
        Assert.Contains("return {}", lua);

        var reloadedSource = LuaGmailSource.FromScript(lua);

        var labelDiff = await GmailSourceDiffer.DiffLabelsAsync(emptySource, reloadedSource);
        var filterDiff = await GmailSourceDiffer.DiffFiltersAsync(emptySource, reloadedSource);
        var autoReplyDiff = await GmailSourceDiffer.DiffAutoReplyAsync(emptySource, reloadedSource);

        Assert.False(labelDiff.HasDifferences);
        Assert.False(filterDiff.HasDifferences);
        Assert.False(autoReplyDiff.HasChanges);
    }

    [Fact]
    public async Task RoundTrip_FileIO_SerializesAndLoadsFromFile()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"vitacernita_test_{Guid.NewGuid():N}.lua");

        try
        {
            var labels = new[] { new GmailLabel("TempTest", "lbl_tmp") };
            var filters = new[] { new GmailFilter("f_tmp", new FieldCondition("from", "tmp@test.com"), new GmailAction().Archive()) };
            var source = new InMemoryGmailSource(labels, filters, name: "TempSource");

            await source.ToLuaFileAsync(tempFile);
            Assert.True(File.Exists(tempFile));

            var fileSource = new LuaGmailSource(tempFile);

            var labelDiff = await GmailSourceDiffer.DiffLabelsAsync(source, fileSource);
            var filterDiff = await GmailSourceDiffer.DiffFiltersAsync(source, fileSource);

            Assert.False(labelDiff.HasDifferences);
            Assert.False(filterDiff.HasDifferences);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void LuaConfigSerializer_CustomOptions_AppliesFormatting()
    {
        var label = new GmailLabel("OptionsTest", "lbl_opt");

        var options = new LuaSerializerOptions
        {
            IndentSpaces = 2,
            IncludeHeaderComment = false
        };

        string lua = _serializer.SerializeLabel(label, options);

        Assert.DoesNotContain("-- =======================================================================", lua);
        Assert.Contains("  id = \"lbl_opt\"", lua);
        Assert.Contains("  name = \"OptionsTest\"", lua);
    }

    [Fact]
    public async Task RoundTrip_NestedNotAndOr_ComplexConditions()
    {
        var complexQuery = new AndCondition(new IQueryCondition[]
        {
            new NotCondition(new OrCondition(new IQueryCondition[]
            {
                new FieldCondition("from", "spammer1@test.com"),
                new FieldCondition("from", "spammer2@test.com")
            })),
            new OrCondition(new IQueryCondition[]
            {
                new FieldCondition("subject", "Urgent"),
                new FieldCondition("subject", "Action Required")
            }),
            new NotCondition(new FieldCondition("to", "ignored@company.com"))
        });

        var filter = new GmailFilter("f_nested_bool", complexQuery, new GmailAction().Star());

        string lua = _serializer.SerializeFilter(filter);
        var reloaded = await _loader.LoadFilterFromScriptAsync(lua);
        var diff = GmailFilterDiffer.Diff(filter, reloaded);

        Assert.False(diff.HasChanges);
    }

    [Fact]
    public async Task RoundTrip_ActionWithAllFeatures_PreservesEquivalence()
    {
        var action = new GmailAction()
            .Archive()
            .MarkUnread()
            .Star()
            .Delete()
            .MarkImportant()
            .AddCategory(SystemLabels.CategorySocial)
            .AddCustomLabel("Custom/Label 1")
            .AddCustomLabel("Custom/Label 2")
            .SetForward("audit@company.com");

        var filter = new GmailFilter("f_all_actions", new FieldCondition("from", "test@company.com"), action);

        string lua = _serializer.SerializeFilter(filter);
        var reloaded = await _loader.LoadFilterFromScriptAsync(lua);
        var diff = GmailFilterDiffer.Diff(filter, reloaded);

        Assert.False(diff.HasChanges);
    }

    [Fact]
    public async Task RoundTrip_LargeSourceWithManyItems_ZeroDifferences()
    {
        var labels = new List<GmailLabel>();
        for (int i = 1; i <= 25; i++)
        {
            string mlv = (i % 2 == 0) ? MessageListVisibility.Show : MessageListVisibility.Hide;
            string llv = (i % 3 == 0) ? LabelListVisibility.LabelShow : (i % 3 == 1) ? LabelListVisibility.LabelHide : LabelListVisibility.LabelShowIfUnread;
            var color = (i % 2 == 0) ? new LabelColor("#ffffff", "#4a86e8") : null;
            labels.Add(new GmailLabel($"Category/Tag_{i:D2}", $"lbl_{i:D3}", mlv, llv, color));
        }

        var filters = new List<GmailFilter>();
        for (int i = 1; i <= 25; i++)
        {
            var action = new GmailAction().AddCustomLabel($"Category/Tag_{i:D2}");
            if (i % 2 == 0) action.Archive();
            if (i % 3 == 0) action.Star();
            if (i % 5 == 0) action.MarkImportant();

            filters.Add(new GmailFilter(
                id: $"filter_{i:D3}",
                query: new FieldCondition("from", $"service_{i}@example.com"),
                action: action,
                name: $"Auto Filter {i}"));
        }

        var autoReply = new AutoReply
        {
            EnableAutoReply = true,
            ResponseSubject = "Batch Out of Office",
            ResponseBodyPlainText = "Out of office test body",
            RestrictToContacts = true,
            RestrictToDomain = true,
            StartTime = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(),
            EndTime = new DateTimeOffset(2026, 11, 30, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds()
        };

        var originalSource = new InMemoryGmailSource(labels, filters, autoReply, "LargeSource");

        string lua = await originalSource.ToLuaAsync();
        var reloadedSource = LuaGmailSource.FromScript(lua, "ReloadedLargeSource");

        var labelDiff = await GmailSourceDiffer.DiffLabelsAsync(originalSource, reloadedSource);
        var filterDiff = await GmailSourceDiffer.DiffFiltersAsync(originalSource, reloadedSource);
        var autoReplyDiff = await GmailSourceDiffer.DiffAutoReplyAsync(originalSource, reloadedSource);

        Assert.False(labelDiff.HasDifferences, $"Labels had diffs: {labelDiff.Differences.Count}");
        Assert.False(filterDiff.HasDifferences, $"Filters had diffs: {filterDiff.Differences.Count}");
        Assert.False(autoReplyDiff.HasChanges);
        Assert.Equal(25, labelDiff.TotalUnchanged);
        Assert.Equal(25, filterDiff.TotalUnchanged);
    }

    [Fact]
    public async Task RoundTrip_LinqSubsetDiffing_EvaluatesEqual()
    {
        var labels = new List<GmailLabel>
        {
            new("Work/Current", "lbl_work", MessageListVisibility.Show, LabelListVisibility.LabelShow),
            new("Personal/Family", "lbl_fam", MessageListVisibility.Hide, LabelListVisibility.LabelHide)
        };

        var filters = new List<GmailFilter>
        {
            new("f_w1", new FieldCondition("from", "boss@corp.com"), new GmailAction().Star(), "Work 1"),
            new("f_p1", new FieldCondition("from", "mom@fam.com"), new GmailAction().Star(), "Personal 1")
        };

        var originalSource = new InMemoryGmailSource(labels, filters);
        string lua = await originalSource.ToLuaAsync();
        var reloadedSource = LuaGmailSource.FromScript(lua);

        // Diff only work-related labels using LINQ
        var labelSubsetDiff = await GmailSourceDiffer.DiffLabelsAsync(
            originalSource,
            reloadedSource,
            current => current.Where(l => l.Name.StartsWith("Work/")),
            desired => desired.Where(l => l.Name.StartsWith("Work/")));

        Assert.False(labelSubsetDiff.HasDifferences);
        Assert.Single(labelSubsetDiff.Unchanged);

        // Diff only personal-related filters using LINQ
        var filterSubsetDiff = await GmailSourceDiffer.DiffFiltersAsync(
            originalSource,
            reloadedSource,
            current => current.Where(f => f.Name != null && f.Name.StartsWith("Personal")),
            desired => desired.Where(f => f.Name != null && f.Name.StartsWith("Personal")));

        Assert.False(filterSubsetDiff.HasDifferences);
        Assert.Single(filterSubsetDiff.Unchanged);
    }
}
