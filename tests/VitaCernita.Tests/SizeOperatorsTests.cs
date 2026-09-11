using System.Threading.Tasks;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Filters.Validation;
using Xunit;

namespace VitaCernita.Tests;

public class SizeOperatorsTests
{
    private readonly GmailFilterLoader _loader = new();

    // =========================================================================
    // 1. Direct DSL Expressions (Callable and Values)
    // =========================================================================

    [Theory]
    [InlineData("return size('10M')", "size:10M")]
    [InlineData("return size('10m')", "size:10M")]
    [InlineData("return size('10mb')", "size:10M")]
    [InlineData("return size('10MB')", "size:10M")]
    [InlineData("return Size('10M')", "size:10M")]
    [InlineData("return size('500k')", "size:500K")]
    [InlineData("return size('500K')", "size:500K")]
    [InlineData("return size('500kb')", "size:500K")]
    [InlineData("return size('500KB')", "size:500K")]
    [InlineData("return size('1g')", "size:1G")]
    [InlineData("return size('1G')", "size:1G")]
    [InlineData("return size('1gb')", "size:1G")]
    [InlineData("return size('1GB')", "size:1G")]
    [InlineData("return size('1000000')", "size:1000000")]
    [InlineData("return size(1000000)", "size:1000000")]
    [InlineData("return size('1000b')", "size:1000")]
    [InlineData("return size('1000B')", "size:1000")]
    [InlineData("return larger('10M')", "larger:10M")]
    [InlineData("return Larger('10M')", "larger:10M")]
    [InlineData("return larger_than('10M')", "larger:10M")]
    [InlineData("return LargerThan('10M')", "larger:10M")]
    [InlineData("return larger(5000000)", "larger:5000000")]
    [InlineData("return smaller('5M')", "smaller:5M")]
    [InlineData("return Smaller('5M')", "smaller:5M")]
    [InlineData("return smaller_than('5M')", "smaller:5M")]
    [InlineData("return SmallerThan('5M')", "smaller:5M")]
    [InlineData("return smaller(2000000)", "smaller:2000000")]
    public async Task Size_EmitsCorrectQuery(string luaScript, string expectedQuery)
    {
        var rule = await _loader.LoadRuleFromScriptAsync(luaScript);
        Assert.Equal(expectedQuery, rule.ToGmailQuery());
    }

    // =========================================================================
    // 2. Declarative Table Syntax
    // =========================================================================

