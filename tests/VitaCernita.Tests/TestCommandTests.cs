using System;
using System.IO;
using System.Threading.Tasks;
using VitaCernita.Cli;
using VitaCernita.Cli.Commands;
using Xunit;

namespace VitaCernita.Tests;

public class TestCommandTests
{
    private static string GetTempConfigPath()
    {
        return Path.Combine(Path.GetTempPath(), $"vitacernita_testcmd_{Guid.NewGuid():N}.lua");
    }

    [Fact]
    public async Task TestCommand_NonExistentConfig_ReturnsErrorCode()
    {
        var cmd = new TestCommand();
        string nonExistent = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.lua");
        int exitCode = await cmd.ExecuteAsync(new[] { "--config", nonExistent });

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task TestCommand_ValidConfig_ReturnsZero()
    {
        string tempPath = GetTempConfigPath();
        try
        {
            await File.WriteAllTextAsync(tempPath, @"
return {
    labels = {
        { name = ""Receipts"" }
    },
    filters = {},
    auto_reply = { enable = false }
}
");
            var cmd = new TestCommand();
            int exitCode = await cmd.ExecuteAsync(new[] { "--config", tempPath });

            Assert.Equal(0, exitCode);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task TestCommand_WithMockDiff_ReturnsZero()
    {
        string tempPath = GetTempConfigPath();
        try
        {
            await File.WriteAllTextAsync(tempPath, @"
return {
    labels = {
        { name = ""Receipts"" }
    },
    filters = {},
    auto_reply = { enable = false }
}
");
            var cmd = new TestCommand();
            int exitCode = await cmd.ExecuteAsync(new[]
            {
                "--config", tempPath,
                "--diff",
                "--mock"
            });

            Assert.Equal(0, exitCode);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task TestCommand_WithClientIdToken_ReturnsErrorCode()
    {
        string tempPath = GetTempConfigPath();
        try
        {
            await File.WriteAllTextAsync(tempPath, @"
return {
    labels = {},
    filters = {},
    auto_reply = { enable = false }
}
");
            var cmd = new TestCommand();
            int exitCode = await cmd.ExecuteAsync(new[]
            {
                "--config", tempPath,
                "--diff",
                "--token", "123456.apps.googleusercontent.com"
            });

            Assert.Equal(1, exitCode);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task TestCommand_WithClientIdInEnvVar_ReturnsErrorCode()
    {
        string tempPath = GetTempConfigPath();
        string? originalToken = Environment.GetEnvironmentVariable("GMAIL_ACCESS_TOKEN");
        try
        {
            Environment.SetEnvironmentVariable("GMAIL_ACCESS_TOKEN", "123456.apps.googleusercontent.com");
            await File.WriteAllTextAsync(tempPath, @"
return {
    labels = {},
    filters = {},
    auto_reply = { enable = false }
}
");
            var cmd = new TestCommand();
            int exitCode = await cmd.ExecuteAsync(new[]
            {
                "--config", tempPath,
                "--diff"
            });

            Assert.Equal(1, exitCode);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GMAIL_ACCESS_TOKEN", originalToken);
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Theory]
    [InlineData("test")]
    public async Task Dispatcher_RoutesTestCommand(string command)
    {
        var dispatcher = Program.CreateDefaultDispatcher();
        int exitCode = await dispatcher.DispatchAsync(new[] { command, "--help" });
        Assert.Equal(0, exitCode);
    }
}
