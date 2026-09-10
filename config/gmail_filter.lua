-- =======================================================================
-- VitaCernita Gmail Filter Configuration
-- =======================================================================
-- Demonstrating composite rules with all supported Gmail API fields:
-- from, to, cc, bcc, subject, list, filename, delivered-to, rfc822msgid, header,
-- exact phrase matches (match("...")), and operators (label, has, is, category, in).

return {
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
                match("unauthorized privilege escalation")
            )
        },

        -- Rule 2: Invoices & Receipts with Attachment Filename Match
        rule {
            name = "Invoices and Monthly Billing",
            match = And(
                Or(From("billing@aws.com"), From("invoicing@google.com")),
                Or(Filename("invoice.pdf"), Filename("receipt.pdf")),
                Has("attachment")
            )
        },

        -- Rule 3: Mailing List & Delivered-To Filter
        rule {
            name = "Internal Engineering Announcements",
            match = And(
                List("dev-announce@lists.company.com"),
                delivered_to("oncall-alias@company.com"),
                Is("unread")
            )
        },

        -- Rule 4: Header & Message-ID Exact Thread Lookup
        rule {
            name = "Calendar Escalations via Header",
            match = And(
                Header("X-Google-Calendar-Notification:rsvpWithNote"),
                rfc822msgid("meeting-alert-2026@google.com")
            )
        }
    }
}
