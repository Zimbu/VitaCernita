using System.Collections.Generic;
using System.Threading.Tasks;
using VitaCernita.Core.Actions;
using VitaCernita.Core.Actions.Validation;
using VitaCernita.Core.Filters;
using Xunit;

namespace VitaCernita.Tests;

public class ActionLanguageTests
{
    private readonly GmailFilterLoader _loader = new();

    // =========================================================================
    // 1. Special System Labels: Individual Action Operations
    // =========================================================================

    [Theory]
    [InlineData("return archive", true, false, false, false, false)]
    [InlineData("return archive()", true, false, false, false, false)]
    [InlineData("return Archive", true, false, false, false, false)]
    [InlineData("return action { archive = true }", true, false, false, false, false)]
    [InlineData("return actions { archive }", true, false, false, false, false)]
    [InlineData("return actions(archive)", true, false, false, false, false)]
    [InlineData("return action():archive():build()", true, false, false, false, false)]
    [InlineData("return mark_unread", false, true, false, false, false)]
    [InlineData("return mark_unread()", false, true, false, false, false)]
    [InlineData("return MarkUnread", false, true, false, false, false)]
    [InlineData("return mark_read", false, true, false, false, false)]
    [InlineData("return mark_as_read", false, true, false, false, false)]
    [InlineData("return action { mark_unread = true }", false, true, false, false, false)]
    [InlineData("return action { mark_read = true }", false, true, false, false, false)]
    [InlineData("return action { read = true }", false, true, false, false, false)]
    [InlineData("return action():mark_unread():build()", false, true, false, false, false)]
    [InlineData("return action():mark_read():build()", false, true, false, false, false)]
    [InlineData("return star", false, false, true, false, false)]
    [InlineData("return star()", false, false, true, false, false)]
    [InlineData("return Star", false, false, true, false, false)]
    [InlineData("return action { star = true }", false, false, true, false, false)]
    [InlineData("return actions(star)", false, false, true, false, false)]
    [InlineData("return action():star():build()", false, false, true, false, false)]
    [InlineData("return delete", false, false, false, true, false)]
    [InlineData("return delete()", false, false, false, true, false)]
    [InlineData("return Delete", false, false, false, true, false)]
    [InlineData("return action { delete = true }", false, false, false, true, false)]
    [InlineData("return action { trash = true }", false, false, false, true, false)]
    [InlineData("return action():delete():build()", false, false, false, true, false)]
    [InlineData("return action():trash():build()", false, false, false, true, false)]
    [InlineData("return mark_important", false, false, false, false, true)]
    [InlineData("return mark_important()", false, false, false, false, true)]
    [InlineData("return MarkImportant", false, false, false, false, true)]
    [InlineData("return action { mark_important = true }", false, false, false, false, true)]
    [InlineData("return action { important = true }", false, false, false, false, true)]
    [InlineData("return action():mark_important():build()", false, false, false, false, true)]
    [InlineData("return action():important():build()", false, false, false, false, true)]
    public async Task SystemLabels_IndividualActions_SetCorrectFlagsAndLabelIds(
        string luaScript, bool isArchive, bool isMarkUnread, bool isStarred, bool isDelete, bool isImportant)
    {
        var action = await _loader.LoadActionFromScriptAsync(luaScript);

        Assert.Equal(isArchive, action.IsArchive);
        Assert.Equal(isMarkUnread, action.IsMarkUnread);
        Assert.Equal(isStarred, action.IsStarred);
        Assert.Equal(isDelete, action.IsDelete);
        Assert.Equal(isImportant, action.IsImportant);

        if (isArchive)
        {
            Assert.Contains(SystemLabels.Inbox, action.RemoveLabelIds);
        }
        if (isMarkUnread)
        {
            Assert.Contains(SystemLabels.Unread, action.RemoveLabelIds);
        }
        if (isStarred)
        {
            Assert.Contains(SystemLabels.Starred, action.AddLabelIds);
        }
        if (isDelete)
        {
            Assert.Contains(SystemLabels.Trash, action.AddLabelIds);
        }
        if (isImportant)
        {
            Assert.Contains(SystemLabels.Important, action.AddLabelIds);
        }
    }

