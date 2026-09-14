using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using VitaCernita.Core.Queries.Validation;

namespace VitaCernita.Core.Queries;

/// <summary>
/// Parses Gmail filter search query text and API criteria into typed <see cref="IQueryCondition"/> trees.
/// Strictly adheres to the simplified grammar:
/// - Supports fields (from, to, subject, has, is, in, category, size, dates, exact match)
/// - Supports 'not(field)', 'not(and())', and 'not(or())'
/// - Supports 'or()' containing only fields or 'not(field)'
/// - Supports 'and()' containing fields, 'not(field)', and 'or()'
/// - Defaults any other structure or unparseable query to <see cref="RawQueryCondition"/>.
/// </summary>
public static class GmailQueryTextParser
{
    /// <summary>
    /// Parses Google Gmail API criteria fields into a structured <see cref="IQueryCondition"/>,
    /// falling back to <see cref="RawQueryCondition"/> if criteria cannot be cleanly represented.
    /// </summary>
    public static IQueryCondition? ParseFilterCriteria(
        string? query,
        string? from,
        string? to,
        string? subject,
        string? negatedQuery,
        bool hasAttachment,
        long? size = null,
        string? sizeComparison = null)
    {
        // 1. Build raw query fallback string to preserve exact fidelity if parsing fails
        var rawParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(query)) rawParts.Add(query.Trim());
        if (!string.IsNullOrWhiteSpace(from)) rawParts.Add($"from:{from.Trim()}");
        if (!string.IsNullOrWhiteSpace(to)) rawParts.Add($"to:{to.Trim()}");
        if (!string.IsNullOrWhiteSpace(subject)) rawParts.Add($"subject:{subject.Trim()}");
        if (!string.IsNullOrWhiteSpace(negatedQuery)) rawParts.Add($"-({negatedQuery.Trim()})");
        if (hasAttachment) rawParts.Add("has:attachment");
        if (size.HasValue) rawParts.Add($"{sizeComparison ?? "larger"}:{size.Value}");
        string rawFallback = string.Join(" ", rawParts);

        if (string.IsNullOrWhiteSpace(rawFallback))
        {
            return null;
        }

        var conditions = new List<IQueryCondition>();

        // 2. Process 'from'
        if (!string.IsNullOrWhiteSpace(from))
        {
            if (from.Contains(" OR ", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParse(from, out var fromCond) && fromCond != null &&
                    (IsOrOfFieldsOrNotFields(fromCond) || IsField(fromCond)))
                {
                    conditions.Add(fromCond);
                }
                else
                {
                    return new RawQueryCondition(rawFallback);
                }
            }
            else
            {
                try
                {
                    conditions.Add(new FieldCondition("from", from.Trim()));
                }
                catch
                {
                    return new RawQueryCondition(rawFallback);
                }
            }
        }

        // 3. Process 'to'
        if (!string.IsNullOrWhiteSpace(to))
        {
            if (to.Contains(" OR ", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParse(to, out var toCond) && toCond != null &&
                    (IsOrOfFieldsOrNotFields(toCond) || IsField(toCond)))
                {
                    conditions.Add(toCond);
                }
                else
                {
                    return new RawQueryCondition(rawFallback);
                }
            }
            else
            {
                try
                {
                    conditions.Add(new FieldCondition("to", to.Trim()));
                }
                catch
                {
                    return new RawQueryCondition(rawFallback);
                }
            }
        }

        // 4. Process 'subject'
        if (!string.IsNullOrWhiteSpace(subject))
        {
            if (subject.Contains(" OR ", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParse(subject, out var subCond) && subCond != null &&
                    (IsOrOfFieldsOrNotFields(subCond) || IsField(subCond)))
                {
                    conditions.Add(subCond);
                }
                else
                {
                    try
                    {
                        conditions.Add(new FieldCondition("subject", subject.Trim()));
                    }
                    catch
                    {
                        return new RawQueryCondition(rawFallback);
                    }
                }
            }
            else
            {
                try
                {
                    conditions.Add(new FieldCondition("subject", subject.Trim()));
                }
                catch
                {
                    return new RawQueryCondition(rawFallback);
                }
            }
        }

