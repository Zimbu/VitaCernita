using System.Threading.Tasks;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Filters.Validation;
using Xunit;

namespace VitaCernita.Tests;

public class CategoryOperatorsTests
{
    private readonly GmailFilterLoader _loader = new();

    // =========================================================================
    // 1. Direct DSL Expressions (Callable and Values)
    // =========================================================================

    [Theory]
    [InlineData("return category_primary", "category:primary")]
    [InlineData("return category_primary()", "category:primary")]
    [InlineData("return CategoryPrimary", "category:primary")]
    [InlineData("return Category('primary')", "category:primary")]
    [InlineData("return category('primary')", "category:primary")]
    [InlineData("return category_social", "category:social")]
    [InlineData("return category_social()", "category:social")]
    [InlineData("return CategorySocial", "category:social")]
    [InlineData("return Category('social')", "category:social")]
    [InlineData("return category('social')", "category:social")]
    [InlineData("return category_promotions", "category:promotions")]
    [InlineData("return category_promotions()", "category:promotions")]
    [InlineData("return CategoryPromotions", "category:promotions")]
    [InlineData("return category_promotion", "category:promotions")]
    [InlineData("return Category('promotions')", "category:promotions")]
    [InlineData("return category('promotions')", "category:promotions")]
    [InlineData("return category('promotion')", "category:promotions")]
    [InlineData("return category_updates", "category:updates")]
    [InlineData("return category_updates()", "category:updates")]
    [InlineData("return CategoryUpdates", "category:updates")]
    [InlineData("return category_update", "category:updates")]
    [InlineData("return Category('updates')", "category:updates")]
    [InlineData("return category('updates')", "category:updates")]
    [InlineData("return category('update')", "category:updates")]
    [InlineData("return category_forums", "category:forums")]
    [InlineData("return category_forums()", "category:forums")]
    [InlineData("return CategoryForums", "category:forums")]
    [InlineData("return category_forum", "category:forums")]
    [InlineData("return Category('forums')", "category:forums")]
    [InlineData("return category('forums')", "category:forums")]
    [InlineData("return category('forum')", "category:forums")]
    [InlineData("return category_reservations", "category:reservations")]
    [InlineData("return category_reservations()", "category:reservations")]
    [InlineData("return CategoryReservations", "category:reservations")]
    [InlineData("return category_reservation", "category:reservations")]
    [InlineData("return Category('reservations')", "category:reservations")]
    [InlineData("return category('reservations')", "category:reservations")]
    [InlineData("return category('reservation')", "category:reservations")]
    [InlineData("return category_purchases", "category:purchases")]
    [InlineData("return category_purchases()", "category:purchases")]
    [InlineData("return CategoryPurchases", "category:purchases")]
    [InlineData("return category_purchase", "category:purchases")]
    [InlineData("return Category('purchases')", "category:purchases")]
    [InlineData("return category('purchases')", "category:purchases")]
    [InlineData("return category('purchase')", "category:purchases")]
    public async Task Category_EmitsCorrectQuery(string luaScript, string expectedQuery)
    {
        var rule = await _loader.LoadRuleFromScriptAsync(luaScript);
        Assert.Equal(expectedQuery, rule.ToGmailQuery());
    }

    // =========================================================================
    // 2. Declarative Table Syntax
    // =========================================================================

    [Fact]
    public async Task TableSyntax_CategoryKey_EmitsCorrectQuery()
    {
        string lua = @"
return {
    from = 'deals@store.com',
    category = 'promotions'
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:deals@store.com category:promotions", rule.ToGmailQuery());
    }

    [Fact]
    public async Task TableSyntax_BooleanKeys_EmitCategoryQueries()
    {
        string lua = @"
return {
    from = 'news@socialnetwork.com',
    category_social = true
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:news@socialnetwork.com category:social", rule.ToGmailQuery());
    }

    [Fact]
    public async Task TableSyntax_CategoryArrayOfStrings_EmitsAllCategoryConditions()
    {
        string lua = @"
return {
    from = 'alerts@service.com',
    category = { 'updates', 'forums' }
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:alerts@service.com category:forums category:updates", rule.ToGmailQuery());
    }

    // =========================================================================
    // 3. Logical Operator Combinations (And, Or, Not)
    // =========================================================================

    [Fact]
    public async Task Logical_AndWithCategoryOperator_EmitsCorrectQuery()
    {
        string lua = @"
return And(
    From('billing@company.com'),
    category_purchases
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:billing@company.com category:purchases", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Logical_OrWithCategoryOperator_EmitsCorrectQuery()
    {
        string lua = @"
return Or(
    category_promotions,
    category_social
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("category:promotions OR category:social", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Logical_NotWithCategoryOperator_EmitsCorrectQuery()
    {
        string lua = "return not(category_promotions)";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-category:promotions", rule.ToGmailQuery());

        string luaCompound = "return not(Or(category_promotions, category_social))";
        var ruleCompound = await _loader.LoadRuleFromScriptAsync(luaCompound);
        Assert.Equal("-(category:promotions OR category:social)", ruleCompound.ToGmailQuery());
    }

    // =========================================================================
    // 4. FilterBuilder Chaining
    // =========================================================================

    [Fact]
    public async Task FilterBuilder_CategoryMethods_ChainsCorrectly()
    {
        string lua = @"
return filter()
    :from('airline@flights.com')
    :category_reservations()
    :build()
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:airline@flights.com category:reservations", rule.ToGmailQuery());
    }

    [Fact]
    public async Task FilterBuilder_CategoryDirectMethod_ChainsCorrectly()
    {
        string lua = @"
return filter()
    :from('admin@company.com')
    :Category('updates')
    :build()
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:admin@company.com category:updates", rule.ToGmailQuery());
    }

    // =========================================================================
    // 5. Input Validation
    // =========================================================================

    [Fact]
    public async Task Validation_InvalidCategoryTarget_ThrowsQueryValidationException()
    {
        string lua = "return category('unknown_category')";
        await Assert.ThrowsAsync<QueryValidationException>(() => _loader.LoadRuleFromScriptAsync(lua));
    }

    [Fact]
    public async Task Validation_EmptyCategoryTarget_ThrowsQueryValidationException()
    {
        string luaEmpty = "return category('')";
        await Assert.ThrowsAsync<QueryValidationException>(() => _loader.LoadRuleFromScriptAsync(luaEmpty));

        string luaWhitespace = "return category('   ')";
        await Assert.ThrowsAsync<QueryValidationException>(() => _loader.LoadRuleFromScriptAsync(luaWhitespace));
    }
}
