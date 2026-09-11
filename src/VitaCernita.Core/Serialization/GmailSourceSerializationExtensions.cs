using System;
using System.Threading;
using System.Threading.Tasks;
using VitaCernita.Core.Sources;

namespace VitaCernita.Core.Serialization;

/// <summary>
/// Extension methods for serializing <see cref="IGmailSource"/> instances to Lua configuration formats.
/// </summary>
public static class GmailSourceSerializationExtensions
{
    /// <summary>
    /// Serializes an abstract <see cref="IGmailSource"/> into a Functional DSL Lua script.
    /// </summary>
    public static Task<string> ToLuaAsync(
        this IGmailSource source,
        LuaSerializerOptions? options = null,
        CancellationToken cancellationToken = default) =>
        LuaConfigSerializer.Default.SerializeAsync(source, options, cancellationToken);

    /// <summary>
    /// Serializes an abstract <see cref="IGmailSource"/> into a Functional DSL Lua file.
    /// </summary>
    public static Task ToLuaFileAsync(
        this IGmailSource source,
        string filePath,
        LuaSerializerOptions? options = null,
        CancellationToken cancellationToken = default) =>
        LuaConfigSerializer.Default.SerializeToFileAsync(source, filePath, options, cancellationToken);
}
