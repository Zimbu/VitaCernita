-- =======================================================================
-- VitaCernita Gmail Filter Configuration
-- =======================================================================
-- This configuration defines a Gmail search filter rule combining
-- 'from' and 'subject' matches with an 'and' operator.

-- Option A: Using the functional DSL:
return rule {
    name = "Production Incident Alert Filter",
    match = And(
        From("alerts@monitoring.com"),
        Subject("High CPU")
    )
}

-- Other supported equivalent expressions:
--
-- Option B: Declarative table with explicit 'and' map:
-- return {
--     ["and"] = {
--         from = "alerts@monitoring.com",
--         subject = "High CPU"
--     }
-- }
--
-- Option C: Declarative table with explicit 'and' array:
-- return {
--     ["and"] = {
--         { from = "alerts@monitoring.com" },
--         { subject = "High CPU" }
--     }
-- }
--
-- Option D: Fluent builder syntax:
-- return filter():from("alerts@monitoring.com"):subject("High CPU")
--
-- Option E: Direct properties table (implicit AND):
-- return {
--     from = "alerts@monitoring.com",
--     subject = "High CPU"
-- }
