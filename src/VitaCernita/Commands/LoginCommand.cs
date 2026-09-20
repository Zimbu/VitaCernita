using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Spectre.Console;
using VitaCernita.Core.Api.Auth;
using VitaCernita.Core.Configuration;
using VitaCernita.Cli.Engine;
using VitaCernita.Cli.Engine.Binding;

namespace VitaCernita.Cli.Commands;

/// <summary>
/// Authenticates the user with Google OAuth 2.0 and securely caches tokens in the configuration directory.
/// </summary>
[Command("login", Description = "Authenticate with Google OAuth 2.0 to access the Gmail API")]
public class LoginCommand : ICliCommand
{
    private readonly IAnsiConsole _console;
    private readonly GoogleOAuthTokenProvider? _tokenProviderOverride;

    public LoginCommand(IAnsiConsole? console = null, GoogleOAuthTokenProvider? tokenProviderOverride = null)
    {
        _console = console ?? AnsiConsole.Console;
        _tokenProviderOverride = tokenProviderOverride;
    }

    [Option("credentials", 'c', Description = "Path to Google Cloud client_secret_xxx.json", ValueHelp = "<file>")]
    public string? CredentialsFile { get; set; }

    [Option("client-id", Description = "Google OAuth Client ID", ValueHelp = "<id>")]
    public string? ClientId { get; set; }

    [Option("client-secret", Description = "Google OAuth Client Secret", ValueHelp = "<sec>")]
    public string? ClientSecret { get; set; }

    [Option("account", 'a', Description = "Target Gmail account email address", ValueHelp = "<email>")]
    public string? Account { get; set; }

    [Option("user", 'u', Description = "Target user ID for token storage (default: 'user')", ValueHelp = "<userId>")]
    public string? UserId { get; set; }

    [Option("config-dir", Description = "Custom configuration directory (default: ~/.config/vitacernita)", ValueHelp = "<dir>")]
    public string? ConfigDir { get; set; }

    [Option("save-only", Aliases = ["credentials-only"], Description = "Only import/save client credentials without initiating OAuth login")]
    public bool SaveCredentialsOnly { get; set; }

    [Option("force", 'f', Description = "Force re-authentication even if valid cached token exists")]
    public bool Force { get; set; }

