-- =======================================================================
-- VitaCernita Configuration
-- Evaluated by the embedded Lua-CSharp interpreter (.NET Core)
-- =======================================================================

return {
    -- Project metadata
    project = {
        name = "VitaCernita",
        version = "0.1.0",
        description = "Cross-platform Core C# engine with Lua configuration",
        author = "Phillip Dressen",
    },

    -- Runtime settings
    settings = {
        -- Log level: "Trace", "Debug", "Information", "Warning", "Error"
        log_level = env("VITACERNITA_LOG_LEVEL", "Information"),

        -- Concurrency and execution limits
        max_concurrency = 8,
        output_dir = "./output",
        dry_run = false,
        timeout_seconds = 30,
    },

    -- Triage & screening rules
    rules = {
        {
            id = "rule-critical",
            description = "High urgency tasks escalated immediately",
            priority = 500,
            enabled = true,
            category = "Critical",
            params = {
                threshold = "8",
                notify = "true",
            },
        },
        {
            id = "rule-routine",
            description = "Standard operational tasks",
            priority = 100,
            enabled = true,
            category = "Standard",
            params = {
                batch_size = "25",
            },
        },
        {
            id = "rule-archive",
            description = "Low priority or historical items",
            priority = 10,
            enabled = true,
            category = "Archive",
            params = {
                retention_days = "90",
            },
        },
    },

    -- Custom key-value properties
    custom = {
        host_platform = platform(),
        region = env("REGION", "us-central"),
        app_mode = "cli",
    },

    -- Lua function hook: dynamic calculation of item priority score
    calculate_score = function(urgency, effort)
        -- Higher urgency increases score, higher effort slightly discounts score
        return (urgency * 2.5) - (effort * 0.8)
    end,
}