    [Fact]
    public async Task TableSyntax_SizeString_EmitsCorrectQuery()
    {
        string lua = @"
return {
    from = 'reports@company.com',
    size = '10M'
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:reports@company.com size:10M", rule.ToGmailQuery());
    }

    [Fact]
    public async Task TableSyntax_SizeNumber_EmitsCorrectQuery()
    {
        string lua = @"
return {
    from = 'reports@company.com',
    size = 1000000
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:reports@company.com size:1000000", rule.ToGmailQuery());
    }

    [Fact]
    public async Task TableSyntax_LargerAndSmaller_EmitsCorrectQuery()
    {
        string lua = @"
return {
    from = 'reports@company.com',
    larger = '5M',
    smaller = '20M'
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:reports@company.com larger:5M smaller:20M", rule.ToGmailQuery());
    }

    [Fact]
    public async Task TableSyntax_LargerThanAndSmallerThan_EmitsCorrectQuery()
    {
        string lua = @"
return {
    from = 'reports@company.com',
    larger_than = '5M',
    smaller_than = '20M'
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:reports@company.com larger:5M smaller:20M", rule.ToGmailQuery());
    }

    [Fact]
    public async Task TableSyntax_SizeArray_EmitsAllConditions()
    {
        string lua = @"
return {
    from = 'reports@company.com',
    larger = { '5M', 10000000 }
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:reports@company.com larger:10000000 larger:5M", rule.ToGmailQuery());
    }

    // =========================================================================
    // 3. Logical Operator Combinations (And, Or, Not)
    // =========================================================================

    [Fact]
    public async Task Logical_AndWithSizeOperator_EmitsCorrectQuery()
    {
        string lua = @"
return And(
    From('archive@company.com'),
    larger('10M')
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:archive@company.com larger:10M", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Logical_OrWithSizeOperator_EmitsCorrectQuery()
    {
        string lua = @"
return Or(
    larger('10M'),
    smaller('1M')
)
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("larger:10M OR smaller:1M", rule.ToGmailQuery());
    }

    [Fact]
    public async Task Logical_NotWithSizeOperator_EmitsCorrectQuery()
    {
        string lua = "return not(larger('10M'))";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("-larger:10M", rule.ToGmailQuery());

        string luaCompound = "return not(Or(larger('10M'), smaller('1M')))";
        var ruleCompound = await _loader.LoadRuleFromScriptAsync(luaCompound);
        Assert.Equal("-(larger:10M OR smaller:1M)", ruleCompound.ToGmailQuery());
    }

    // =========================================================================
    // 4. FilterBuilder Chaining
    // =========================================================================

    [Fact]
    public async Task FilterBuilder_SizeMethods_ChainsCorrectly()
    {
        string lua = @"
return filter()
    :from('admin@company.com')
    :size('10M')
    :build()
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:admin@company.com size:10M", rule.ToGmailQuery());
    }

    [Fact]
    public async Task FilterBuilder_LargerSmallerMethods_ChainsCorrectly()
    {
        string lua = @"
return filter()
    :from('admin@company.com')
    :larger('5M')
    :smaller('20M')
    :build()
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:admin@company.com larger:5M smaller:20M", rule.ToGmailQuery());
    }

    [Fact]
    public async Task FilterBuilder_LargerThanSmallerThanMethods_ChainsCorrectly()
    {
        string lua = @"
return filter()
    :from('admin@company.com')
    :larger_than('5M')
    :smaller_than('20M')
    :build()
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("from:admin@company.com larger:5M smaller:20M", rule.ToGmailQuery());
    }

    // =========================================================================
    // 5. Input Validation
    // =========================================================================

    [Fact]
    public async Task Validation_InvalidSize_ThrowsQueryValidationException()
    {
        string luaInvalidStr = "return size('abc')";
        await Assert.ThrowsAsync<QueryValidationException>(() => _loader.LoadRuleFromScriptAsync(luaInvalidStr));

        string luaInvalidUnit = "return size('10TB')";
        await Assert.ThrowsAsync<QueryValidationException>(() => _loader.LoadRuleFromScriptAsync(luaInvalidUnit));

        string luaDecimal = "return size('1.5M')";
        await Assert.ThrowsAsync<QueryValidationException>(() => _loader.LoadRuleFromScriptAsync(luaDecimal));

        string luaNegative = "return size('-10M')";
        await Assert.ThrowsAsync<QueryValidationException>(() => _loader.LoadRuleFromScriptAsync(luaNegative));

        string luaZero = "return size('0')";
        await Assert.ThrowsAsync<QueryValidationException>(() => _loader.LoadRuleFromScriptAsync(luaZero));

        string luaZeroM = "return size('0M')";
        await Assert.ThrowsAsync<QueryValidationException>(() => _loader.LoadRuleFromScriptAsync(luaZeroM));
    }

    [Fact]
    public async Task Validation_EmptySize_ThrowsQueryValidationException()
    {
        string luaEmpty = "return size('')";
        await Assert.ThrowsAsync<QueryValidationException>(() => _loader.LoadRuleFromScriptAsync(luaEmpty));

        string luaWhitespace = "return size('   ')";
        await Assert.ThrowsAsync<QueryValidationException>(() => _loader.LoadRuleFromScriptAsync(luaWhitespace));
    }

    [Fact]
    public void Validation_InvalidSizeOperator_ThrowsQueryValidationException()
    {
        Assert.Throws<QueryValidationException>(() => new SizeCondition("invalid_op", "10M"));
    }
}