    // =========================================================================
    // 2. Category Actions: 6 Enumerated Categories
    // =========================================================================

    [Theory]
    [InlineData("return add_category('Primary')", SystemLabels.CategoryPersonal)]
    [InlineData("return add_category('primary')", SystemLabels.CategoryPersonal)]
    [InlineData("return add_category('personal')", SystemLabels.CategoryPersonal)]
    [InlineData("return add_category(category_primary)", SystemLabels.CategoryPersonal)]
    [InlineData("return add_category('Purchases')", SystemLabels.CategoryPurchases)]
    [InlineData("return add_category('purchases')", SystemLabels.CategoryPurchases)]
    [InlineData("return add_category('purchase')", SystemLabels.CategoryPurchases)]
    [InlineData("return add_category(category_purchases)", SystemLabels.CategoryPurchases)]
    [InlineData("return add_category('Social')", SystemLabels.CategorySocial)]
    [InlineData("return add_category('social')", SystemLabels.CategorySocial)]
    [InlineData("return add_category(category_social)", SystemLabels.CategorySocial)]
    [InlineData("return add_category('Updates')", SystemLabels.CategoryUpdates)]
    [InlineData("return add_category('updates')", SystemLabels.CategoryUpdates)]
    [InlineData("return add_category('update')", SystemLabels.CategoryUpdates)]
    [InlineData("return add_category(category_updates)", SystemLabels.CategoryUpdates)]
    [InlineData("return add_category('Forums')", SystemLabels.CategoryForums)]
    [InlineData("return add_category('forums')", SystemLabels.CategoryForums)]
    [InlineData("return add_category('forum')", SystemLabels.CategoryForums)]
    [InlineData("return add_category(category_forums)", SystemLabels.CategoryForums)]
    [InlineData("return add_category('Promotions')", SystemLabels.CategoryPromotions)]
    [InlineData("return add_category('promotions')", SystemLabels.CategoryPromotions)]
    [InlineData("return add_category('promotion')", SystemLabels.CategoryPromotions)]
    [InlineData("return add_category(category_promotions)", SystemLabels.CategoryPromotions)]
    [InlineData("return categorize('Purchases')", SystemLabels.CategoryPurchases)]
    [InlineData("return Categorize('Social')", SystemLabels.CategorySocial)]
    [InlineData("return action { add_category = 'Purchases' }", SystemLabels.CategoryPurchases)]
    [InlineData("return action { category = 'Social' }", SystemLabels.CategorySocial)]
    [InlineData("return action { categorize = 'Promotions' }", SystemLabels.CategoryPromotions)]
    [InlineData("return action():add_category('Updates'):build()", SystemLabels.CategoryUpdates)]
    [InlineData("return action():categorize('Forums'):build()", SystemLabels.CategoryForums)]
    public async Task AddCategory_EnumeratedCategories_MapsToCorrectSystemLabel(string luaScript, string expectedSystemLabel)
    {
        var action = await _loader.LoadActionFromScriptAsync(luaScript);
        Assert.Contains(expectedSystemLabel, action.AddLabelIds);
        Assert.Equal(expectedSystemLabel, action.Category);
    }

    // =========================================================================
    // 3. Custom Labels: add_label & add_labels
    // =========================================================================

    [Fact]
    public async Task AddLabel_SingleLabel_AddsCustomLabel()
    {
        string lua = "return add_label('Receipts')";
        var action = await _loader.LoadActionFromScriptAsync(lua);

        Assert.Contains("Receipts", action.AddLabelIds);
        Assert.Contains("Receipts", action.CustomLabels);
    }

