using System;
using System.IO;
using System.Threading.Tasks;
using VitaCernita.Cli;
using VitaCernita.Cli.Commands;
using VitaCernita.Cli.Engine;
using Xunit;

namespace VitaCernita.Tests.Cli.Commands;

public class CliSubcommandTests
{
    private static string CreateTempConfigFile()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"vitacernita_test_{Guid.NewGuid():N}.lua");
        string lua = @"
return {
    labels = {
        label { name = 'TestLabel', message_list_visibility = 'show' }
    },
    filters = {
        filter {
            id = 'test-001',
            query = From('test@example.com'),
            action = actions(star)
        }
    }
}
";
        File.WriteAllText(tempFile, lua);
        return tempFile;
    }

    [Fact]
    public void DefaultDispatcher_HasTestCommandRegistered()
    {
        var dispatcher = Program.CreateDefaultDispatcher();
        var testCmd = dispatcher.GetCommand("test");

        Assert.NotNull(testCmd);
        Assert.Equal("test", testCmd.Name);
    }

    [Theory]
    [InlineData("-v")]
    [InlineData("--version")]
    public async Task Dispatcher_VersionFlags_ReturnZero(string flag)
    {
        var dispatcher = Program.CreateDefaultDispatcher();
        int exitCode = await dispatcher.DispatchAsync(new[] { flag });

        Assert.Equal(0, exitCode);
    }

    [Theory]
    [InlineData("-h")]
    [InlineData("--help")]
    [InlineData("help")]
    public async Task Dispatcher_HelpFlags_ReturnZero(string flag)
    {
        var dispatcher = Program.CreateDefaultDispatcher();
        int exitCode = await dispatcher.DispatchAsync(new[] { flag });

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Dispatcher_HelpSubcommand_ReturnZero()
    {
        var dispatcher = Program.CreateDefaultDispatcher();
        int exitCode = await dispatcher.DispatchAsync(new[] { "help", "test" });

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Dispatcher_UnknownCommand_ReturnsOne()
    {
        var dispatcher = Program.CreateDefaultDispatcher();
        int exitCode = await dispatcher.DispatchAsync(new[] { "unknown-nonexistent-command" });

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Dispatcher_TestCommand_ExplicitWithMockAndDiff_ReturnsZero()
    {
        string tempConfig = CreateTempConfigFile();
        try
        {
            var dispatcher = Program.CreateDefaultDispatcher();
            int exitCode = await dispatcher.DispatchAsync(new[]
            {
                "test",
                "--config", tempConfig,
                "--mock",
                "--diff"
            });

            Assert.Equal(0, exitCode);
        }
        finally
        {
            if (File.Exists(tempConfig)) File.Delete(tempConfig);
        }
    }

    [Fact]
    public async Task Dispatcher_EmptyArgs_InvokesDefaultCommand_ReturnsZero()
    {
        var dispatcher = Program.CreateDefaultDispatcher();
        int exitCode = await dispatcher.DispatchAsync(Array.Empty<string>());

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Dispatcher_InvocationWithoutSubcommand_FlagsRoutedToDefaultCommand_ReturnsOne()
    {
        string tempConfig = CreateTempConfigFile();
        try
        {
            var dispatcher = Program.CreateDefaultDispatcher();
            int exitCode = await dispatcher.DispatchAsync(new[]
            {
                "--config", tempConfig,
                "--mock",
                "--diff"
            });

            Assert.Equal(1, exitCode);
        }
        finally
        {
            if (File.Exists(tempConfig)) File.Delete(tempConfig);
        }
    }

    [Fact]
    public async Task Dispatcher_TestCommand_NonExistentConfigFile_ReturnsOne()
    {
        var dispatcher = Program.CreateDefaultDispatcher();
        int exitCode = await dispatcher.DispatchAsync(new[]
        {
            "test",
            "--config", "non_existent_file_definitely_not_here.lua"
        });

        Assert.Equal(1, exitCode);
    }
}
