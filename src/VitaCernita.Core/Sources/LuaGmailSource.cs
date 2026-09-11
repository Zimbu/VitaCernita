using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;

namespace VitaCernita.Core.Sources;

/// <summary>
/// An IGmailSource implementation backed by local Lua configuration files or scripts.
/// Evaluates Lua configurations and exposes them as in-memory IQueryable collections.
/// </summary>
public class LuaGmailSource : IGmailSource
{
    private readonly string? _filePath;
    private readonly string? _script;
    private readonly GmailFilterLoader _loader = new();

    public string Name { get; }

    public LuaGmailSource(string filePath, string? name = null)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        Name = name ?? $"Lua: {filePath}";
    }

    public static LuaGmailSource FromScript(string script, string name = "Lua Script") =>
        new(script, name, isScript: true);

    private LuaGmailSource(string script, string name, bool isScript)
    {
        _script = script ?? throw new ArgumentNullException(nameof(script));
        Name = name;
    }

    public async Task<IQueryable<GmailLabel>> GetLabelsAsync(CancellationToken cancellationToken = default)
    {
        if (_filePath != null)
        {
            var config = await _loader.LoadConfigurationFromFileAsync(_filePath);
            return config.Labels.AsQueryable();
        }

        if (_script != null)
        {
            var config = await _loader.LoadConfigurationFromScriptAsync(_script);
            return config.Labels.AsQueryable();
        }

        return Enumerable.Empty<GmailLabel>().AsQueryable();
    }

    public async Task<IQueryable<GmailFilter>> GetFiltersAsync(CancellationToken cancellationToken = default)
    {
        if (_filePath != null)
        {
            var config = await _loader.LoadConfigurationFromFileAsync(_filePath);
            return config.Filters.AsQueryable();
        }

        if (_script != null)
        {
            var config = await _loader.LoadConfigurationFromScriptAsync(_script);
            return config.Filters.AsQueryable();
        }

        return Enumerable.Empty<GmailFilter>().AsQueryable();
    }
}