        // 5. Process 'hasAttachment'
        if (hasAttachment)
        {
            conditions.Add(new HasCondition("attachment"));
        }

        // 6. Process 'size'
        if (size.HasValue)
        {
            try
            {
                conditions.Add(new SizeCondition(sizeComparison ?? "larger", size.Value.ToString(CultureInfo.InvariantCulture)));
            }
            catch
            {
                return new RawQueryCondition(rawFallback);
            }
        }

        // 7. Process 'query'
        if (!string.IsNullOrWhiteSpace(query))
        {
            if (!TryParse(query.Trim(), out var parsedQuery) || parsedQuery == null)
            {
                return new RawQueryCondition(rawFallback);
            }

            if (parsedQuery is AndCondition ac)
            {
                foreach (var child in ac.Conditions)
                {
                    if (!IsValidAndChild(child))
                    {
                        return new RawQueryCondition(rawFallback);
                    }
                    conditions.Add(child);
                }
            }
            else if (IsValidAndChild(parsedQuery))
            {
                conditions.Add(parsedQuery);
            }
            else if (conditions.Count == 0 && string.IsNullOrWhiteSpace(negatedQuery) &&
                     parsedQuery is NotCondition nc &&
                     (IsField(nc.InnerCondition) ||
                      (nc.InnerCondition is AndCondition nac && nac.Conditions.All(IsValidAndChild)) ||
                      (nc.InnerCondition is OrCondition noc && IsOrOfFieldsOrNotFields(noc))))
            {
                conditions.Add(parsedQuery);
            }
            else
            {
                return new RawQueryCondition(rawFallback);
            }
        }

        // 8. Process 'negatedQuery'
        if (!string.IsNullOrWhiteSpace(negatedQuery))
        {
            if (!TryParse(negatedQuery.Trim(), out var parsedNeg) || parsedNeg == null)
            {
                return new RawQueryCondition(rawFallback);
            }

            if (conditions.Count == 0)
            {
                // Top-level Not: supports not(field), not(and()), not(or())
                if (IsField(parsedNeg) ||
                    (parsedNeg is AndCondition nac && nac.Conditions.All(IsValidAndChild)) ||
                    (parsedNeg is OrCondition noc && IsOrOfFieldsOrNotFields(noc)))
                {
                    conditions.Add(new NotCondition(parsedNeg));
                }
                else
                {
                    return new RawQueryCondition(rawFallback);
                }
            }
            else
            {
                // Inside an And: only 'not()' fields are permitted
                if (IsField(parsedNeg))
                {
                    conditions.Add(new NotCondition(parsedNeg));
                }
                else
                {
                    return new RawQueryCondition(rawFallback);
                }
            }
        }

        // 9. Combine conditions
        if (conditions.Count == 0)
        {
            return null;
        }

        if (conditions.Count == 1)
        {
            return conditions[0];
        }

        if (conditions.All(IsValidAndChild))
        {
            return new AndCondition(conditions);
        }