    [Fact]
    public async Task AddLabel_MultipleCustomLabels_ViaAddLabelsVarargs()
    {
        string lua = "return add_labels('Finance', 'Taxes')";
        var action = await _loader.LoadActionFromScriptAsync(lua);

        Assert.Contains("Finance", action.AddLabelIds);
        Assert.Contains("Taxes", action.AddLabelIds);
        Assert.Equal(2, action.CustomLabels.Count);
    }

    [Fact]
    public async Task AddLabel_MultipleCustomLabels_ViaAddLabelsTable()
    {
        string lua = "return add_labels { 'Projects', 'Important-Clients' }";
        var action = await _loader.LoadActionFromScriptAsync(lua);

        Assert.Contains("Projects", action.AddLabelIds);
        Assert.Contains("Important-Clients", action.AddLabelIds);
    }

    [Fact]
    public async Task AddLabel_DeclarativeTable_SingleAndMultiple()
    {
        string lua = @"
return action {
    add_labels = { 'Invoices', '2026' }
}
";
        var action = await _loader.LoadActionFromScriptAsync(lua);

        Assert.Contains("Invoices", action.AddLabelIds);
        Assert.Contains("2026", action.AddLabelIds);
    }

    [Fact]
    public async Task AddLabel_ActionBuilder_ChainedLabels()
    {
        string lua = "return action():add_label('Work'):add_label('Urgent'):build()";
        var action = await _loader.LoadActionFromScriptAsync(lua);

        Assert.Contains("Work", action.AddLabelIds);
        Assert.Contains("Urgent", action.AddLabelIds);
    }

    // =========================================================================
    // 4. Forwarding: forward_message
    // =========================================================================

    [Theory]
    [InlineData("return forward_message('finance@example.com')", "finance@example.com")]
    [InlineData("return forward('accountant@tax.org')", "accountant@tax.org")]
    [InlineData("return action { forward = 'audit@corp.com' }", "audit@corp.com")]
    [InlineData("return action { forward_message = 'team@company.net' }", "team@company.net")]
    [InlineData("return action():forward_message('boss@example.com'):build()", "boss@example.com")]
    [InlineData("return action():forward('lead@example.com'):build()", "lead@example.com")]
    public async Task ForwardMessage_ValidEmails_SetsForwardProperty(string luaScript, string expectedEmail)
    {
        var action = await _loader.LoadActionFromScriptAsync(luaScript);
        Assert.Equal(expectedEmail, action.Forward);
    }

    // =========================================================================
    // 5. Multi-Action Composition & Gmail API Dictionary Mapping
    // =========================================================================

    [Fact]
    public async Task MultiAction_CompositeActionsFunction_PopulatesAllFields()
    {
        string lua = @"
return actions(
    archive,
    star,
    mark_important,
    add_category('Purchases'),
    add_label('Stripe'),
    forward_message('accounting@company.com')
)
";
        var action = await _loader.LoadActionFromScriptAsync(lua);

        Assert.True(action.IsArchive);
        Assert.True(action.IsStarred);
        Assert.True(action.IsImportant);
        Assert.Equal(SystemLabels.CategoryPurchases, action.Category);
        Assert.Contains("Stripe", action.CustomLabels);
        Assert.Equal("accounting@company.com", action.Forward);

        var dict = action.ToDictionary();
        Assert.True(dict.ContainsKey("addLabelIds"));
        Assert.True(dict.ContainsKey("removeLabelIds"));
        Assert.True(dict.ContainsKey("forward"));

        var addIds = Assert.IsAssignableFrom<List<string>>(dict["addLabelIds"]);
        var removeIds = Assert.IsAssignableFrom<List<string>>(dict["removeLabelIds"]);

        Assert.Contains(SystemLabels.Starred, addIds);
        Assert.Contains(SystemLabels.Important, addIds);
        Assert.Contains(SystemLabels.CategoryPurchases, addIds);
        Assert.Contains("Stripe", addIds);
        Assert.Contains(SystemLabels.Inbox, removeIds);
        Assert.Equal("accounting@company.com", dict["forward"]);
    }

