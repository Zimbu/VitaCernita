-- =======================================================================
-- VitaCernita Gmail Filter Configuration
-- =======================================================================
-- Demonstrating composite rules with:
-- - String matching fields: from, to, cc, bcc, subject, list, filename, delivered-to,
--   rfc822msgid, header, label, exact match("phrase").
-- - Email fragments and display names.
-- - Date operators: after, before, older, newer.
-- - Duration operators: older_than, newer_than (e.g. 7d, 3m, 1y).
-- - Global custom date format: date_format = "MM-dd-YYYY".

return {
    -- Global custom date format (enforces MM-dd-YYYY for all dates in this config)
    date_format = "MM-dd-YYYY",

    rules = {
        -- Rule 1: High Priority Audit Alerts with Exact Phrase Match
        rule {
            name = "Security Incident - Confidential Audit Alert",
            match = And(
                From("secops@company.com"),
                To("compliance@company.com"),
                Cc("ciso@company.com"),
                Subject("Security Audit"),
                Header("X-Severity", "CRITICAL"),
                Label("security-alerts"),
                match("unauthorized privilege escalation")
            )
        },

        -- Rule 2: Invoices & Receipts with Attachment Filename Match & Date Range
        rule {
            name = "Invoices and Monthly Billing (2026 Fiscal Year)",
            match = And(
                Or(From("billing@aws.com"), From("invoicing@google.com")),
                Or(Filename("invoice.pdf"), Filename("receipt.pdf")),
                After("01-01-2026"),
                Before("12-31-2026"),
                Label("finance-invoices")
            )
        },

        -- Rule 3: Archive Stale Engineering Announcements with Duration Filter
        rule {
            name = "Stale Engineering Announcements",
            match = And(
                List("dev-announce@lists.company.com"),
                delivered_to("oncall-alias@company.com"),
                older_than("90d"),
                Label("engineering")
            )
        },

        -- Rule 4: Recent Calendar Escalations via Header and newer_than
        rule {
            name = "Recent Calendar Escalations via Header",
            match = And(
                Header("X-Google-Calendar-Notification:rsvpWithNote"),
                rfc822msgid("meeting-alert-2026@google.com"),
                newer_than("14d")
            )
        },

        -- Rule 5: Email Domain Fragment Match with older operator
        rule {
            name = "Vendor Notifications Before Q3",
            match = And(
                From("@vendor-services.org"),
                older("07-01-2026")
            )
        }
    }
}

