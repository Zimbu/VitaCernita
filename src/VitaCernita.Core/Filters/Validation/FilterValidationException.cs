using System;
using VitaCernita.Core.Queries.Validation;

namespace VitaCernita.Core.Filters.Validation;

/// <summary>
/// Legacy validation exception preserved for backward compatibility.
/// Inherits from <see cref="QueryValidationException"/>.
/// </summary>
public class FilterValidationException : QueryValidationException
{
    public FilterValidationException(string message) : base(message) { }
    public FilterValidationException(string message, Exception innerException) : base(message, innerException) { }
}
