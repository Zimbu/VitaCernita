using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Spectre.Console;
using VitaCernita.Core.Actions;
using VitaCernita.Core.Api;
using VitaCernita.Core.Api.Auth;
using VitaCernita.Core.Api.Fakes;
using VitaCernita.Core.Configuration;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Queries;
using VitaCernita.Core.Serialization;
using VitaCernita.Core.Sources;
using AutoReplyModel = VitaCernita.Core.AutoReply.AutoReply;

namespace VitaCernita.Cli.Commands;

/// <summary>
/// CLI command that initializes a VitaCernita Lua configuration file.
/// If connecting to an existing Gmail account, reads vacation responder, labels, and filters,
/// serializing the result to the target configuration file using the Functional DSL.
/// If no account is specified and the file does not exist, generates a starter default configuration.
/// </summary>
[Command("initialize", Description = "Initialize a VitaCernita configuration file from scratch or an existing Gmail account", Aliases = ["init"])]
public class InitializeCommand : ICliCommand
{
    private readonly IAnsiConsole _console;
    private readonly IGmailApiClient? _apiClientOverride;

    public InitializeCommand(IAnsiConsole? console = null, IGmailApiClient? apiClientOverride = null)
    {
        _console = console ?? AnsiConsole.Console;
        _apiClientOverride = apiClientOverride;
    }
    [Option("output", 'o', Aliases = ["config"], Description = "Destination path for configuration (default: ~/.config/vitacernita/gmail.lua)", ValueHelp = "<path>")]
    public string? OutputPath { get; set; }

    [Option("account", 'a', Description = "Target Gmail account email address", ValueHelp = "<email>")]
    public string? Account { get; set; }

    [Option("user", 'u', Description = "Target Gmail user ID (defaults to --account or 'me')", ValueHelp = "<userId>")]
    public string? UserId { get; set; }

    [Option("token", 't', Description = "Bearer token for Gmail API (defaults to GMAIL_ACCESS_TOKEN)", ValueHelp = "<token>")]
    public string? Token { get; set; }

    [Option("config-dir", Description = "Custom configuration directory (default: ~/.config/vitacernita)", ValueHelp = "<dir>")]
    public string? ConfigDir { get; set; }

    [Option("mock", Aliases = ["fake-account"], Description = "Connect to an in-memory mock account (for testing/dry-run)")]
    public bool UseMock { get; set; }

    [Option("force", 'f', Description = "Overwrite destination file if it already exists")]
    public bool Force { get; set; }

