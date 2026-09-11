using System;
using VitaCernita.Core.Filters.Validation;

namespace VitaCernita.Core.Actions;

/// <summary>
/// Exception thrown when action parameters or label definitions fail validation.
/// </summary>
public class ActionValidationException : FilterValidationException
{
    public ActionValidationException(string message) : base(message)
    {
    }

    public ActionValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
