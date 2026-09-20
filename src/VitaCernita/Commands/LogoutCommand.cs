using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;
using VitaCernita.Cli.Engine;
using VitaCernita.Cli.Engine.Binding;
using VitaCernita.Core.Configuration;

namespace VitaCernita.Cli.Commands;

/// <summary>
/// Clears cached OAuth 2.0 tokens and optionally client credentials from the configuration directory.
/// </summary>
[Command("logout", Description = "Log out and remove cached Google OAuth tokens")]
public class LogoutCommand : ICliCommand
{
    private readonly IAnsiConsole _console;

    public LogoutCommand(IAnsiConsole? console = null)
    {
        _console = console ?? AnsiConsole.Console;
    }

    [Option("account", 'a', Description = "Specific account email to log out", ValueHelp = "<email>")]
    public string? Account { get; set; }

    [Option("user", 'u', Description = "Specific user ID to log out", ValueHelp = "<userId>")]
    public string? UserId { get; set; }

    [Option("config-dir", Description = "Custom configuration directory (default: ~/.config/vitacernita)", ValueHelp = "<dir>")]
    public string? ConfigDir { get; set; }

    [Option("all", Description = "Also remove cached client credentials (credentials.json)")]
    public bool All { get; set; }

    public Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length > 0)
        {
            var bindResult = CommandParameterBinder.Default.Bind(this, args);
            if (bindResult.HelpRequested)
            {
                PrintHelp();
                return Task.FromResult(0);
            }
            if (!bindResult.IsSuccess)
            {
                _console.MarkupLine($"[bold red]Error:[/] {bindResult.ErrorMessage}");
                return Task.FromResult(1);
            }
        }

        string configDir = ConfigPathResolver.GetDefaultConfigDirectory(customConfigDir: ConfigDir);
        string tokenDir = ConfigPathResolver.GetTokenStorageDirectory(configDir);
        string targetUser = !string.IsNullOrWhiteSpace(UserId)
            ? UserId
            : (!string.IsNullOrWhiteSpace(Account) ? Account : string.Empty);

        if (!Directory.Exists(tokenDir))
        {
            _console.MarkupLine($"[yellow]Warning:[/] Token directory not found: [dim]{tokenDir}[/]. No tokens to remove.");
        }
        else
        {
            var tokenFiles = Directory.GetFiles(tokenDir);
            if (tokenFiles.Length == 0)
            {
                _console.MarkupLine($"[yellow]Note:[/] No cached tokens found in [dim]{tokenDir}[/].");
            }
            else
            {
                var filesToDelete = string.IsNullOrWhiteSpace(targetUser)
                    ? tokenFiles
                    : tokenFiles.Where(f => Path.GetFileName(f).Contains(targetUser, StringComparison.OrdinalIgnoreCase)).ToArray();

                if (filesToDelete.Length == 0)
                {
                    _console.MarkupLine($"[yellow]Note:[/] No tokens found matching user '[cyan]{targetUser}[/]'.");
                }
                else
                {
                    foreach (var file in filesToDelete)
                    {
                        File.Delete(file);
                        _console.MarkupLine($"[green]Deleted token:[/] [dim]{Path.GetFileName(file)}[/]");
                    }
                    _console.MarkupLine($"[bold green]Success:[/] Removed {filesToDelete.Length} token file(s).");
                }
            }
        }

        if (All)
        {
            string credsFile = ConfigPathResolver.GetCredentialsPath(configDir);
            if (File.Exists(credsFile))
            {
                File.Delete(credsFile);
                _console.MarkupLine($"[green]Deleted credentials file:[/] [dim]{credsFile}[/]");
            }
            else
            {
                _console.MarkupLine($"[dim]Credentials file not found: {credsFile}[/]");
            }
        }

        return Task.FromResult(0);
    }

    public void PrintHelp() => CommandHelpRenderer.Render(this, _console);
}
