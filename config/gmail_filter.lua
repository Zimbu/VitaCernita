-- =======================================================================
-- VitaCernita Gmail Filter Configuration
-- =======================================================================
-- This configuration showcases composite Gmail filter rules combining
-- AND and OR operators with arbitrary nesting depths.

return {
    rules = {
        -- Rule 1: OR containing three different matches of AND operations
        -- (Matching three distinct combinations of from and subject)
        rule {
            name = "Tri-Team Incident Dispatcher (OR of three ANDs)",
            match = Or(
                And(From("secops@company.com"), Subject("Security Breach")),
                And(From("devops@company.com"), Subject("Cluster Outage")),
                And(From("netops@company.com"), Subject("BGP Route Leak"))
            )
        },

        -- Rule 2: AND containing nested OR
        -- (Matching a specific sender with any of several priority subjects)
        rule {
            name = "Executive Escalations (AND containing OR)",
            match = And(
                From("ceo@company.com"),
                Or(
                    Subject("Immediate Action Required"),
                    Subject("Board Resolution"),
                    Subject("Urgent")
                )
            )
        },

        -- Rule 3: Declarative table style (OR containing ANDs)
        rule {
            name = "Declarative On-Call Routing",
            match = {
                ["or"] = {
                    { from = "pagerduty.com", subject = "Sev-1" },
                    { from = "datadog.com", subject = "Monitor Triggered" }
                }
            }
        }
    }
}
