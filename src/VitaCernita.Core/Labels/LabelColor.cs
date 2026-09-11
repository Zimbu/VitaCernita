using System;
using System.Collections.Generic;
using VitaCernita.Core.Labels.Validation;

namespace VitaCernita.Core.Labels;

/// <summary>
/// Represents a label color containing a text color and background color.
/// Colors must be chosen from the predefined set of allowed hex values specified by Google Gmail API.
/// </summary>
public sealed class LabelColor : IEquatable<LabelColor>
{
    /// <summary>
    /// The predefined set of 102 color values allowed by the Google Gmail API.
    /// </summary>
    public static readonly IReadOnlySet<string> AllowedHexColors = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "#000000", "#434343", "#666666", "#999999", "#cccccc", "#efefef", "#f3f3f3", "#ffffff",
        "#fb4c2f", "#ffad47", "#fad165", "#16a766", "#43d692", "#4a86e8", "#a479e2", "#f691b3",
        "#f6c5be", "#ffe6c7", "#fef1d1", "#b9e4d0", "#c6f3de", "#c9daf8", "#e4d7f5", "#fcdee8",
        "#efa093", "#ffd6a2", "#fce8b3", "#89d3b2", "#a0eac9", "#a4c2f4", "#d0bcf1", "#fbc8d9",
        "#e66550", "#ffbc6b", "#fcda83", "#44b984", "#68dfa9", "#6d9eeb", "#b694e8", "#f7a7c0",
        "#cc3a21", "#eaa041", "#f2c960", "#149e60", "#3dc789", "#3c78d8", "#8e63ce", "#e07798",
        "#ac2b16", "#cf8933", "#d5ae49", "#0b804b", "#2a9c68", "#285bac", "#653e9b", "#b65775",
        "#822111", "#a46a21", "#aa8831", "#076239", "#1a764d", "#1c4587", "#41236d", "#83334c",
        "#464646", "#e7e7e7", "#0d3472", "#b6cff5", "#0d3b44", "#98d7e4", "#3d188e", "#e3d7ff",
        "#711a36", "#fbd3e0", "#8a1c0a", "#f2b2a8", "#7a2e0b", "#ffc8af", "#7a4706", "#ffdeb5",
        "#594c05", "#fbe983", "#684e07", "#fdedc1", "#0b4f30", "#b3efd3", "#04502e", "#a2dcc1",
        "#c2c2c2", "#4986e7", "#2da2bb", "#b99aff", "#994a64", "#f691b2", "#ff7537", "#ffad46",
        "#662e37", "#ebdbde", "#cca6ac", "#094228", "#42d692", "#16a765"
    };

    /// <summary>
    /// Standard named color aliases that match exact hex values in the Gmail API allowed color list.
    /// Only common colors whose standard hex value appears in the allowed set are included.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> ColorAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["black"] = "#000000",
        ["white"] = "#ffffff"
    };

    public string TextColor { get; }
    public string BackgroundColor { get; }

    public LabelColor(string textColor, string backgroundColor)
    {
        TextColor = NormalizeColorOrThrow(textColor, nameof(textColor));
        BackgroundColor = NormalizeColorOrThrow(backgroundColor, nameof(backgroundColor));
    }

    /// <summary>
    /// Normalizes and validates a color value against the predefined Gmail color list and supported aliases.
    /// </summary>
    public static string NormalizeColorOrThrow(string? color, string fieldName = "color")
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            throw new LabelValidationException($"{fieldName} cannot be empty.");
        }

        string trimmed = color.Trim();

        // Check if it matches an alias
        if (ColorAliases.TryGetValue(trimmed, out string? aliasHex))
        {
            return aliasHex;
        }

        // Format hex with leading hash
        string hex = trimmed.StartsWith('#') ? trimmed : "#" + trimmed;
        hex = hex.ToLowerInvariant();

        if (!AllowedHexColors.Contains(hex))
        {
            throw new LabelValidationException(
                $"Invalid color '{color}' for {fieldName}. Value must be one of the 102 predefined hex colors allowed by the Google Gmail API (e.g. #000000, #ffffff, #4a86e8, #16a766) or a supported alias ('black', 'white').");
        }

        return hex;
    }

    public static bool TryNormalizeColor(string? color, out string? normalizedHex)
    {
        normalizedHex = null;
        if (string.IsNullOrWhiteSpace(color)) return false;

        string trimmed = color.Trim();
        if (ColorAliases.TryGetValue(trimmed, out string? aliasHex))
        {
            normalizedHex = aliasHex;
            return true;
        }

        string hex = trimmed.StartsWith('#') ? trimmed : "#" + trimmed;
        hex = hex.ToLowerInvariant();

        if (AllowedHexColors.Contains(hex))
        {
            normalizedHex = hex;
            return true;
        }

        return false;
    }

    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            ["textColor"] = TextColor,
            ["backgroundColor"] = BackgroundColor
        };
    }

    public override string ToString() => $"textColor: {TextColor}, backgroundColor: {BackgroundColor}";

    public bool Equals(LabelColor? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(TextColor, other.TextColor, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(BackgroundColor, other.BackgroundColor, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => obj is LabelColor other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(TextColor.ToLowerInvariant(), BackgroundColor.ToLowerInvariant());
}
