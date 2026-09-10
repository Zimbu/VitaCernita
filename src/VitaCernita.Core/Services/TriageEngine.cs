using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lua;
using VitaCernita.Core.Configuration;
using VitaCernita.Core.Models;

namespace VitaCernita.Core.Services;

public sealed class TriageEngine
{
    private readonly AppConfig _config;
    private readonly LuaState _state;

    public TriageEngine(AppConfig config, LuaState? state = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _state = state ?? LuaState.Create();
    }

    public async Task<TriageItem> ProcessItemAsync(TriageItem item)
    {
        // 1. Calculate score via Lua hook if defined, or default formula
        if (_config.RawTable != null &&
            _config.RawTable.TryGetValue("calculate_score", out var scoreVal) &&
            scoreVal.Type == LuaValueType.Function)
        {
            var fn = scoreVal.Read<LuaFunction>();
            var results = await _state.CallAsync(fn, [item.Urgency, item.Effort]);
            if (results.Length > 0 && results[0].Type == LuaValueType.Number)
            {
                item.Score = results[0].Read<double>();
            }
        }
        else
        {
            // Default formula: score = urgency * 2 - effort
            item.Score = (item.Urgency * 2.0) - item.Effort;
        }

        // 2. Determine category via active rules
        var matchingRule = _config.Rules
            .Where(r => r.Enabled)
            .OrderByDescending(r => r.Priority)
            .FirstOrDefault();

        if (matchingRule != null)
        {
            item.Category = matchingRule.Category;
        }

        return item;
    }
}
