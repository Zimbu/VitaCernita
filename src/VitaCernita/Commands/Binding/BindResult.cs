using System;
using System.Collections.Generic;

namespace VitaCernita.Cli.Commands.Binding;

/// <summary>
/// Result of binding CLI arguments to an <see cref="ICliCommand"/>'s properties.
/// </summary>
public sealed class BindResult
{
    public bool IsSuccess { get; }
    public bool HelpRequested { get; }
    public string? ErrorMessage { get; }
    public IReadOnlyList<string> UnhandledArguments { get; }

    private BindResult(bool isSuccess, bool helpRequested, string? errorMessage, IReadOnlyList<string> unhandledArguments)
    {
        IsSuccess = isSuccess;
        HelpRequested = helpRequested;
        ErrorMessage = errorMessage;
        UnhandledArguments = unhandledArguments;
    }

    public static BindResult Success(IReadOnlyList<string>? unhandled = null) =>
        new(true, false, null, unhandled ?? Array.Empty<string>());

    public static BindResult Help() =>
        new(false, true, null, Array.Empty<string>());

    public static BindResult Failure(string errorMessage) =>
        new(false, false, errorMessage, Array.Empty<string>());
}
