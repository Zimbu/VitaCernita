using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using VitaCernita.Core.Actions;
using VitaCernita.Core.Api;
using VitaCernita.Core.Api.Fakes;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Labels.Diff;
using VitaCernita.Core.Queries;
using VitaCernita.Core.Sources;

namespace VitaCernita.Tests;

public class GmailSourceTests
{
    #region LuaGmailSource Tests

    [Fact]
    public async Task LuaGmailSource_FromScript_ReturnsQueryableLabelsAndFilters()
    {
        string lua = @"
            return {
                labels = {
                    label { name = 'Finance/Receipts', message_list_visibility = 'show' },
                    label { name = 'Finance/Invoices', message_list_visibility = 'hide' },
                    label { name = 'Engineering', message_list_visibility = 'show' }
                },
                filters = {
                    filter {
                        id = 'f_stripe',
                        query = From('billing@stripe.com'),
                        action = actions(archive, add_label('Finance/Receipts'))
                    }
                }
            }
        ";

        var source = LuaGmailSource.FromScript(lua, "TestLua");
        Assert.Equal("TestLua", source.Name);

        var labels = await source.GetLabelsAsync();
        Assert.Equal(3, labels.Count());

        // LINQ query over Lua configuration
        var financeLabels = labels.Where(l => l.Name.StartsWith("Finance/")).ToList();
        Assert.Equal(2, financeLabels.Count);

        var filters = await source.GetFiltersAsync();
        Assert.Single(filters);
        Assert.Equal("f_stripe", filters.First().Id);
    }

    #endregion

    #region ApiGmailSource Tests

    [Fact]
    public async Task ApiGmailSource_AdaptsApiClientToQueryable()
    {
        var fakeClient = new FakeGmailApiClient();
        fakeClient.AddLabel(new GmailLabel("Work", id: "L1", messageListVisibility: "show"));
        fakeClient.AddLabel(new GmailLabel("Personal", id: "L2", messageListVisibility: "hide"));
        fakeClient.AddFilter(new GmailFilter("F1", new RawQueryCondition("from:boss@corp.com")));

        var source = new ApiGmailSource(fakeClient, "user@example.com", name: "Corp Mailbox");
        Assert.Equal("Corp Mailbox", source.Name);

        var labels = await source.GetLabelsAsync();
        Assert.Equal(2, labels.Count());

        // LINQ predicate
        var visibleLabels = labels.Where(l => l.MessageListVisibility == "show").ToList();
        Assert.Single(visibleLabels);
        Assert.Equal("Work", visibleLabels[0].Name);

        var filters = await source.GetFiltersAsync();
        Assert.Single(filters);
        Assert.Equal("from:boss@corp.com", filters.First().ToGmailQuery());
    }

    #endregion

    #region InMemoryGmailSource Tests

    [Fact]
    public async Task InMemoryGmailSource_SupportsMutationAndLinq()
    {
        var source = new InMemoryGmailSource();
        source.AddLabel(new GmailLabel("Tag1"));
        source.AddLabel(new GmailLabel("Tag2", color: new LabelColor("white", "black")));

        var labels = await source.GetLabelsAsync();
        Assert.Equal(2, labels.Count());

        var colored = labels.Where(l => l.Color != null).ToList();
        Assert.Single(colored);
        Assert.Equal("Tag2", colored[0].Name);
    }

    #endregion

    #region GmailSourceDiffer Agnostic Diffing Tests

    [Fact]
    public async Task GmailSourceDiffer_DiffsLuaAgainstApiAgnostically()
    {
        // 1. Current State: Target Gmail Account (via FakeClient)
        var fakeClient = new FakeGmailApiClient();
        fakeClient.AddLabel(new GmailLabel("Receipts", id: "L_REC", messageListVisibility: "show"));
        fakeClient.AddLabel(new GmailLabel("DeprecatedTag", id: "L_DEP"));
        var currentSource = new ApiGmailSource(fakeClient, "me", name: "Target Account");

        // 2. Desired State: Local Lua configuration
        string lua = @"
            return {
                label { name = 'Receipts', message_list_visibility = 'hide' },
                label { name = 'NewFeatureTag' }
            }
        ";
        var desiredSource = LuaGmailSource.FromScript(lua, "Local Lua");

        // 3. Agnostic diff across both sources
        var diff = await GmailSourceDiffer.DiffLabelsAsync(currentSource, desiredSource);

        Assert.True(diff.HasDifferences);
        Assert.Equal(1, diff.TotalCreations);
        Assert.Equal("NewFeatureTag", diff.Creations[0].Name);

        Assert.Equal(1, diff.TotalModifications);
        Assert.Equal("Receipts", diff.Modifications[0].Name);
        Assert.Equal("L_REC", diff.Modifications[0].Id);

        Assert.Equal(1, diff.TotalDeletions);
        Assert.Equal("DeprecatedTag", diff.Deletions[0].Name);
        Assert.Equal("L_DEP", diff.Deletions[0].GetDeleteId());
    }

