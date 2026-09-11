using System;

namespace VitaCernita.Core.AutoReply.Validation;

/// <summary>
/// Exception thrown when auto-reply (VacationSettings) configuration or inputs fail validation rules.
/// </summary>
public class AutoReplyValidationException : Exception
{
    public AutoReplyValidationException(string message) : base(message)
    {
    }

    public AutoReplyValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
