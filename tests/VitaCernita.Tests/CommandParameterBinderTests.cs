using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Spectre.Console;
using VitaCernita.Cli.Commands;
using VitaCernita.Cli.Commands.Binding;
using Xunit;

namespace VitaCernita.Tests;

public class CommandParameterBinderTests
{
    private enum OutputFormat
    {
        Text,
        Json,
        Yaml
    }

    [Command("test-types")]
    private class AllTypesCommand : ICliCommand
    {
        [Option("str", 's', Description = "String option")]
        public string? Text { get; set; }

        [Option("num", 'n', Description = "Integer option")]
        public int Number { get; set; }

        [Option("nullable-num", Description = "Nullable integer option")]
        public int? NullableNumber { get; set; }

        [Option("ratio", Description = "Double option")]
        public double Ratio { get; set; }

        [Option("flag", 'f', Description = "Boolean flag")]
        public bool Flag { get; set; }

        [Option("verbose", 'v', Description = "Second boolean flag")]
        public bool Verbose { get; set; }

        [Option("format", Description = "Enum format")]
        public OutputFormat Format { get; set; } = OutputFormat.Text;

        [Option("items", Description = "List of items")]
        public List<string>? Items { get; set; }

        public Task<int> ExecuteAsync(string[] args) => Task.FromResult(0);
    }

    [Command("test-required")]
    private class RequiredCommand : ICliCommand
    {
        [Option("token", 't', Required = true, Description = "Required token")]
        public string? Token { get; set; }

        [Option("optional", 'o', Description = "Optional flag")]
        public string? Optional { get; set; }

        public Task<int> ExecuteAsync(string[] args) => Task.FromResult(0);
    }

    [Command("test-alias")]
    private class AliasCommand : ICliCommand
    {
        [Option("diff", 'd', Aliases = ["dry-run", "sync-plan"], Description = "Diff switch")]
        public bool Diff { get; set; }

        public Task<int> ExecuteAsync(string[] args) => Task.FromResult(0);
    }

    [Command("collision-long")]
    private class DuplicateLongOptionCommand : ICliCommand
    {
        [Option("name")]
        public string? Name1 { get; set; }

        [Option("name")]
        public string? Name2 { get; set; }

        public Task<int> ExecuteAsync(string[] args) => Task.FromResult(0);
    }

    [Command("collision-short")]
    private class DuplicateShortOptionCommand : ICliCommand
    {
        [Option('a')]
        public string? Opt1 { get; set; }

        [Option('a')]
        public string? Opt2 { get; set; }

        public Task<int> ExecuteAsync(string[] args) => Task.FromResult(0);
    }

    [Command("collision-alias")]
    private class AliasCollisionCommand : ICliCommand
    {
        [Option("primary")]
        public string? Opt1 { get; set; }

        [Option("other", Aliases = ["primary"])]
        public string? Opt2 { get; set; }

        public Task<int> ExecuteAsync(string[] args) => Task.FromResult(0);
    }

    [Command("self-alias")]
    private class SelfAliasCommand : ICliCommand
    {
        [Option("primary", Aliases = ["primary"])]
        public string? Opt1 { get; set; }

        public Task<int> ExecuteAsync(string[] args) => Task.FromResult(0);
    }

