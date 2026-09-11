using System;

namespace VitaCernita.Core.Labels.Validation;

public class LabelValidationException : Exception
{
    public LabelValidationException(string message) : base(message)
    {
    }

    public LabelValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
