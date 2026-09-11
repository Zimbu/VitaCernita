using System;
using System.Collections.Generic;
using Lua;
using VitaCernita.Core.Labels.Validation;

namespace VitaCernita.Core.Labels;

/// <summary>
/// Parses Lua tables into GmailLabel and LabelColor objects.
/// </summary>
public static class GmailLabelParser
{
    public static bool IsLabelTable(LuaTable table)
    {
        if (table.TryGetValue("type", out var typeVal))
        {
            string t = typeVal.ToString();
            if (t is "gmail_label" or "label_builder" or "label") return true;
        }

        if (table.TryGetValue("name", out _) || table.TryGetValue("display_name", out _))
        {
            // If it also defines query/criteria or action, it is a composite filter rather than a standalone label
            if (table.TryGetValue("query", out _) || table.TryGetValue("action", out _) ||
                table.TryGetValue("match", out _) || table.TryGetValue("criteria", out _))
            {
                return false;
            }
            return true;
        }

        return false;
    }

    public static GmailLabel ParseLabel(LuaTable table)
    {
        // Unwrap builder _data if present
        if (table.TryGetValue("_data", out var dataVal) && dataVal.TryRead<LuaTable>(out var dataTable))
        {
            return ParseLabel(dataTable);
        }

        string? id = null;
        if (table.TryGetValue("id", out var idVal) && idVal.Type != LuaValueType.Nil)
        {
            id = idVal.ToString();
        }

        string? name = null;
        if (table.TryGetValue("name", out var nameVal) && nameVal.Type != LuaValueType.Nil)
        {
            name = nameVal.ToString();
        }
        else if (table.TryGetValue("display_name", out var dispVal) && dispVal.Type != LuaValueType.Nil)
        {
            name = dispVal.ToString();
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new LabelValidationException("Label name cannot be empty or whitespace.");
        }

        string? messageListVisibility = null;
        if (table.TryGetValue("message_list_visibility", out var mlvVal) && mlvVal.Type != LuaValueType.Nil)
        {
            messageListVisibility = ParseMessageListVisibility(mlvVal);
        }
        else if (table.TryGetValue("messageListVisibility", out var mlvCamel) && mlvCamel.Type != LuaValueType.Nil)
        {
            messageListVisibility = ParseMessageListVisibility(mlvCamel);
        }

        string? labelListVisibility = null;
        if (table.TryGetValue("label_list_visibility", out var llvVal) && llvVal.Type != LuaValueType.Nil)
        {
            labelListVisibility = ParseLabelListVisibility(llvVal);
        }
        else if (table.TryGetValue("labelListVisibility", out var llvCamel) && llvCamel.Type != LuaValueType.Nil)
        {
            labelListVisibility = ParseLabelListVisibility(llvCamel);
        }

        LabelColor? color = null;
        if (table.TryGetValue("color", out var colorVal) && colorVal.Type != LuaValueType.Nil)
        {
            color = ParseColor(colorVal);
        }
        else
        {
            // Check direct text_color/textColor and background_color/backgroundColor on label table
            bool hasText = table.TryGetValue("text_color", out var tcVal) || table.TryGetValue("textColor", out tcVal);
            bool hasBg = table.TryGetValue("background_color", out var bgVal) || table.TryGetValue("backgroundColor", out bgVal);

            if (hasText || hasBg)
            {
                string? textStr = hasText && tcVal.Type != LuaValueType.Nil ? tcVal.ToString() : null;
                string? bgStr = hasBg && bgVal.Type != LuaValueType.Nil ? bgVal.ToString() : null;

                if (string.IsNullOrWhiteSpace(textStr) || string.IsNullOrWhiteSpace(bgStr))
                {
                    throw new LabelValidationException("Both textColor and backgroundColor must be provided when setting a label color.");
                }

                color = new LabelColor(textStr!, bgStr!);
            }
        }

        return new GmailLabel(name!, id, messageListVisibility, labelListVisibility, color);
    }

    public static string ParseMessageListVisibility(LuaValue val)
    {
        if (val.TryRead<bool>(out bool b))
        {
            return MessageListVisibility.FromBoolean(b);
        }
        return MessageListVisibility.Normalize(val.ToString());
    }

    public static string ParseLabelListVisibility(LuaValue val)
    {
        return LabelListVisibility.Normalize(val.ToString());
    }

    public static LabelColor ParseColor(LuaValue val)
    {
        if (val.TryRead<LuaTable>(out var colorTable))
        {
            string? text = null;
            if (colorTable.TryGetValue("text", out var tVal) && tVal.Type != LuaValueType.Nil) text = tVal.ToString();
            else if (colorTable.TryGetValue("textColor", out var tcVal) && tcVal.Type != LuaValueType.Nil) text = tcVal.ToString();
            else if (colorTable.TryGetValue("text_color", out var tuVal) && tuVal.Type != LuaValueType.Nil) text = tuVal.ToString();

            string? bg = null;
            if (colorTable.TryGetValue("background", out var bVal) && bVal.Type != LuaValueType.Nil) bg = bVal.ToString();
            else if (colorTable.TryGetValue("backgroundColor", out var bcVal) && bcVal.Type != LuaValueType.Nil) bg = bcVal.ToString();
            else if (colorTable.TryGetValue("background_color", out var buVal) && buVal.Type != LuaValueType.Nil) bg = buVal.ToString();
            else if (colorTable.TryGetValue("bg", out var bgShort) && bgShort.Type != LuaValueType.Nil) bg = bgShort.ToString();

            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(bg))
            {
                throw new LabelValidationException("Both textColor and backgroundColor must be provided when setting a label color.");
            }

            return new LabelColor(text!, bg!);
        }

        throw new LabelValidationException($"Invalid color specification. Expected color table or color(text, background), got '{val}'.");
    }
}
