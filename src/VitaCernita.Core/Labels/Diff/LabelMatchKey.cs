namespace VitaCernita.Core.Labels.Diff;

/// <summary>
/// Strategy used to match labels between current (Gmail account) and desired (Lua config) sets.
/// </summary>
public enum LabelMatchKey
{
    /// <summary>
    /// Match labels by display name (default, recommended since desired configs usually omit ID).
    /// </summary>
    Name,

    /// <summary>
    /// Match labels strictly by immutable ID.
    /// </summary>
    Id,

    /// <summary>
    /// Match by ID first if both labels specify an ID, otherwise fall back to matching by Name.
    /// </summary>
    IdThenName
}
