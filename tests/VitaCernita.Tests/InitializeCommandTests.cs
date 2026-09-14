using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VitaCernita.Cli;
using VitaCernita.Cli.Commands;
using VitaCernita.Core.Actions;
using VitaCernita.Core.Api.Fakes;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Queries;
using Xunit;
using AutoReplyModel = VitaCernita.Core.AutoReply.AutoReply;

namespace VitaCernita.Tests;

public class InitializeCommandTests
{
    private static string GetTempConfigPath()
    {
        return Path.Combine(Path.GetTempPath(), $"vitacernita_init_test_{Guid.NewGuid():N}.lua");
    }

    [Fact]
    public async Task Initialize_GeneratesDefaultConfigurationFile_WhenNoneExists()
    {
        string tempPath = GetTempConfigPath();
        try
        {
            var cmd = new InitializeCommand();
            int exitCode = await cmd.ExecuteAsync(new[] { "--output", tempPath });

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(tempPath));

            var loader = new GmailFilterLoader();
            var config = await loader.LoadConfigurationFromFileAsync(tempPath);

            Assert.NotEmpty(config.Labels);
            Assert.NotEmpty(config.Filters);
            Assert.NotNull(config.AutoReply);
            Assert.False(config.AutoReply.EnableAutoReply);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task Initialize_FailsWhenFileExistsWithoutForce()
    {
        string tempPath = GetTempConfigPath();
        try
        {
            await File.WriteAllTextAsync(tempPath, "-- existing content");

            var cmd = new InitializeCommand();
            int exitCode = await cmd.ExecuteAsync(new[] { "--output", tempPath });

            Assert.Equal(1, exitCode);
            Assert.Equal("-- existing content", await File.ReadAllTextAsync(tempPath));
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task Initialize_OverwritesWhenForceProvided()
    {
        string tempPath = GetTempConfigPath();
        try
        {
            await File.WriteAllTextAsync(tempPath, "-- existing content");

            var cmd = new InitializeCommand();
            int exitCode = await cmd.ExecuteAsync(new[] { "--output", tempPath, "--force" });

            Assert.Equal(0, exitCode);
            string newContent = await File.ReadAllTextAsync(tempPath);
            Assert.NotEqual("-- existing content", newContent);
            Assert.Contains("return {", newContent);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task Initialize_FromMockAccount_GeneratesValidLuaConfig()
    {
        string tempPath = GetTempConfigPath();
        try
        {
            var cmd = new InitializeCommand();
            int exitCode = await cmd.ExecuteAsync(new[] { "--output", tempPath, "--mock" });

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(tempPath));

            var loader = new GmailFilterLoader();
            var config = await loader.LoadConfigurationFromFileAsync(tempPath);

            Assert.Equal(2, config.Labels.Count);
            Assert.Contains(config.Labels, l => l.Name == "Receipts");
            Assert.Contains(config.Labels, l => l.Name == "Work");

            Assert.Equal(2, config.Filters.Count);
            Assert.NotNull(config.AutoReply);
            Assert.True(config.AutoReply.EnableAutoReply);
            Assert.Equal("Out of Office", config.AutoReply.ResponseSubject);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task Initialize_WithInjectedApiClient_ReadsAndSerializesAccountData()
    {
        string tempPath = GetTempConfigPath();
        try
        {
            var fakeClient = new FakeGmailApiClient();
            fakeClient.AddLabel(new GmailLabel("CustomLabel1", id: "L1", messageListVisibility: "show"));
            fakeClient.AddLabel(new GmailLabel("CustomLabel2", id: "L2", messageListVisibility: "hide"));
            fakeClient.AddFilter(new GmailFilter(
                id: "filter-01",
                query: new FieldCondition("from", "alerts@custom.org"),
                action: new GmailAction().Star().MarkImportant(),
                name: "Custom Alert Filter"));
            fakeClient.SetAutoReply(new AutoReplyModel
            {
                EnableAutoReply = true,
                ResponseSubject = "Custom Vacation Responder",
                ResponseBodyPlainText = "Out on PTO",
                RestrictToContacts = true
            }, "user@custom.org");

            var cmd = new InitializeCommand(apiClientOverride: fakeClient);
            int exitCode = await cmd.ExecuteAsync(new[]
            {
                "--output", tempPath,
                "--account", "user@custom.org"
            });

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(tempPath));

            var loader = new GmailFilterLoader();
            var config = await loader.LoadConfigurationFromFileAsync(tempPath);

            Assert.Equal(2, config.Labels.Count);
            Assert.Contains(config.Labels, l => l.Name == "CustomLabel1");
            Assert.Contains(config.Labels, l => l.Name == "CustomLabel2");

            Assert.Single(config.Filters);
            Assert.Equal("Custom Alert Filter", config.Filters[0].Name);

            Assert.NotNull(config.AutoReply);
            Assert.Equal("Custom Vacation Responder", config.AutoReply.ResponseSubject);
            Assert.Equal("Out on PTO", config.AutoReply.ResponseBodyPlainText);
            Assert.True(config.AutoReply.RestrictToContacts);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task Initialize_WithoutTokenOrEnvVar_ReturnsErrorCode()
    {
        string tempPath = GetTempConfigPath();
        string? originalToken = Environment.GetEnvironmentVariable("GMAIL_ACCESS_TOKEN");
        try
        {
            Environment.SetEnvironmentVariable("GMAIL_ACCESS_TOKEN", null);

            var cmd = new InitializeCommand();
            int exitCode = await cmd.ExecuteAsync(new[]
            {
                "--output", tempPath,
                "--account", "user@example.com"
            });

            Assert.Equal(1, exitCode);
            Assert.False(File.Exists(tempPath));
        }
        finally
        {
            Environment.SetEnvironmentVariable("GMAIL_ACCESS_TOKEN", originalToken);
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Theory]
    [InlineData("initialize")]
    [InlineData("init")]
    public async Task Dispatcher_RoutesInitializeAndInitAliases(string subcommand)
    {
        string tempPath = GetTempConfigPath();
        try
        {
            var dispatcher = Program.CreateDefaultDispatcher();
            int exitCode = await dispatcher.DispatchAsync(new[]
            {
                subcommand,
                "--output", tempPath,
                "--mock"
            });

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(tempPath));
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task Dispatcher_HelpForInitialize_ReturnsZero()
    {
        var dispatcher = Program.CreateDefaultDispatcher();
        int exitCode1 = await dispatcher.DispatchAsync(new[] { "initialize", "--help" });
        int exitCode2 = await dispatcher.DispatchAsync(new[] { "help", "init" });

        Assert.Equal(0, exitCode1);
        Assert.Equal(0, exitCode2);
    }

    [Fact]
    public async Task Initialize_WithClientIdInTokenArg_ReturnsErrorCode()
    {
        string tempPath = GetTempConfigPath();
        try
        {
            var cmd = new InitializeCommand();
            int exitCode = await cmd.ExecuteAsync(new[]
            {
                "--output", tempPath,
                "--account", "user@example.com",
                "--token", "12345-fake.apps.googleusercontent.com"
            });

            Assert.Equal(1, exitCode);
            Assert.False(File.Exists(tempPath));
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task Initialize_WithClientIdInEnvVar_ReturnsErrorCode()
    {
        string tempPath = GetTempConfigPath();
        string? originalToken = Environment.GetEnvironmentVariable("GMAIL_ACCESS_TOKEN");
        try
        {
            Environment.SetEnvironmentVariable("GMAIL_ACCESS_TOKEN", "12345-fake.apps.googleusercontent.com");

            var cmd = new InitializeCommand();
            int exitCode = await cmd.ExecuteAsync(new[]
            {
                "--output", tempPath,
                "--account", "user@example.com"
            });

            Assert.Equal(1, exitCode);
            Assert.False(File.Exists(tempPath));
        }
        finally
        {
            Environment.SetEnvironmentVariable("GMAIL_ACCESS_TOKEN", originalToken);
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }
}
