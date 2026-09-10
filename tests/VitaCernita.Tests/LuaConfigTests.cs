using System;
using System.Threading.Tasks;
using Xunit;
using VitaCernita.Core.Configuration;
using VitaCernita.Core.Models;
using VitaCernita.Core.Services;

namespace VitaCernita.Tests;

public class LuaConfigTests
{
    private readonly LuaConfigLoader _loader = new();

    [Fact]
    public async Task LoadFromScript_WithValidTable_ParsesCorrectly()
    {
        string script = @"
return {
    project = {
        name = 'TestProject',
        version = '1.2.3',
        description = 'A test project',
        author = 'Tester'
    },
    settings = {
        log_level = 'Debug',
        max_concurrency = 16,
        output_dir = '/tmp/test',
        dry_run = true,
        timeout_seconds = 60
    },
    custom = {
        environment = 'testing'
    }
}
";

        var config = await _loader.LoadFromScriptAsync(script);

        Assert.Equal("TestProject", config.Project.Name);
        Assert.Equal("1.2.3", config.Project.Version);
        Assert.Equal("A test project", config.Project.Description);
        Assert.Equal("Tester", config.Project.Author);
        Assert.Equal("Debug", config.Settings.LogLevel);
        Assert.Equal(16, config.Settings.MaxConcurrency);
        Assert.Equal("/tmp/test", config.Settings.OutputDirectory);
        Assert.True(config.Settings.DryRun);
        Assert.Equal(60, config.Settings.TimeoutSeconds);
        Assert.Equal("testing", config.CustomProperties["environment"]);
    }

    [Fact]
    public async Task LoadFromScript_WithMissingOptionalFields_UsesDefaults()
    {
        string script = @"return {}";

        var config = await _loader.LoadFromScriptAsync(script);

        Assert.Equal("VitaCernita", config.Project.Name);
        Assert.Equal("0.1.0", config.Project.Version);
        Assert.Equal(4, config.Settings.MaxConcurrency);
        Assert.False(config.Settings.DryRun);
    }

    [Fact]
    public async Task LoadFromScript_WithEnvHelper_ResolvesVariableOrDefault()
    {
        Environment.SetEnvironmentVariable("VITACERNITA_TEST_VAR", "SampleVal");

        string script = @"
return {
    custom = {
        resolved = env('VITACERNITA_TEST_VAR', 'fallback'),
        missing = env('NON_EXISTENT_VAR_XYZ', 'default_fallback')
    }
}
";

        var config = await _loader.LoadFromScriptAsync(script);

        Assert.Equal("SampleVal", config.CustomProperties["resolved"]);
        Assert.Equal("default_fallback", config.CustomProperties["missing"]);
    }

    [Fact]
    public async Task LoadFromScript_WithInvalidSyntax_ThrowsLuaConfigException()
    {
        string invalidScript = "this is not valid lua code !!!";

        await Assert.ThrowsAsync<LuaConfigException>(() => _loader.LoadFromScriptAsync(invalidScript));
    }

    [Fact]
    public async Task TriageEngine_ExecutesLuaScoreFunction()
    {
        string script = @"
return {
    calculate_score = function(urgency, effort)
        return urgency * 10 - effort
    end
}
";

        var state = Lua.LuaState.Create();
        var config = await _loader.LoadFromScriptAsync(script, state);
        var engine = new TriageEngine(config, state);

        var item = new TriageItem { Urgency = 5, Effort = 3 };
        var processed = await engine.ProcessItemAsync(item);

        Assert.Equal(47.0, processed.Score);
    }
}