    [Fact]
    public async Task GmailSourceDiffer_CrossAccountDiffing_ReplicatesAccountBToAccountA()
    {
        // Source Account A (e.g. staging account)
        var clientA = new FakeGmailApiClient();
        clientA.AddLabel(new GmailLabel("Billing", id: "LA_1", messageListVisibility: "show"));
        clientA.AddLabel(new GmailLabel("OldStagingTag", id: "LA_2"));
        var accountA = new ApiGmailSource(clientA, "staging@corp.com", name: "Staging Account");

        // Source Account B (e.g. production template account)
        var clientB = new FakeGmailApiClient();
        clientB.AddLabel(new GmailLabel("Billing", id: "LB_1", messageListVisibility: "hide"));
        clientB.AddLabel(new GmailLabel("Security", id: "LB_2", messageListVisibility: "show"));
        var accountB = new ApiGmailSource(clientB, "prod@corp.com", name: "Production Template");

        // Compare account A (current) with account B (desired)
        var diff = await GmailSourceDiffer.DiffLabelsAsync(accountA, accountB);

        Assert.Equal(1, diff.TotalCreations);
        Assert.Equal("Security", diff.Creations[0].Name);

        Assert.Equal(1, diff.TotalModifications);
        Assert.Equal("Billing", diff.Modifications[0].Name);

        Assert.Equal(1, diff.TotalDeletions);
        Assert.Equal("OldStagingTag", diff.Deletions[0].Name);
    }

    [Fact]
    public async Task GmailSourceDiffer_LinqSubsetFiltering_DiffsOnlyTargetHierarchy()
    {
        // Current account contains Finance and Engineering labels
        var currentSource = new InMemoryGmailSource(new[]
        {
            new GmailLabel("Finance/Invoices", id: "F1", messageListVisibility: "show"),
            new GmailLabel("Finance/OldReceipts", id: "F2"),
            new GmailLabel("Eng/Bug", id: "E1", messageListVisibility: "show")
        }, name: "Current Mailbox");

        // Desired source also contains Finance and Engineering labels
        var desiredSource = new InMemoryGmailSource(new[]
        {
            new GmailLabel("Finance/Invoices", messageListVisibility: "hide"),
            new GmailLabel("Finance/NewTax2025"),
            new GmailLabel("Eng/Feature", messageListVisibility: "show")
        }, name: "Desired Spec");

        // Diff ONLY "Finance/" hierarchy using LINQ filters on both sides
        var diff = await GmailSourceDiffer.DiffLabelsAsync(
            currentSource,
            desiredSource,
            currentFilter: q => q.Where(l => l.Name.StartsWith("Finance/")),
            desiredFilter: q => q.Where(l => l.Name.StartsWith("Finance/"))
        );

        // Eng/* labels must be completely excluded from diff
        Assert.Equal(1, diff.TotalCreations);
        Assert.Equal("Finance/NewTax2025", diff.Creations[0].Name);

        Assert.Equal(1, diff.TotalModifications);
        Assert.Equal("Finance/Invoices", diff.Modifications[0].Name);

        Assert.Equal(1, diff.TotalDeletions);
        Assert.Equal("Finance/OldReceipts", diff.Deletions[0].Name);

        Assert.DoesNotContain(diff.Differences, d => d.Name.StartsWith("Eng/"));
    }

    [Fact]
    public async Task GmailSourceDiffer_LinqSubsetFiltering_DiffsOnlyColoredLabels()
    {
        var currentSource = new InMemoryGmailSource(new[]
        {
            new GmailLabel("PlainLabel", id: "P1"),
            new GmailLabel("ColoredTag", id: "C1", color: new LabelColor("white", "black"))
        });

        var desiredSource = new InMemoryGmailSource(new[]
        {
            new GmailLabel("PlainLabel", messageListVisibility: "hide"), // Not colored
            new GmailLabel("ColoredTag", color: new LabelColor("black", "white"))
        });

        // Filter diff to only colored labels
        var diff = await GmailSourceDiffer.DiffLabelsAsync(
            currentSource,
            desiredSource,
            currentFilter: q => q.Where(l => l.Color != null),
            desiredFilter: q => q.Where(l => l.Color != null)
        );

        Assert.Single(diff.Modifications);
        Assert.Equal("ColoredTag", diff.Modifications[0].Name);
        Assert.Empty(diff.Creations);
        Assert.Empty(diff.Deletions);
        Assert.DoesNotContain(diff.Differences, d => d.Name == "PlainLabel");
    }

    #endregion
}
