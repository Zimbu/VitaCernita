using System;
using System.Threading.Tasks;
using VitaCernita.Cli.Commands;
using Xunit;

namespace VitaCernita.Tests;

public class CommandDispatcherTests
{
    [Command("sample-a", Description = "Sample A command", Aliases = ["a", "sa"])]
    private class SampleACommand : ICliCommand
    {
        public Task<int> ExecuteAsync(string[] args) => Task.FromResult(0);
        public void PrintHelp() { }
    }

    [Command("sample-b", Description = "Sample B command", Aliases = ["b"])]
    private class SampleBCommand : ICliCommand
    {
        public Task<int> ExecuteAsync(string[] args) => Task.FromResult(0);
        public void PrintHelp() { }
    }

    [Command("SAMPLE-A", Description = "Colliding uppercase name")]
    private class CollidingNameCommand : ICliCommand
    {
        public Task<int> ExecuteAsync(string[] args) => Task.FromResult(0);
        public void PrintHelp() { }
    }

    [Command("custom", Aliases = ["sa"])]
    private class CollidingAliasWithAliasCommand : ICliCommand
    {
        public Task<int> ExecuteAsync(string[] args) => Task.FromResult(0);
        public void PrintHelp() { }
    }

    [Command("sa", Description = "Name matches existing alias")]
    private class NameMatchesExistingAliasCommand : ICliCommand
    {
        public Task<int> ExecuteAsync(string[] args) => Task.FromResult(0);
        public void PrintHelp() { }
    }

    [Command("other", Aliases = ["sample-a"])]
    private class AliasMatchesExistingNameCommand : ICliCommand
    {
        public Task<int> ExecuteAsync(string[] args) => Task.FromResult(0);
        public void PrintHelp() { }
    }

    [Command("self-match", Aliases = ["self-match"])]
    private class SelfMatchingAliasCommand : ICliCommand
    {
        public Task<int> ExecuteAsync(string[] args) => Task.FromResult(0);
        public void PrintHelp() { }
    }

    [Command("dupe-aliases", Aliases = ["x", "X"])]
    private class DuplicateInternalAliasesCommand : ICliCommand
    {
        public Task<int> ExecuteAsync(string[] args) => Task.FromResult(0);
        public void PrintHelp() { }
    }

    [Command("blank-alias", Aliases = ["   "])]
    private class BlankAliasCommand : ICliCommand
    {
        public Task<int> ExecuteAsync(string[] args) => Task.FromResult(0);
        public void PrintHelp() { }
    }

    [Command(Description = "First Default Command")]
    private class FirstDefaultCommand : ICliCommand
    {
        public Task<int> ExecuteAsync(string[] args) => Task.FromResult(0);
        public void PrintHelp() { }
    }

    [Command(Description = "Second Default Command")]
    private class SecondDefaultCommand : ICliCommand
    {
        public Task<int> ExecuteAsync(string[] args) => Task.FromResult(0);
        public void PrintHelp() { }
    }

    [Command(Description = "Default Command with Aliases", Aliases = ["def"])]
    private class DefaultCommandWithAliases : ICliCommand
    {
        public Task<int> ExecuteAsync(string[] args) => Task.FromResult(0);
        public void PrintHelp() { }
    }

    [Fact]
    public void RegisterFromAssembly_RegistersAllExpectedCommands()
    {
        var dispatcher = new CommandDispatcher();
        dispatcher.RegisterFromAssembly(typeof(DefaultCommand).Assembly);

        Assert.NotNull(dispatcher.DefaultCommand);
        Assert.IsType<DefaultCommand>(dispatcher.DefaultCommand);

        Assert.NotNull(dispatcher.GetCommand("test"));
        Assert.NotNull(dispatcher.GetCommand("t")); // alias
        Assert.NotNull(dispatcher.GetCommand("initialize"));
        Assert.NotNull(dispatcher.GetCommand("init")); // alias
        Assert.NotNull(dispatcher.GetCommand("login"));
        Assert.NotNull(dispatcher.GetCommand("logout"));

        // Default command is returned when query is empty or whitespace
        Assert.Same(dispatcher.DefaultCommand, dispatcher.GetCommand(""));
        Assert.Same(dispatcher.DefaultCommand, dispatcher.GetCommand("   "));
    }

    [Fact]
    public void Register_CommandNameCollision_ThrowsInvalidOperationException()
    {
        var dispatcher = new CommandDispatcher();
        dispatcher.Register(new SampleACommand());

        var ex = Assert.Throws<InvalidOperationException>(() => dispatcher.Register(new CollidingNameCommand()));
        Assert.Contains("SAMPLE-A", ex.Message);
        Assert.Contains("already registered", ex.Message);
    }

    [Fact]
    public void Register_AliasCollidesWithExistingAlias_ThrowsInvalidOperationException()
    {
        var dispatcher = new CommandDispatcher();
        dispatcher.Register(new SampleACommand());

        var ex = Assert.Throws<InvalidOperationException>(() => dispatcher.Register(new CollidingAliasWithAliasCommand()));
        Assert.Contains("sa", ex.Message);
        Assert.Contains("conflicts with an alias", ex.Message);
    }