    public async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length > 0)
        {
            var bindResult = CommandParameterBinder.Default.Bind(this, args);
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
        string effectiveUser = !string.IsNullOrWhiteSpace(UserId)
            ? UserId
            : (!string.IsNullOrWhiteSpace(Account) ? Account : "user");

        // 1. Resolve client credentials
        ClientCredentials? credentials = null;

        if (!string.IsNullOrWhiteSpace(CredentialsFile))
        {
            try
            {
                credentials = await ClientCredentialsManager.LoadCredentialsAsync(CredentialsFile, configDir);
            }
            catch (Exception ex)
            {
                _console.MarkupLine($"[bold red]Error:[/] {Markup.Escape(ex.Message)}");
                return 1;
            }

            if (credentials == null || !credentials.IsValid)
            {
                _console.MarkupLine($"[bold red]Error:[/] Could not parse valid client credentials from '[yellow]{Markup.Escape(CredentialsFile)}[/]'.");
                return 1;
            }
            await ClientCredentialsManager.SaveCredentialsAsync(credentials, configDir: configDir);
            _console.MarkupLine($"[bold green]Imported credentials from:[/] [cyan]{Markup.Escape(CredentialsFile)}[/]");
        }
        else if (!string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret))
        {
            credentials = new ClientCredentials(ClientId.Trim(), ClientSecret.Trim());
            await ClientCredentialsManager.SaveCredentialsAsync(credentials, configDir: configDir);
            _console.MarkupLine("[bold green]Client credentials saved successfully.[/]");
        }
        else
        {
            credentials = await ClientCredentialsManager.LoadCredentialsAsync(configDir: configDir);
        }

        // 2. If credentials still not found, prompt interactively if possible
        if (credentials == null || !credentials.IsValid)
        {
            if (Console.IsInputRedirected)
            {
                _console.MarkupLine("[bold red]Error:[/] Google OAuth Client credentials not found.");
                _console.MarkupLine("Provide [bold]--credentials <file.json>[/] or [bold]--client-id[/] and [bold]--client-secret[/].");
                _console.MarkupLine("See docs/auth/README.md for setup instructions.");
                return 1;
            }

            _console.MarkupLine("[bold yellow]Google OAuth Client credentials not found.[/]");
            _console.MarkupLine("Please enter your OAuth Client ID and Secret (or run with --credentials <file.json>):\n");

            string inputId = _console.Prompt(new TextPrompt<string>("Enter [cyan]OAuth Client ID[/]:").PromptStyle("green"));
            string inputSecret = _console.Prompt(new TextPrompt<string>("Enter [cyan]OAuth Client Secret[/]:").PromptStyle("green").Secret());

            if (string.IsNullOrWhiteSpace(inputId) || string.IsNullOrWhiteSpace(inputSecret))
            {
                _console.MarkupLine("[bold red]Error:[/] Client ID and Client Secret cannot be empty.");
                return 1;
            }

            credentials = new ClientCredentials(inputId.Trim(), inputSecret.Trim());
            await ClientCredentialsManager.SaveCredentialsAsync(credentials, configDir: configDir);
            _console.MarkupLine($"[bold green]Client credentials saved to:[/] [yellow]{Markup.Escape(ConfigPathResolver.GetCredentialsPath(configDir))}[/]\n");
        }

        if (SaveCredentialsOnly)
        {
            _console.MarkupLine("[bold green]Client credentials saved successfully. Skipping interactive login as requested.[/]");
            return 0;
        }

        // 3. Check for existing cached token
        var provider = _tokenProviderOverride ?? new GoogleOAuthTokenProvider(
            credentials: credentials,
            configDir: configDir,
            user: effectiveUser);

        if (!Force && provider.HasCachedToken())
        {
            try
            {
                string? existingToken = await provider.GetAccessTokenAsync();
                if (!string.IsNullOrEmpty(existingToken))
                {
                    _console.MarkupLine($"[bold green]Already authenticated as [cyan]{Markup.Escape(effectiveUser)}[/].[/]");
                    _console.MarkupLine($"Token storage: [yellow]{Markup.Escape(ConfigPathResolver.GetTokenStorageDirectory(configDir))}[/]");
                    _console.MarkupLine("[dim]Use --force to re-authenticate or switch accounts.[/]");
                    return 0;
                }
            }
            catch
            {
                // Expired or invalid, proceed to re-auth
            }
        }

        // Never open an interactive browser in automated test runners unless explicitly provided a mock
        if (ConfigPathResolver.IsTestEnvironment && _tokenProviderOverride == null)
        {
            _console.MarkupLine("[dim]Test environment detected: skipping interactive browser launch.[/]");
            return 0;
        }

        // 4. Run authorization flow
        _console.MarkupLine($"Initiating OAuth 2.0 authorization for [cyan]{Markup.Escape(effectiveUser)}[/]...");
        _console.MarkupLine("[dim]Opening browser window for Google authentication. Complete the sign-in prompt to continue...[/]\n");

        try
        {
            var credential = await provider.GetUserCredentialAsync();
            string? token = await credential.GetAccessTokenForRequestAsync();

            if (!string.IsNullOrEmpty(token))
            {
                _console.MarkupLine($"[bold green]Successfully authenticated as [cyan]{Markup.Escape(effectiveUser)}[/]![/]");
                _console.MarkupLine($"Tokens securely cached at: [yellow]{Markup.Escape(ConfigPathResolver.GetTokenStorageDirectory(configDir))}[/]");
                return 0;
            }

            _console.MarkupLine("[bold red]Error:[/] Failed to obtain access token from Google.");
            return 1;
        }
        catch (Exception ex)
        {
            _console.MarkupLine($"[bold red]Authentication failed:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    public void PrintHelp() => CommandHelpRenderer.Render(this, _console);
}
