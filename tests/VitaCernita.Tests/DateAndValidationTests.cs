using System.Threading.Tasks;
using Xunit;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Filters.Validation;

namespace VitaCernita.Tests;

public class DateAndValidationTests
{
    private readonly GmailFilterLoader _loader = new();

    // =========================================================================
    // Date Operators: after, before, older, newer
    // =========================================================================

    [Theory]
    [InlineData("after", "04/16/2024", "after:2024/04/16")]
    [InlineData("before", "2024/12/31", "before:2024/12/31")]
    [InlineData("older", "01-15-2024", "older:2024/01/15")]
    [InlineData("newer", "2024-06-30", "newer:2024/06/30")]
    public async Task DateOperators_DefaultFormats_EmitNormalizedYearMonthDay(string op, string inputDate, string expectedQuery)
    {
        string lua = $"return {op}('{inputDate}')";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);

        Assert.Equal(expectedQuery, rule.ToGmailQuery());
    }

    [Fact]
    public async Task DateOperators_DeclarativeTable_EmitsNormalizedFormat()
    {
        string lua = @"
return {
    after = '04/16/2024',
    before = '2024/12/31'
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.Equal("after:2024/04/16 before:2024/12/31", rule.ToGmailQuery());
    }

    // =========================================================================
    // Global date_format Configuration Option in Lua
    // =========================================================================

    [Fact]
    public async Task DateOperators_WithGlobalCustomDateFormat_EnforcesCustomFormat()
    {
        // Custom format specified globally in Lua: MM-dd-YYYY
        string lua = @"
return {
    date_format = 'MM-dd-YYYY',
    rules = {
        rule {
            match = And(
                after('12-25-2024'),
                before('12-31-2024')
            )
        }
    }
}
";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);

        // Emitted format on the Gmail filter must always be yyyy/MM/dd
        Assert.Equal("after:2024/12/25 before:2024/12/31", rule.ToGmailQuery());
    }

    [Fact]
    public async Task DateOperators_WithGlobalCustomDateFormat_RejectsDatesInOtherFormats()
    {
        // Configured for MM-dd-yyyy, but providing yyyy/MM/dd
        string lua = @"
return {
    date_format = 'MM-dd-yyyy',
    match = after('2024/12/25')
}
";
        await Assert.ThrowsAsync<FilterValidationException>(() => _loader.LoadRuleFromScriptAsync(lua));
    }

    // =========================================================================
    // Date Validation: Rejection of Invalid Dates & Times
    // =========================================================================

    [Theory]
    [InlineData("2024/04/16 10:30:00")] // Contains time with colons
    [InlineData("2024-04-16T12:00:00")] // ISO with time
    [InlineData("04/16/2024 10:00 AM")] // Contains AM/PM
    public async Task DateValidation_RejectsDatesWithTimes(string invalidDateWithTime)
    {
        string lua = $"return after('{invalidDateWithTime}')";
        await Assert.ThrowsAsync<FilterValidationException>(() => _loader.LoadRuleFromScriptAsync(lua));
    }

    [Theory]
    [InlineData("02/30/2024")] // February 30 does not exist
    [InlineData("13/01/2024")] // Month 13 does not exist
    [InlineData("not-a-date")] // Nonsense string
    [InlineData("2024/02/29/10")] // Malformed date
    public async Task DateValidation_RejectsCalendarInvalidDates(string invalidDate)
    {
        string lua = $"return before('{invalidDate}')";
        await Assert.ThrowsAsync<FilterValidationException>(() => _loader.LoadRuleFromScriptAsync(lua));
    }

    // =========================================================================
    // Duration Operators: older_than, newer_than
    // =========================================================================

    [Theory]
    [InlineData("older_than", "1y", "older_than:1y")]
    [InlineData("older_than", "2d", "older_than:2d")]
    [InlineData("newer_than", "6m", "newer_than:6m")]
    [InlineData("newer_than", "14D", "newer_than:14d")] // Normalizes uppercase
    public async Task DurationOperators_ValidDurations_EmitCorrectFormat(string op, string duration, string expectedQuery)
    {
        string lua = $"return {op}('{duration}')";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);

        Assert.Equal(expectedQuery, rule.ToGmailQuery());
    }

    [Theory]
    [InlineData("older_than", "0d")]      // Non-positive integer
    [InlineData("older_than", "-2m")]     // Negative duration
    [InlineData("newer_than", "5w")]      // 'w' is not a recognized Gmail duration unit
    [InlineData("newer_than", "10s")]     // 's' is not a recognized Gmail duration unit
    [InlineData("older_than", "10years")] // Full word not allowed
    [InlineData("newer_than", "abc")]     // Non-numeric
    [InlineData("older_than", "")]        // Empty
    public async Task DurationOperators_InvalidDurations_ThrowValidationException(string op, string invalidDuration)
    {
        string lua = $"return {op}('{invalidDuration}')";
        await Assert.ThrowsAsync<FilterValidationException>(() => _loader.LoadRuleFromScriptAsync(lua));
    }

    // =========================================================================
    // String Field Validation: Non-Empty / Non-Whitespace
    // =========================================================================

    [Theory]
    [InlineData("subject")]
    [InlineData("filename")]
    [InlineData("header")]
    [InlineData("list")]
    [InlineData("label")]
    [InlineData("match")]
    public async Task StringFields_RejectEmptyOrWhitespaceOnly(string field)
    {
        string luaEmpty = $"return {field}('')";
        await Assert.ThrowsAsync<FilterValidationException>(() => _loader.LoadRuleFromScriptAsync(luaEmpty));

        string luaWhitespace = $"return {field}('   ')";
        await Assert.ThrowsAsync<FilterValidationException>(() => _loader.LoadRuleFromScriptAsync(luaWhitespace));
    }

    // =========================================================================
    // Email Address & Fragment Validation: from, to, cc, bcc, delivered-to
    // =========================================================================

    [Theory]
    [InlineData("alice@example.com")]           // Full email
    [InlineData("alice.smith")]                 // Left-side fragment (username)
    [InlineData("example.com")]                 // Right-side fragment (domain)
    [InlineData("@company.org")]                // Right-side fragment with @
    [InlineData("support+billing")]             // Plus-addressing fragment
    [InlineData("dev_team-ops")]                // Underscores and hyphens
    [InlineData("Alice Smith <alice@co.com>")]  // Display name with address
    public async Task EmailFields_AcceptValidAddressesAndFragments(string validEmailOrFragment)
    {
        string lua = $"return from('{validEmailOrFragment}')";
        var rule = await _loader.LoadRuleFromScriptAsync(lua);
        Assert.NotNull(rule);
    }

    [Theory]
    [InlineData("user@@example.com")]   // Two consecutive @
    [InlineData("a@b@c.com")]           // Two separate @
    [InlineData("first@mid@last")]      // Two @
    public async Task EmailFields_RejectMoreThanOneAtSign(string invalidEmail)
    {
        string lua = $"return to('{invalidEmail}')";
        await Assert.ThrowsAsync<FilterValidationException>(() => _loader.LoadRuleFromScriptAsync(lua));
    }

    [Theory]
    [InlineData("user;name@example.com")] // Semicolon
    [InlineData("user/name@example.com")] // Slash
    [InlineData("user:name@example.com")] // Colon
    [InlineData("user name")]             // Space without brackets
    [InlineData("user..name@example.com")]// Consecutive dots
    [InlineData("user?test@example.com")] // Question mark
    public async Task EmailFields_RejectUnsupportedCharacters(string invalidEmail)
    {
        string lua = $"return from('{invalidEmail}')";
        await Assert.ThrowsAsync<FilterValidationException>(() => _loader.LoadRuleFromScriptAsync(lua));
    }
}