        return new RawQueryCondition(rawFallback);
    }

    /// <summary>
    /// Attempts to parse a Gmail query string into an <see cref="IQueryCondition"/>
    /// adhering to the simplified grammar.
    /// </summary>
    public static bool TryParse(string? text, out IQueryCondition? condition)
    {
        condition = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (!Tokenize(text.Trim(), out var tokens) || tokens.Count == 0)
        {
            return false;
        }

        return TryParseTokens(tokens, out condition);
    }

    private static bool TryParseTokens(List<string> tokens, out IQueryCondition? condition)
    {
        condition = null;
        if (tokens.Count == 0) return false;

        // Strip explicit 'AND' keywords (in Gmail, 'AND' is syntactic sugar for space conjunction)
        tokens = tokens.Where(t => !t.Equals("AND", StringComparison.OrdinalIgnoreCase)).ToList();
        if (tokens.Count == 0) return false;

        // Check for top-level OR disjunction
        if (tokens.Any(t => t.Equals("OR", StringComparison.OrdinalIgnoreCase)))
        {
            var orItems = new List<IQueryCondition>();
            var currentGroup = new List<string>();

            foreach (var tok in tokens)
            {
                if (tok.Equals("OR", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentGroup.Count != 1) return false; // Or must contain single field / not(field) items
                    if (!TryParseSingleToken(currentGroup[0], out var item) || item == null) return false;
                    if (!IsField(item) && !IsNotField(item)) return false;
                    orItems.Add(item);
                    currentGroup.Clear();
                }
                else
                {
                    currentGroup.Add(tok);
                }
            }

            if (currentGroup.Count != 1) return false;
            if (!TryParseSingleToken(currentGroup[0], out var lastItem) || lastItem == null) return false;
            if (!IsField(lastItem) && !IsNotField(lastItem)) return false;
            orItems.Add(lastItem);

            condition = orItems.Count == 1 ? orItems[0] : new OrCondition(orItems);
            return true;
        }

        // Conjunction (AND): multiple tokens
        if (tokens.Count > 1)
        {
            var andItems = new List<IQueryCondition>();
            foreach (var tok in tokens)
            {
                if (!TryParseSingleToken(tok, out var child) || child == null) return false;
                if (!IsValidAndChild(child)) return false;
                andItems.Add(child);
            }

            condition = new AndCondition(andItems);
            return true;
        }

        // Single token
        return TryParseSingleToken(tokens[0], out condition);
    }

    private static bool TryParseSingleToken(string tok, out IQueryCondition? condition)
    {
        condition = null;
        if (string.IsNullOrWhiteSpace(tok)) return false;
        tok = tok.Trim();

        // 1. Negated parenthesized expression: '-(...)'
        if (tok.StartsWith("-(") && tok.EndsWith(")"))
        {
            string inner = tok[2..^1].Trim();
            if (!TryParse(inner, out var innerCond) || innerCond == null) return false;

            if (IsField(innerCond) ||
                (innerCond is AndCondition ac && ac.Conditions.All(IsValidAndChild)) ||
                (innerCond is OrCondition oc && IsOrOfFieldsOrNotFields(oc)))
            {
                condition = new NotCondition(innerCond);
                return true;
            }

            return false;
        }

        // 2. Parenthesized group: '(...)'
        if (tok.StartsWith("(") && tok.EndsWith(")"))
        {
            string inner = tok[1..^1].Trim();
            return TryParse(inner, out condition);
        }

        // 3. Curly-bracket OR group: '{a b c}' (Gmail GUI shorthand for OR)
        if (tok.StartsWith("{") && tok.EndsWith("}"))
        {
            string inner = tok[1..^1].Trim();
            if (!Tokenize(inner, out var innerTokens) || innerTokens.Count == 0) return false;

            var orItems = new List<IQueryCondition>();
            foreach (var it in innerTokens)
            {
                if (!TryParseSingleToken(it, out var c) || c == null) return false;
                if (!IsField(c) && !IsNotField(c)) return false;
                orItems.Add(c);
            }

            condition = orItems.Count == 1 ? orItems[0] : new OrCondition(orItems);
            return true;
        }

        // 4. Negated single token: '-token'
        if (tok.StartsWith("-") && tok.Length > 1)
        {
            string sub = tok[1..];
            if (!TryParseField(sub, out var fieldCond) || fieldCond == null) return false;
            if (!IsField(fieldCond)) return false;

            condition = new NotCondition(fieldCond);
            return true;
        }

        // 5. 'NOT token'
        if (tok.StartsWith("NOT ", StringComparison.OrdinalIgnoreCase) && tok.Length > 4)
        {
            string sub = tok[4..].Trim();
            return TryParseSingleToken("-" + sub, out condition);
        }

        // 6. Direct field match
        return TryParseField(tok, out condition);
    }

    private static bool TryParseField(string tok, out IQueryCondition? condition)
    {
        condition = null;
        if (string.IsNullOrWhiteSpace(tok)) return false;
        tok = tok.Trim();

        // Exact quoted phrase: "..."
        if (tok.Length >= 2 && tok.StartsWith('"') && tok.EndsWith('"'))
        {
            try
            {
                condition = new ExactMatchCondition(Unquote(tok));
                return true;
            }
            catch
            {
                return false;
            }
        }

        int colon = tok.IndexOf(':');
        if (colon > 0)
        {
            string field = tok[..colon].ToLowerInvariant();
            string val = tok[(colon + 1)..];

            // Value wrapped in parentheses: e.g. from:(alice OR bob)
            if (val.StartsWith("(") && val.EndsWith(")"))
            {
                string inner = val[1..^1].Trim();
                if (inner.Contains(" OR ", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = inner.Split(new[] { " OR ", " or " }, StringSplitOptions.RemoveEmptyEntries);
                    var orList = new List<IQueryCondition>();
                    foreach (var p in parts)
                    {
                        if (TryCreateField(field, p.Trim(), out var fc) && fc != null)
                        {
                            orList.Add(fc);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    condition = orList.Count == 1 ? orList[0] : new OrCondition(orList);
                    return true;
                }
                if (inner.Contains(' '))
                {
                    var parts = inner.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var andList = new List<IQueryCondition>();
                    foreach (var p in parts)
                    {
                        if (TryCreateField(field, p.Trim(), out var fc) && fc != null)
                        {
                            andList.Add(fc);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    condition = andList.Count == 1 ? andList[0] : new AndCondition(andList);
                    return true;
                }
                val = inner;
            }

            val = Unquote(val);
            return TryCreateField(field, val, out condition);
        }

        // Single clean word without colon
        if (!tok.Contains(' ') && !tok.Contains('(') && !tok.Contains(')') && !tok.Contains('{') && !tok.Contains('}'))
        {
            try
            {
                condition = new ExactMatchCondition(tok);
                return true;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    private static bool TryCreateField(string field, string val, out IQueryCondition? condition)
    {
        condition = null;
        if (string.IsNullOrWhiteSpace(val)) return false;

        try
        {
            switch (field.ToLowerInvariant())
            {
                case "from":
                case "to":
                case "cc":
                case "bcc":
                case "subject":
                case "label":
                case "list":
                case "filename":
                case "deliveredto":
                case "delivered-to":
                case "delivered_to":
                case "rfc822msgid":
                case "header":
                    condition = new FieldCondition(field, val);
                    return true;

                case "has":
                    condition = new HasCondition(val);
                    return true;

                case "is":
                    condition = new IsCondition(val);
                    return true;

                case "in":
                    condition = new InCondition(val);
                    return true;

                case "category":
                    condition = new CategoryCondition(val);
                    return true;

                case "larger":
                case "larger_than":
                    condition = new SizeCondition("larger", val);
                    return true;

                case "smaller":
                case "smaller_than":
                    condition = new SizeCondition("smaller", val);
                    return true;

                case "size":
                    condition = new SizeCondition("size", val);
                    return true;

                case "older_than":
                    condition = new DurationCondition("older_than", val);
                    return true;

                case "newer_than":
                    condition = new DurationCondition("newer_than", val);
                    return true;

                case "after":
                case "before":
                case "older":
                case "newer":
                    if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    {
                        condition = new DateCondition(field, dt);
                    }
                    else
                    {
                        condition = new FieldCondition(field, val);
                    }
                    return true;

                default:
                    condition = new FieldCondition(field, val);
                    return true;
            }
        }
        catch
        {
            condition = null;
            return false;
        }
    }

    private static bool Tokenize(string text, out List<string> tokens)
    {
        tokens = new List<string>();
        int i = 0;
        int len = text.Length;

        while (i < len)
        {
            while (i < len && char.IsWhiteSpace(text[i])) i++;
            if (i >= len) break;

            // Negated group: '-(...)'
            if (text[i] == '-' && i + 1 < len && text[i + 1] == '(')
            {
                int pDepth = 1;
                int j = i + 2;
                while (j < len && pDepth > 0)
                {
                    if (text[j] == '(') pDepth++;
                    else if (text[j] == ')') pDepth--;
                    j++;
                }
                if (pDepth != 0) return false;
                tokens.Add(text[i..j]);
                i = j;
                continue;
            }

            // Parenthesized group: '(...)'
            if (text[i] == '(')
            {
                int pDepth = 1;
                int j = i + 1;
                while (j < len && pDepth > 0)
                {
                    if (text[j] == '(') pDepth++;
                    else if (text[j] == ')') pDepth--;
                    j++;
                }
                if (pDepth != 0) return false;
                tokens.Add(text[i..j]);
                i = j;
                continue;
            }

            // Curly brace group: '{...}'
            if (text[i] == '{')
            {
                int bDepth = 1;
                int j = i + 1;
                while (j < len && bDepth > 0)
                {
                    if (text[j] == '{') bDepth++;
                    else if (text[j] == '}') bDepth--;
                    j++;
                }
                if (bDepth != 0) return false;
                tokens.Add(text[i..j]);
                i = j;
                continue;
            }

            // Quoted string: "..."
            if (text[i] == '"')
            {
                int j = i + 1;
                while (j < len && text[j] != '"')
                {
                    if (text[j] == '\\' && j + 1 < len) j++;
                    j++;
                }
                if (j >= len) return false;
                tokens.Add(text[i..(j + 1)]);
                i = j + 1;
                continue;
            }

            // Negated quote: -"..."
            if (text[i] == '-' && i + 1 < len && text[i + 1] == '"')
            {
                int j = i + 2;
                while (j < len && text[j] != '"')
                {
                    if (text[j] == '\\' && j + 1 < len) j++;
                    j++;
                }
                if (j >= len) return false;
                tokens.Add(text[i..(j + 1)]);
                i = j + 1;
                continue;
            }

            // General token
            int start = i;
            while (i < len && !char.IsWhiteSpace(text[i]))
            {
                if (text[i] == '"')
                {
                    i++;
                    while (i < len && text[i] != '"')
                    {
                        if (text[i] == '\\' && i + 1 < len) i++;
                        i++;
                    }
                    if (i < len) i++;
                }
                else if (text[i] == '(')
                {
                    int pDepth = 1;
                    i++;
                    while (i < len && pDepth > 0)
                    {
                        if (text[i] == '(') pDepth++;
                        else if (text[i] == ')') pDepth--;
                        i++;
                    }
                }
                else
                {
                    i++;
                }
            }

            tokens.Add(text[start..i]);
        }

        return true;
    }

    private static string Unquote(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        s = s.Trim();
        if (s.Length >= 2 && s.StartsWith('"') && s.EndsWith('"'))
        {
            return s[1..^1].Replace("\\\"", "\"");
        }
        return s;
    }

    /// <summary>
    /// Checks if a condition is a basic field condition.
    /// </summary>
    public static bool IsField(IQueryCondition cond) =>
        cond is FieldCondition or HasCondition or IsCondition or InCondition or
                CategoryCondition or SizeCondition or DateCondition or
                DurationCondition or ExactMatchCondition;

    /// <summary>
    /// Checks if a condition is a negation of a basic field condition.
    /// </summary>
    public static bool IsNotField(IQueryCondition cond) =>
        cond is NotCondition nc && IsField(nc.InnerCondition);

    /// <summary>
    /// Checks if an OrCondition contains only fields or not(fields).
    /// </summary>
    public static bool IsOrOfFieldsOrNotFields(IQueryCondition cond) =>
        cond is OrCondition oc && oc.Conditions.Count > 0 && oc.Conditions.All(c => IsField(c) || IsNotField(c));

    /// <summary>
    /// Checks if a condition is valid inside an AndCondition (field, not(field), or or()).
    /// </summary>
    public static bool IsValidAndChild(IQueryCondition cond) =>
        IsField(cond) || IsNotField(cond) || IsOrOfFieldsOrNotFields(cond);
}
