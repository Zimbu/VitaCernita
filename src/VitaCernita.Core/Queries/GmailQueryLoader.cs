using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Lua;
using VitaCernita.Core.Filters;

namespace VitaCernita.Core.Queries;

/// <summary>
/// Dedicated loader for compiling and evaluating Gmail search queries from Lua scripts and files.
/// </summary>
public sealed class GmailQueryLoader
{
    private readonly GmailFilterLoader _filterLoader = new();

    public Task<IQueryCondition> LoadQueryFromScriptAsync(string script, LuaState? externalState = null)
        => _filterLoader.LoadQueryFromScriptAsync(script, externalState);

    public Task<IQueryCondition> LoadQueryFromFileAsync(string filePath, LuaState? externalState = null)
        => _filterLoader.LoadQueryFromFileAsync(filePath, externalState);

    public Task<List<IQueryCondition>> LoadQueriesFromScriptAsync(string script, LuaState? externalState = null)
        => _filterLoader.LoadQueriesFromScriptAsync(script, externalState);

    public Task<List<IQueryCondition>> LoadQueriesFromFileAsync(string filePath, LuaState? externalState = null)
        => _filterLoader.LoadQueriesFromFileAsync(filePath, externalState);
}