    [Fact]
    public void Register_NameCollidesWithExistingAlias_ThrowsInvalidOperationException()
    {
        var dispatcher = new CommandDispatcher();
        dispatcher.Register(new SampleACommand());

        var ex = Assert.Throws<InvalidOperationException>(() => dispatcher.Register(new NameMatchesExistingAliasCommand()));
        Assert.Contains("sa", ex.Message);
        Assert.Contains("conflicts with an alias", ex.Message);
    }

    [Fact]
    public void Register_AliasCollidesWithExistingName_ThrowsInvalidOperationException()
    {
        var dispatcher = new CommandDispatcher();
        dispatcher.Register(new SampleACommand());

        var ex = Assert.Throws<InvalidOperationException>(() => dispatcher.Register(new AliasMatchesExistingNameCommand()));
        Assert.Contains("sample-a", ex.Message);
        Assert.Contains("conflicts with the command name", ex.Message);
    }

    [Fact]
    public void Register_AliasMatchesPrimaryName_ThrowsInvalidOperationException()
    {
        var dispatcher = new CommandDispatcher();
        var ex = Assert.Throws<InvalidOperationException>(() => dispatcher.Register(new SelfMatchingAliasCommand()));
        Assert.Contains("matches its primary name", ex.Message);
    }

    [Fact]
    public void Register_DuplicateInternalAliases_ThrowsInvalidOperationException()
    {
        var dispatcher = new CommandDispatcher();
        var ex = Assert.Throws<InvalidOperationException>(() => dispatcher.Register(new DuplicateInternalAliasesCommand()));
        Assert.Contains("duplicate alias", ex.Message);
    }

    [Fact]
    public void Register_BlankAlias_ThrowsInvalidOperationException()
    {
        var dispatcher = new CommandDispatcher();
        var ex = Assert.Throws<InvalidOperationException>(() => dispatcher.Register(new BlankAliasCommand()));
        Assert.Contains("empty or whitespace alias", ex.Message);
    }

    [Fact]
    public void Register_MultipleDefaultCommands_ThrowsInvalidOperationException()
    {
        var dispatcher = new CommandDispatcher();
        dispatcher.Register(new FirstDefaultCommand());

        var ex = Assert.Throws<InvalidOperationException>(() => dispatcher.Register(new SecondDefaultCommand()));
        Assert.Contains("SecondDefaultCommand", ex.Message);
        Assert.Contains("FirstDefaultCommand", ex.Message);
    }

    [Fact]
    public void Register_DefaultCommandWithAliases_ThrowsInvalidOperationException()
    {
        var dispatcher = new CommandDispatcher();
        var ex = Assert.Throws<InvalidOperationException>(() => dispatcher.Register(new DefaultCommandWithAliases()));
        Assert.Contains("cannot have aliases", ex.Message);
    }

    [Fact]
    public async Task DefaultCommand_Execution_HandlesFlagsCorrectly()
    {
        var cmd = new DefaultCommand();

        // Empty args returns 0
        Assert.Equal(0, await cmd.ExecuteAsync(Array.Empty<string>()));

        // -v and --version return 0
        Assert.Equal(0, await cmd.ExecuteAsync(new[] { "-v" }));
        Assert.Equal(0, await cmd.ExecuteAsync(new[] { "--version" }));

        // -h and --help return 0
        Assert.Equal(0, await cmd.ExecuteAsync(new[] { "-h" }));
        Assert.Equal(0, await cmd.ExecuteAsync(new[] { "--help" }));

        // Unknown option returns 1
        Assert.Equal(1, await cmd.ExecuteAsync(new[] { "--unknown-flag" }));
    }

    [Fact]
    public async Task Dispatcher_ResolvesAliases_CaseInsensitively()
    {
        var dispatcher = new CommandDispatcher();
        dispatcher.Register(new SampleACommand());

        // By primary name
        Assert.NotNull(dispatcher.GetCommand("SAMPLE-A"));
        Assert.Equal(0, await dispatcher.DispatchAsync(new[] { "SAMPLE-A" }));

        // By alias
        Assert.NotNull(dispatcher.GetCommand("SA"));
        Assert.Equal(0, await dispatcher.DispatchAsync(new[] { "SA" }));

        Assert.NotNull(dispatcher.GetCommand("a"));
        Assert.Equal(0, await dispatcher.DispatchAsync(new[] { "a" }));
    }

    [Fact]
    public async Task Dispatcher_CustomApplicationName_UsedInErrorMessagesAndHelp()
    {
        var writer = new System.IO.StringWriter();
        var console = Spectre.Console.AnsiConsole.Create(new Spectre.Console.AnsiConsoleSettings
        {
            Out = new Spectre.Console.AnsiConsoleOutput(writer)
        });

        var dispatcher = new CommandDispatcher(console, applicationName: "mycli");
        Assert.Equal("mycli", dispatcher.ApplicationName);

        int exitCode = await dispatcher.DispatchAsync(new[] { "unknown-cmd" });
        Assert.Equal(1, exitCode);
        Assert.Contains("Run 'mycli --help'", writer.ToString());

        writer.GetStringBuilder().Clear();
        dispatcher.PrintGlobalHelp();
        Assert.Contains("Usage: mycli <command>", writer.ToString());
    }

    [Fact]
    public void Dispatcher_RegisterFromAssembly_InfersApplicationName()
    {
        var dispatcher = new CommandDispatcher();
        dispatcher.RegisterFromAssembly(typeof(VitaCernita.Cli.Program).Assembly);

        Assert.Equal("vitacernita", dispatcher.ApplicationName);
    }
}
