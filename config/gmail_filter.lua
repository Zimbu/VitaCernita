-- =======================================================================
-- VitaCernita Gmail Filter Configuration
-- =======================================================================
-- Demonstrating composite rules with:
-- - String matching fields: from, to, cc, bcc, subject, list, filename, delivered-to,
--   rfc822msgid, header, label, exact match("phrase").
-- - Star & Icon operators: has_yellow_star, has_red_bang, is_starred, etc.
-- - Media, Document & Label metadata: has_attachment, has_drive, has_document, has_user_labels, etc.
-- - Status & State operators: is_unread, is_read, is_important, is_muted, is_snoozed, etc.
-- - Location & Folder operators: in_inbox, in_archive, in_trash, in_spam, in_anywhere, etc.
-- - Category operators: category_promotions, category_updates, category_social, category_primary, etc.
-- - Size operators: size, larger, smaller, larger_than, smaller_than (e.g. 5M, 500K, 1000000).
-- - Date operators: after, before, older, newer.
-- - Duration operators: older_than, newer_than (e.g. 7d, 3m, 1y).
-- - Negation: not(), negate(), invert().
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
            ),
            action = actions(archive, add_category('Purchases'), add_label('Receipts'), forward_message('accounting@company.com'))
        },

        -- Rule 3: Archive Stale Engineering Announcements with Duration Filter
        rule {
            name = "Stale Engineering Announcements",
            match = And(
                List("dev-announce@lists.company.com"),
                delivered_to("oncall-alias@company.com"),
                older_than("90d"),
                Label("engineering")
            ),
            action = actions(archive, mark_read)
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
        },

        -- Rule 6: Critical Action Items (Red Bang or Yellow Bang)
        rule {
            name = "Critical Action Items with Star Operators",
            match = And(
                From("director@company.com"),
                Or(has_red_bang, has_yellow_bang),
                newer_than("7d")
            ),
            action = actions(star, mark_important)
        },

        -- Rule 7: Follow-ups with Guillemets and Star Icons
        rule {
            name = "Important Inquiries & Follow-ups",
            match = And(
                Label("action-needed"),
                Or(has_orange_guillemet, has_purple_question, has_green_check)
            )
        },

        -- Rule 8: All Starred Messages from Leadership (matches any star color/icon)
        rule {
            name = "Leadership Starred Highlights",
            match = And(
                From("exec-team@company.com"),
                is_starred
            )
        },

        -- Rule 9: Negate specific terms and negated logical OR expressions
        rule {
            name = "Direct Executive Traffic Excluding Automated Noise",
            match = And(
                From("exec-team@company.com"),
                not({ is_starred = true }),
                not(Or(Subject("automated"), Subject("newsletter")))
            )
        },

        -- Rule 10: Unread Important Communications in Inbox
        rule {
            name = "Unread Critical Inbox Items",
            match = And(
                in_inbox,
                is_unread,
                is_important
            )
        },

        -- Rule 11: Drive and Workspace Document Attachments
        rule {
            name = "Project Collateral with Cloud Documents",
            match = And(
                From("pm@company.com"),
                has_drive,
                Or(has_document, has_spreadsheet)
            )
        },

        -- Rule 12: Promotional Emails & File Size Boundary Filtering
        rule {
            name = "Large Media and Promotional Collateral",
            match = And(
                category_promotions,
                larger("5M"),
                smaller("25M")
            ),
            action = actions(delete)
        }
    }
}