    [Fact]
    public async Task MultiAction_DeclarativeTableSyntax_PopulatesAllFields()
    {
        string lua = @"
return action {
    archive = true,
    star = true,
    mark_important = true,
    add_category = 'Purchases',
    add_label = 'Stripe',
    forward = 'accounting@company.com'
}
";
        var action = await _loader.LoadActionFromScriptAsync(lua);

        Assert.True(action.IsArchive);
        Assert.True(action.IsStarred);
        Assert.True(action.IsImportant);
        Assert.Equal(SystemLabels.CategoryPurchases, action.Category);
        Assert.Contains("Stripe", action.CustomLabels);
        Assert.Equal("accounting@company.com", action.Forward);
    }

    [Fact]
    public async Task MultiAction_ActionBuilderFluentSyntax_PopulatesAllFields()
    {
        string lua = @"
return action()
    :archive()
    :star()
    :mark_important()
    :add_category('Purchases')
    :add_label('Stripe')
    :forward_message('accounting@company.com')
    :build()
";
        var action = await _loader.LoadActionFromScriptAsync(lua);

        Assert.True(action.IsArchive);
        Assert.True(action.IsStarred);
        Assert.True(action.IsImportant);
        Assert.Equal(SystemLabels.CategoryPurchases, action.Category);
        Assert.Contains("Stripe", action.CustomLabels);
        Assert.Equal("accounting@company.com", action.Forward);
    }

    // =========================================================================
    // 6. Decoupled Architecture: Action & Query Together or Separate
    // =========================================================================

    [Fact]
    public async Task Decoupled_StandaloneAction_LoadedAsFilter_HasNoCriteria()
    {
        string lua = "return action { archive = true, star = true }";
        var filter = await _loader.LoadFilterFromScriptAsync(lua);

        Assert.Null(filter.Criteria);
        Assert.NotNull(filter.Action);
        Assert.True(filter.Action.IsArchive);
        Assert.True(filter.Action.IsStarred);
        Assert.Equal(string.Empty, filter.ToGmailQuery());
    }

    [Fact]
    public async Task Decoupled_StandaloneQuery_LoadedAsFilter_HasNoAction()
    {
        string lua = "return from('boss@example.com')";
        var filter = await _loader.LoadFilterFromScriptAsync(lua);

        Assert.NotNull(filter.Criteria);
        Assert.Null(filter.Action);
        Assert.Equal("from:boss@example.com", filter.ToGmailQuery());
    }

    [Fact]
    public async Task Decoupled_CombinedFilter_FilterFunctionWithQueryAndAction()
    {
        string lua = @"
return filter {
    query = { from = 'billing@stripe.com' },
    action = actions(archive, add_category('Purchases'))
}
";
        var filter = await _loader.LoadFilterFromScriptAsync(lua);

        Assert.NotNull(filter.Criteria);
        Assert.NotNull(filter.Action);
        Assert.Equal("from:billing@stripe.com", filter.ToGmailQuery());
        Assert.True(filter.Action.IsArchive);
        Assert.Equal(SystemLabels.CategoryPurchases, filter.Action.Category);
    }

    [Fact]
    public async Task Decoupled_CombinedRule_WithNamedMetadata()
    {
        string lua = @"
return rule {
    name = 'Stripe Receipts',
    query = from('billing@stripe.com'),
    action = action():archive():add_category('Purchases'):add_label('Receipts'):build()
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);

        Assert.Equal("Stripe Receipts", rule.Name);
        Assert.NotNull(rule.Criteria);
        Assert.NotNull(rule.Action);
        Assert.Equal("from:billing@stripe.com", rule.ToGmailQuery());
        Assert.True(rule.Action.IsArchive);
        Assert.Equal(SystemLabels.CategoryPurchases, rule.Action.Category);
        Assert.Contains("Receipts", rule.Action.CustomLabels);
    }

