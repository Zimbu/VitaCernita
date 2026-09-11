using System;
using System.Collections.Generic;
using Lua;
using VitaCernita.Core.Actions.Validation;

namespace VitaCernita.Core.Actions;

/// <summary>
/// Parses Lua tables and expressions into GmailAction models.
/// Completely decoupled from query / criteria parsing.
/// </summary>
public static class GmailActionParser
{
    private static readonly HashSet<string> ActionKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "archive",
        "mark_unread", "mark_read", "mark_as_read", "read",
        "star",
        "delete", "trash",
        "mark_important", "important",
        "add_category", "categorize",
        "add_label", "add_labels", "apply_label", "apply_labels",
        "forward", "forward_message"
    };

    public static bool IsActionTable(LuaTable table)
    {
        if (table.TryGetValue("type", out var typeVal))
        {
            string t = typeVal.ToString();
            if (t is "action" or "action_builder" or "action_item") return true;
        }

        foreach (var key in ActionKeys)
        {
            if (table.TryGetValue(key, out _)) return true;
        }

        return false;
    }

    public static GmailAction ParseAction(LuaTable table)
    {
        var action = new GmailAction();
        PopulateAction(action, table);
        return action;
    }

    public static GmailAction ParseActionString(string actionName)
    {
        var action = new GmailAction();
        switch (actionName.Trim().ToLowerInvariant())
        {
            case "archive":
                action.Archive();
                break;
            case "mark_unread":
            case "mark_read":
            case "mark_as_read":
            case "read":
                action.MarkUnread();
                break;
            case "star":
                action.Star();
                break;
            case "delete":
            case "trash":
                action.Delete();
                break;
            case "mark_important":
            case "important":
                action.MarkImportant();
                break;
            default:
                throw new ActionValidationException($"Unknown action operation '{actionName}'.");
        }
        return action;
    }

    private static void PopulateAction(GmailAction action, LuaTable table)
    {
        // 1. If wrapped in type == "action" with items
        if (table.TryGetValue("type", out var typeVal))
        {
            string typeStr = typeVal.ToString();
            if (typeStr is "action" or "action_builder")
            {
                if (table.TryGetValue("items", out var itemsVal) && itemsVal.TryRead<LuaTable>(out var itemsTable))
                {
                    PopulateFromItemsList(action, itemsTable);
                    return;
                }
                if (table.TryGetValue("definition", out var defVal) && defVal.TryRead<LuaTable>(out var defTable))
                {
                    PopulateAction(action, defTable);
                    return;
                }
            }
            else if (typeStr == "action_item")
            {
                ApplyActionItem(action, table);
                return;
            }
        }

        // 2. Special case: if table is the archive dual token { type = 'in', value = 'archive' }
        if (table.TryGetValue("action", out var actToken) && actToken.ToString() == "archive")
        {
            action.Archive();
            return;
        }

        // 3. Array of action items: { archive, star, ... }
        bool hasArrayItems = false;
        for (int i = 1; i <= table.ArrayLength; i++)
        {
            if (table[i].TryRead<LuaTable>(out var itemTable))
            {
                ApplyActionItem(action, itemTable);
                hasArrayItems = true;
            }
        }
        if (hasArrayItems && table.ArrayLength > 0 && !HasNamedActionKeys(table))
        {
            return;
        }

        // 4. Named action properties in table
        if (table.TryGetValue("archive", out var archVal) && archVal.Type == LuaValueType.Boolean && archVal.Read<bool>())
        {
            action.Archive();
        }

        foreach (var key in new[] { "mark_unread", "mark_read", "mark_as_read", "read" })
        {
            if (table.TryGetValue(key, out var unreadVal) && unreadVal.Type == LuaValueType.Boolean && unreadVal.Read<bool>())
            {
                action.MarkUnread();
                break;
            }
        }

        if (table.TryGetValue("star", out var starVal) && starVal.Type == LuaValueType.Boolean && starVal.Read<bool>())
        {
            action.Star();
        }

        foreach (var key in new[] { "delete", "trash" })
        {
            if (table.TryGetValue(key, out var delVal) && delVal.Type == LuaValueType.Boolean && delVal.Read<bool>())
            {
                action.Delete();
                break;
            }
        }

        foreach (var key in new[] { "mark_important", "important" })
        {
            if (table.TryGetValue(key, out var impVal) && impVal.Type == LuaValueType.Boolean && impVal.Read<bool>())
            {
                action.MarkImportant();
                break;
            }
        }

        foreach (var key in new[] { "add_category", "category", "categorize" })
        {
            if (table.TryGetValue(key, out var catVal) && catVal.Type == LuaValueType.String)
            {
                action.AddCategory(catVal.Read<string>());
                break;
            }
        }

        foreach (var key in new[] { "add_label", "add_labels", "apply_label", "apply_labels", "label" })
        {
            if (table.TryGetValue(key, out var lblVal))
            {
                if (lblVal.Type == LuaValueType.String)
                {
                    action.AddCustomLabel(lblVal.Read<string>());
                }
                else if (lblVal.TryRead<LuaTable>(out var lblTable))
                {
                    for (int i = 1; i <= lblTable.ArrayLength; i++)
                    {
                        if (lblTable[i].Type == LuaValueType.String)
                        {
                            action.AddCustomLabel(lblTable[i].Read<string>());
                        }
                    }
                }
                break;
            }
        }

        foreach (var key in new[] { "forward", "forward_message" })
        {
            if (table.TryGetValue(key, out var fwdVal) && fwdVal.Type == LuaValueType.String)
            {
                action.SetForward(fwdVal.Read<string>());
                break;
            }
        }
    }

    private static void PopulateFromItemsList(GmailAction action, LuaTable itemsTable)
    {
        for (int i = 1; i <= itemsTable.ArrayLength; i++)
        {
            if (itemsTable[i].TryRead<LuaTable>(out var itemTable))
            {
                ApplyActionItem(action, itemTable);
            }
        }
        if (itemsTable.ArrayLength == 0)
        {
            ApplyActionItem(action, itemsTable);
        }
    }

    private static void ApplyActionItem(GmailAction action, LuaTable itemTable)
    {
        // Check if it's the dual archive token
        if (itemTable.TryGetValue("action", out var directAction) && directAction.ToString() == "archive")
        {
            action.Archive();
            return;
        }

        if (itemTable.TryGetValue("type", out var typeVal) && typeVal.ToString() == "action_item")
        {
            string act = itemTable.TryGetValue("action", out var aVal) ? aVal.ToString().ToLowerInvariant() : string.Empty;
            switch (act)
            {
                case "archive":
                    action.Archive();
                    break;
                case "mark_unread":
                case "mark_read":
                case "read":
                    action.MarkUnread();
                    break;
                case "star":
                    action.Star();
                    break;
                case "delete":
                case "trash":
                    action.Delete();
                    break;
                case "mark_important":
                case "important":
                    action.MarkImportant();
                    break;
                case "add_category":
                case "categorize":
                    string cat = itemTable.TryGetValue("value", out var cVal) ? cVal.ToString() : string.Empty;
                    action.AddCategory(cat);
                    break;
                case "add_label":
                case "apply_label":
                    if (itemTable.TryGetValue("value", out var lVal))
                    {
                        if (lVal.Type == LuaValueType.String)
                        {
                            action.AddCustomLabel(lVal.Read<string>());
                        }
                        else if (lVal.TryRead<LuaTable>(out var lTbl))
                        {
                            for (int i = 1; i <= lTbl.ArrayLength; i++)
                            {
                                if (lTbl[i].Type == LuaValueType.String)
                                    action.AddCustomLabel(lTbl[i].Read<string>());
                            }
                        }
                    }
                    break;
                case "add_labels":
                case "apply_labels":
                    if (itemTable.TryGetValue("value", out var lsVal) && lsVal.TryRead<LuaTable>(out var lsTbl))
                    {
                        for (int i = 1; i <= lsTbl.ArrayLength; i++)
                        {
                            if (lsTbl[i].Type == LuaValueType.String)
                                action.AddCustomLabel(lsTbl[i].Read<string>());
                        }
                    }
                    break;
                case "forward":
                case "forward_message":
                    string email = itemTable.TryGetValue("value", out var fVal) ? fVal.ToString() : string.Empty;
                    action.SetForward(email);
                    break;
                default:
                    throw new ActionValidationException($"Unknown action operation '{act}'.");
            }
            return;
        }

        // If it's a nested action table
        PopulateAction(action, itemTable);
    }

    private static bool HasNamedActionKeys(LuaTable table)
    {
        foreach (var key in ActionKeys)
        {
            if (table.TryGetValue(key, out _)) return true;
        }
        return false;
    }
}
