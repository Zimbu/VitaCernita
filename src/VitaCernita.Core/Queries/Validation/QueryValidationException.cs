using System;

namespace VitaCernita.Core.Queries.Validation;

/// <summary>
/// Exception thrown when a Gmail search query parameter or syntax fails validation.
/// </summary>
public class QueryValidationException : Exception
{
    public QueryValidationException(string message) : base(message) { }
    public QueryValidationException(string message, Exception innerException) : base(message, innerException) { }
}