    [Fact]
    public async Task Decoupled_CombinedFilter_FilterBuilderWithActionMethod()
    {
        string lua = @"
return filter()
    :from('boss@example.com')
    :action(archive)
    :build()
";
        var filter = await _loader.LoadFilterFromScriptAsync(lua);

        Assert.NotNull(filter.Criteria);
        Assert.NotNull(filter.Action);
        Assert.Equal("from:boss@example.com", filter.ToGmailQuery());
        Assert.True(filter.Action.IsArchive);
    }

    [Fact]
    public async Task Decoupled_CombinedFilter_FilterBuilderWithActionsMethod()
    {
        string lua = @"
return filter()
    :from('boss@example.com')
    :actions(archive, mark_important)
    :build()
";
        var filter = await _loader.LoadFilterFromScriptAsync(lua);

        Assert.NotNull(filter.Criteria);
        Assert.NotNull(filter.Action);
        Assert.Equal("from:boss@example.com", filter.ToGmailQuery());
        Assert.True(filter.Action.IsArchive);
        Assert.True(filter.Action.IsImportant);
    }

    [Fact]
    public async Task Decoupled_CombinedFilter_DirectFieldsWithAction()
    {
        string lua = @"
return {
    from = 'alerts@monitoring.com',
    subject = 'CPU Spike',
    action = star
}
";
        var filter = await _loader.LoadFilterFromScriptAsync(lua);

        Assert.NotNull(filter.Criteria);
        Assert.NotNull(filter.Action);
        Assert.Equal("from:alerts@monitoring.com subject:\"CPU Spike\"", filter.ToGmailQuery());
        Assert.True(filter.Action.IsStarred);
    }

    // =========================================================================
    // 7. Input Validation & Error Handling
    // =========================================================================

    [Theory]
    [InlineData("return add_category('Finance')")]
    [InlineData("return add_category('Unknown')")]
    [InlineData("return add_category('')")]
    [InlineData("return add_category('   ')")]
    public async Task Validation_InvalidCategory_ThrowsActionValidationException(string luaScript)
    {
        await Assert.ThrowsAsync<ActionValidationException>(() => _loader.LoadActionFromScriptAsync(luaScript));
    }

    [Theory]
    [InlineData("return add_label('')")]
    [InlineData("return add_label('   ')")]
    [InlineData("return add_label('INBOX')")]
    [InlineData("return add_label('STARRED')")]
    [InlineData("return add_label('TRASH')")]
    [InlineData("return add_label('UNREAD')")]
    [InlineData("return add_label('IMPORTANT')")]
    [InlineData("return add_label('CATEGORY_PERSONAL')")]
    [InlineData("return add_label('category_purchases')")]
    public async Task Validation_InvalidLabel_ThrowsActionValidationException(string luaScript)
    {
        await Assert.ThrowsAsync<ActionValidationException>(() => _loader.LoadActionFromScriptAsync(luaScript));
    }

    [Theory]
    [InlineData("return forward_message('')")]
    [InlineData("return forward_message('   ')")]
    [InlineData("return forward_message('not-an-email')")]
    [InlineData("return forward_message('@nodomain.com')")]
    [InlineData("return forward_message('user@')")]
    [InlineData("return forward_message('user @domain.com')")]
    public async Task Validation_InvalidForwardEmail_ThrowsActionValidationException(string luaScript)
    {
        await Assert.ThrowsAsync<ActionValidationException>(() => _loader.LoadActionFromScriptAsync(luaScript));
    }

    [Fact]
    public void Validation_UnknownActionString_ThrowsActionValidationException()
    {
        Assert.Throws<ActionValidationException>(() => GmailActionParser.ParseActionString("invalid_action_name"));
    }
}
