using System;

namespace VitaCernita.Core.Filters.Validation;

public class FilterValidationException : Exception
{
    public FilterValidationException(string message) : base(message) { }
    public FilterValidationException(string message, Exception innerException) : base(message, innerException) { }
}
