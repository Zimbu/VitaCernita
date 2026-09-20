using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Spectre.Console;

namespace VitaCernita.Cli.Commands.Binding;

/// <summary>
/// Renders standard CLI help and options tables dynamically for any <see cref="ICliCommand"/>
/// decorated with <see cref="OptionAttribute"/> properties.
/// </summary>
public static class CommandHelpRenderer
{
    public static void Render(ICliCommand command, IAnsiConsole console, string? applicationName = null)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(console);

        string appName = applicationName ?? CommandDispatcher.ResolveApplicationName(command.GetType().Assembly);
        string commandName = command.Name;
        string title = !string.IsNullOrWhiteSpace(commandName)
            ? $"{CultureInfo.InvariantCulture.TextInfo.ToTitleCase(commandName)} Command"
            : "Command";

        console.MarkupLine($"[bold]{appName} - {title}[/]");
        console.MarkupLine($"Usage: {appName} {commandName} [[options]]\n");

        if (!string.IsNullOrWhiteSpace(command.Description))
        {
            console.MarkupLine("[bold]Description:[/]");
            console.MarkupLine($"  {command.Description}\n");
        }

        var descriptors = CommandParameterBinder.GetDescriptors(command.GetType());
        if (descriptors.Count == 0)
        {
            console.MarkupLine("[bold]Options:[/]");
            console.MarkupLine("  -h, --help              Show this help message");
            return;
        }

        console.MarkupLine("[bold]Options:[/]");

        var optionLines = descriptors.Select(d =>
        {
            var sb = new StringBuilder();
            if (d.ShortName != '\0')
            {
                sb.Append($"-{d.ShortName}, --{d.EffectiveName}");
            }
            else
            {
                sb.Append($"    --{d.EffectiveName}");
            }

            foreach (var alias in d.Aliases)
            {
                sb.Append($", --{alias}");
            }

            if (!d.IsSwitch)
            {
                string helpPlaceholder = !string.IsNullOrWhiteSpace(d.ValueHelp)
                    ? d.ValueHelp
                    : $"<{d.EffectiveName}>";
                sb.Append($" {helpPlaceholder}");
            }

            string prefix = sb.ToString();

            var descSb = new StringBuilder(d.Description);
            if (d.Required)
            {
                descSb.Append(" [bold red](required)[/]");
            }
            if (d.DefaultValue != null && !d.IsSwitch && !(d.DefaultValue is string s && string.IsNullOrEmpty(s)))
            {
                descSb.Append($" (default: '{d.DefaultValue}')");
            }

            return (Prefix: prefix, Description: descSb.ToString());
        }).ToList();

        int maxPrefixLen = Math.Max(optionLines.Max(o => o.Prefix.Length), "-h, --help".Length) + 2;

        foreach (var line in optionLines)
        {
            console.MarkupLine($"  [cyan]{line.Prefix.PadRight(maxPrefixLen)}[/] {line.Description}");
        }

        console.MarkupLine($"  [cyan]{"-h, --help".PadRight(maxPrefixLen)}[/] Show this help message");
    }
}