    public async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length > 0)
        {
            var bindResult = Binding.CommandParameterBinder.Default.Bind(this, args);
            if (bindResult.HelpRequested)
            {
                PrintHelp();
                return 0;
            }
            if (!bindResult.IsSuccess)
            {
                _console.MarkupLine($"[bold red]Error:[/] {bindResult.ErrorMessage}");
                return 1;
            }
        }

        string configDir = ConfigPathResolver.GetDefaultConfigDirectory(customConfigDir: ConfigDir);

        string targetPath = !string.IsNullOrWhiteSpace(OutputPath)
            ? ConfigPathResolver.ResolveConfigPath(OutputPath, customConfigDir: ConfigDir)
            : ConfigPathResolver.GetDefaultConfigPath(customConfigDir: ConfigDir);

        if (File.Exists(targetPath) && !Force)
        {
            _console.MarkupLine($"[bold red]Error:[/] Configuration file already exists at '[yellow]{Markup.Escape(targetPath)}[/]'. Use [bold]--force[/] to overwrite.");
            return 1;
        }

        bool connectToAccount = UseMock || !string.IsNullOrWhiteSpace(Account) || !string.IsNullOrWhiteSpace(Token) || _apiClientOverride != null;

        if (connectToAccount)
        {
            return await InitializeFromAccountAsync(targetPath, Account, UserId, Token, UseMock, configDir);
        }

        return await InitializeDefaultConfigAsync(targetPath);
    }

    private async Task<int> InitializeFromAccountAsync(
        string targetPath,
        string? account,
        string? userId,
        string? token,
        bool useMock,
        string configDir)
    {
        string effectiveUser = !string.IsNullOrWhiteSpace(userId)
            ? userId
            : (!string.IsNullOrWhiteSpace(account) ? account : "me");

        IGmailApiClient client;

        if (_apiClientOverride != null)
        {
            client = _apiClientOverride;
        }
        else if (useMock)
        {
            _console.MarkupLine("[bold yellow]Mode:[/] In-Memory Fake Gmail Account (Mock)");
            var fake = new FakeGmailApiClient();
            fake.AddLabel(new GmailLabel("Receipts", id: "Label_1", messageListVisibility: "show", labelListVisibility: "labelShow"));
            fake.AddLabel(new GmailLabel("Work", id: "Label_2", messageListVisibility: "show", labelListVisibility: "labelShow"));
            fake.AddFilter(new GmailFilter(
                id: "sec-001",
                query: new FieldCondition("from", "secops@company.com"),
                action: new GmailAction().Star(),
                name: "Security Alerts"));
            fake.AddFilter(new GmailFilter(
                id: "fin-002",
                query: new FieldCondition("from", "billing@vendor.com"),
                action: new GmailAction().Archive().AddCustomLabel("Receipts"),
                name: "Vendor Invoices"));
            fake.SetAutoReply(new AutoReplyModel
            {
                EnableAutoReply = true,
                ResponseSubject = "Out of Office",
                ResponseBodyPlainText = "Thank you for reaching out. I am currently out of the office.",
                RestrictToContacts = true,
                RestrictToDomain = false
            }, effectiveUser);
            client = fake;
        }
        else
        {
            IGmailTokenProvider tokenProvider;

            if (!string.IsNullOrWhiteSpace(token))
            {
                if (token.Contains(".apps.googleusercontent.com", StringComparison.OrdinalIgnoreCase))
                {
                    _console.MarkupLine("[bold red]Error:[/] The provided --token appears to be an OAuth Client ID, not an OAuth 2.0 access token.");
                    _console.MarkupLine("OAuth 2.0 access tokens typically begin with 'ya29.' or are retrieved via [bold cyan]vitacernita login[/].");
                    return 1;
                }
                tokenProvider = new BearerTokenProvider(token);
            }
            else
            {
                var oauthProvider = new GoogleOAuthTokenProvider(configDir: configDir, user: effectiveUser);
                if (oauthProvider.HasCachedToken())
                {
                    _console.MarkupLine($"Using cached Google OAuth token for [cyan]{Markup.Escape(effectiveUser)}[/]...");
                    string? envToken = Environment.GetEnvironmentVariable("GMAIL_ACCESS_TOKEN");
                    if (!string.IsNullOrWhiteSpace(envToken) && envToken.Contains(".apps.googleusercontent.com", StringComparison.OrdinalIgnoreCase))
                    {
                        _console.MarkupLine("[grey](Note: Ignoring GMAIL_ACCESS_TOKEN environment variable because it contains an OAuth Client ID instead of an access token.)[/]");
                    }
                    tokenProvider = oauthProvider;
                }
                else
                {
                    string? envToken = Environment.GetEnvironmentVariable("GMAIL_ACCESS_TOKEN");
                    if (!string.IsNullOrWhiteSpace(envToken))
                    {
                        if (envToken.Contains(".apps.googleusercontent.com", StringComparison.OrdinalIgnoreCase))
                        {
                            _console.MarkupLine("[bold red]Error:[/] The GMAIL_ACCESS_TOKEN environment variable is set to an OAuth Client ID ('...apps.googleusercontent.com'), not an OAuth 2.0 access token.");
                            _console.MarkupLine("OAuth 2.0 access tokens typically begin with 'ya29.'. Run [bold cyan]unset GMAIL_ACCESS_TOKEN[/] in your terminal and authenticate using [bold cyan]vitacernita login[/].");
                            return 1;
                        }

                        tokenProvider = new BearerTokenProvider(envToken);
                    }
                    else
                    {
                        _console.MarkupLine($"[bold red]Error:[/] No active Google login session or access token found for account '[yellow]{Markup.Escape(effectiveUser)}[/]'.");
                        _console.MarkupLine("Run [bold cyan]vitacernita login[/] to authenticate, or provide [bold]--token <token>[/].");
                        return 1;
                    }
                }
            }

            client = new HttpGmailApiClient(new HttpClient(), tokenProvider);
        }

        string apiUserId = !string.IsNullOrWhiteSpace(userId) ? userId : "me";
        var source = new ApiGmailSource(client, apiUserId, name: $"Gmail Account ({effectiveUser})");

        _console.MarkupLine($"Connecting to Gmail account [cyan]{Markup.Escape(effectiveUser)}[/]...");

        try
        {
            var labels = (await source.GetLabelsAsync()).ToList();
            var filters = (await source.GetFiltersAsync()).ToList();
            var autoReply = await source.GetAutoReplyAsync();

            string vacationStatus = autoReply != null && autoReply.EnableAutoReply ? "enabled" : "disabled";
            _console.MarkupLine($"Retrieved [green]{labels.Count}[/] label(s), [green]{filters.Count}[/] filter(s), and auto-reply ({vacationStatus}).");

            var serializerOptions = new LuaSerializerOptions
            {
                HeaderComment =
                    $"-- =======================================================================\n" +
                    $"-- VitaCernita Gmail Configuration\n" +
                    $"-- Initialized from Gmail Account: {effectiveUser}\n" +
                    $"-- Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                    $"-- =======================================================================",
                IndentSpaces = 4,
                KnownLabels = labels
            };

            string luaContent = LuaConfigSerializer.Default.Serialize(labels, filters, autoReply, serializerOptions);

            ConfigPathResolver.EnsureDirectoryExists(targetPath);
            await File.WriteAllTextAsync(targetPath, luaContent);

            _console.MarkupLine($"[bold green]Configuration successfully initialized and saved to:[/] [yellow]{Markup.Escape(targetPath)}[/]");
            return 0;
        }
        catch (Exception ex)
        {
            _console.MarkupLine($"[bold red]Error connecting to Gmail account:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    private async Task<int> InitializeDefaultConfigAsync(string targetPath)
    {
        _console.MarkupLine($"Generating default VitaCernita configuration at: [cyan]{Markup.Escape(targetPath)}[/]...");

        var defaultLabels = new[]
        {
            new GmailLabel("Receipts", messageListVisibility: "show", labelListVisibility: "labelShow"),
            new GmailLabel("Work", messageListVisibility: "show", labelListVisibility: "labelShow")
        };

        var defaultFilters = new[]
        {
            new GmailFilter(
                id: null,
                query: new AndCondition(new IQueryCondition[]
                {
                    new OrCondition(new IQueryCondition[]
                    {
                        new FieldCondition("from", "billing@"),
                        new FieldCondition("from", "invoices@")
                    }),
                    new HasCondition("attachment")
                }),
                action: new GmailAction().Archive().AddCustomLabel("Receipts"),
                name: "Receipts & Billing"
            ),
            new GmailFilter(
                id: null,
                query: new AndCondition(new IQueryCondition[]
                {
                    new FieldCondition("from", "@company.com"),
                    new IsCondition("important")
                }),
                action: new GmailAction().Star(),
                name: "Important Team Communications"
            )
        };

        var defaultAutoReply = new AutoReplyModel
        {
            EnableAutoReply = false,
            ResponseSubject = "Out of Office",
            ResponseBodyPlainText = "Thank you for reaching out. I am currently out of the office and will respond upon my return.",
            RestrictToContacts = false,
            RestrictToDomain = false
        };

        var options = new LuaSerializerOptions
        {
            HeaderComment =
                "-- =======================================================================\n" +
                "-- VitaCernita Gmail Configuration\n" +
                "-- Default configuration generated by 'vitacernita initialize'\n" +
                "-- =======================================================================",
            IndentSpaces = 4
        };

        string luaContent = LuaConfigSerializer.Default.Serialize(defaultLabels, defaultFilters, defaultAutoReply, options);

        ConfigPathResolver.EnsureDirectoryExists(targetPath);
        await File.WriteAllTextAsync(targetPath, luaContent);

        _console.MarkupLine($"[bold green]Configuration successfully created at:[/] [yellow]{Markup.Escape(targetPath)}[/]");
        _console.MarkupLine("[dim]Tip: You can now edit your configuration or run 'vitacernita test --diff' to preview changes.[/]");
        return 0;
    }

    public void PrintHelp() => Binding.CommandHelpRenderer.Render(this, _console);
}
