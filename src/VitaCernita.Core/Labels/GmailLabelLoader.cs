using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lua;
using VitaCernita.Core.Filters;

namespace VitaCernita.Core.Labels;

/// <summary>
/// Dedicated loader for compiling and evaluating Gmail labels from Lua scripts and files.
/// </summary>
public sealed class GmailLabelLoader
{
    private readonly GmailFilterLoader _filterLoader = new();

    public Task<GmailLabel> LoadLabelFromScriptAsync(string script, LuaState? externalState = null)
        => _filterLoader.LoadLabelFromScriptAsync(script, externalState);

    public Task<GmailLabel> LoadLabelFromFileAsync(string filePath, LuaState? externalState = null)
        => _filterLoader.LoadLabelFromFileAsync(filePath, externalState);

    public Task<List<GmailLabel>> LoadLabelsFromScriptAsync(string script, LuaState? externalState = null)
        => _filterLoader.LoadLabelsFromScriptAsync(script, externalState);

    public Task<List<GmailLabel>> LoadLabelsFromFileAsync(string filePath, LuaState? externalState = null)
        => _filterLoader.LoadLabelsFromFileAsync(filePath, externalState);
}