    [Fact]
    public void Bind_StringAndNumericOptions_BindsCorrectly()
    {
        var cmd = new AllTypesCommand();
        var binder = new CommandParameterBinder();

        var result = binder.Bind(cmd, new[]
        {
            "--str", "hello world",
            "--num=42",
            "--nullable-num", "100",
            "--ratio", "3.14"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("hello world", cmd.Text);
        Assert.Equal(42, cmd.Number);
        Assert.Equal(100, cmd.NullableNumber);
        Assert.Equal(3.14, cmd.Ratio);
    }

    [Fact]
    public void Bind_ShortOptions_SpaceAndEquals_BindsCorrectly()
    {
        var cmd = new AllTypesCommand();
        var binder = new CommandParameterBinder();

        var result = binder.Bind(cmd, new[] { "-s", "val1", "-n=99" });

        Assert.True(result.IsSuccess);
        Assert.Equal("val1", cmd.Text);
        Assert.Equal(99, cmd.Number);
    }

    [Fact]
    public void Bind_BooleanSwitches_FlagPresenceAndNegation_BindsCorrectly()
    {
        var cmd = new AllTypesCommand();
        var binder = new CommandParameterBinder();

        var result = binder.Bind(cmd, new[] { "--flag", "--no-verbose" });

        Assert.True(result.IsSuccess);
        Assert.True(cmd.Flag);
        Assert.False(cmd.Verbose);
    }

    [Fact]
    public void Bind_BundledShortSwitches_BindsAllSwitches()
    {
        var cmd = new AllTypesCommand();
        var binder = new CommandParameterBinder();

        var result = binder.Bind(cmd, new[] { "-fv" });

        Assert.True(result.IsSuccess);
        Assert.True(cmd.Flag);
        Assert.True(cmd.Verbose);
    }

    [Fact]
    public void Bind_AttachedShortValue_BindsCorrectly()
    {
        var cmd = new AllTypesCommand();
        var binder = new CommandParameterBinder();

        var result = binder.Bind(cmd, new[] { "-smy-string" });

        Assert.True(result.IsSuccess);
        Assert.Equal("my-string", cmd.Text);
    }

    [Fact]
    public void Bind_EnumParsing_CaseInsensitive()
    {
        var cmd = new AllTypesCommand();
        var binder = new CommandParameterBinder();

        var result = binder.Bind(cmd, new[] { "--format", "json" });

        Assert.True(result.IsSuccess);
        Assert.Equal(OutputFormat.Json, cmd.Format);
    }

    [Fact]
    public void Bind_EnumParsing_InvalidValue_ReturnsError()
    {
        var cmd = new AllTypesCommand();
        var binder = new CommandParameterBinder();

        var result = binder.Bind(cmd, new[] { "--format", "xml" });

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid value 'xml'", result.ErrorMessage);
        Assert.Contains("Text, Json, Yaml", result.ErrorMessage);
    }

    [Fact]
    public void Bind_InvalidNumber_ReturnsError()
    {
        var cmd = new AllTypesCommand();
        var binder = new CommandParameterBinder();

        var result = binder.Bind(cmd, new[] { "--num", "not-a-number" });

        Assert.False(result.IsSuccess);
        Assert.Contains("expected a valid int32", result.ErrorMessage);
    }

    [Fact]
    public void Bind_MissingValueForOption_ReturnsError()
    {
        var cmd = new AllTypesCommand();
        var binder = new CommandParameterBinder();

        var result = binder.Bind(cmd, new[] { "--str" });

        Assert.False(result.IsSuccess);
        Assert.Contains("Missing value for option '--str'", result.ErrorMessage);
    }

    [Fact]
    public void Bind_RequiredOption_Missing_ReturnsError()
    {
        var cmd = new RequiredCommand();
        var binder = new CommandParameterBinder();

        var result = binder.Bind(cmd, new[] { "--optional", "foo" });

        Assert.False(result.IsSuccess);
        Assert.Contains("Missing required option '--token'", result.ErrorMessage);
    }

    [Fact]
    public void Bind_RequiredOption_Provided_Succeeds()
    {
        var cmd = new RequiredCommand();
        var binder = new CommandParameterBinder();

        var result = binder.Bind(cmd, new[] { "--token", "secret123" });

        Assert.True(result.IsSuccess);
        Assert.Equal("secret123", cmd.Token);
    }

    [Fact]
    public void Bind_Aliases_BindsToTargetProperty()
    {
        var cmd1 = new AliasCommand();
        var cmd2 = new AliasCommand();
        var binder = new CommandParameterBinder();

        var res1 = binder.Bind(cmd1, new[] { "--dry-run" });
        var res2 = binder.Bind(cmd2, new[] { "--sync-plan" });

        Assert.True(res1.IsSuccess);
        Assert.True(cmd1.Diff);
        Assert.True(res2.IsSuccess);
        Assert.True(cmd2.Diff);
    }

    [Fact]
    public void Bind_HelpFlags_ReturnsHelpRequested()
    {
        var cmd = new AllTypesCommand();
        var binder = new CommandParameterBinder();

        Assert.True(binder.Bind(cmd, new[] { "-h" }).HelpRequested);
        Assert.True(binder.Bind(cmd, new[] { "--help" }).HelpRequested);
    }

    [Fact]
    public void Bind_PositionalAndDoubleDash_ReturnsUnhandledArguments()
    {
        var cmd = new AllTypesCommand();
        var binder = new CommandParameterBinder();

        var result = binder.Bind(cmd, new[] { "positional1", "--str", "hello", "--", "--not-a-flag", "extra" });

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", cmd.Text);
        Assert.Equal(new[] { "positional1", "--not-a-flag", "extra" }, result.UnhandledArguments);
    }

    [Fact]
    public void Bind_CommaSeparatedList_BindsList()
    {
        var cmd = new AllTypesCommand();
        var binder = new CommandParameterBinder();

        var result = binder.Bind(cmd, new[] { "--items", "a,b,c" });

        Assert.True(result.IsSuccess);
        Assert.NotNull(cmd.Items);
        Assert.Equal(new[] { "a", "b", "c" }, cmd.Items);
    }

    [Fact]
    public void IntraCommandCollision_DuplicateLongName_ThrowsInvalidOperationException()
    {
        var cmd = new DuplicateLongOptionCommand();
        var binder = new CommandParameterBinder();

        Assert.Throws<InvalidOperationException>(() => binder.Bind(cmd, Array.Empty<string>()));
    }

    [Fact]
    public void IntraCommandCollision_DuplicateShortName_ThrowsInvalidOperationException()
    {
        var cmd = new DuplicateShortOptionCommand();
        var binder = new CommandParameterBinder();

        Assert.Throws<InvalidOperationException>(() => binder.Bind(cmd, Array.Empty<string>()));
    }

    [Fact]
    public void IntraCommandCollision_AliasCollision_ThrowsInvalidOperationException()
    {
        var cmd = new AliasCollisionCommand();
        var binder = new CommandParameterBinder();

        Assert.Throws<InvalidOperationException>(() => binder.Bind(cmd, Array.Empty<string>()));
    }

    [Fact]
    public void IntraCommandCollision_SelfMatchingAlias_ThrowsInvalidOperationException()
    {
        var cmd = new SelfAliasCommand();
        var binder = new CommandParameterBinder();

        Assert.Throws<InvalidOperationException>(() => binder.Bind(cmd, Array.Empty<string>()));
    }

    [Fact]
    public void CommandHelpRenderer_RendersOptionTable()
    {
        var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(writer)
        });
        var cmd = new AllTypesCommand();

        CommandHelpRenderer.Render(cmd, console);
        string output = writer.ToString();

        Assert.Contains("Usage: vitacernita test-types", output);
        Assert.Contains("-s, --str <str>", output);
        Assert.Contains("-n, --num <num>", output);
        Assert.Contains("-f, --flag", output);
        Assert.Contains("-h, --help", output);
    }
}
