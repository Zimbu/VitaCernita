using System;
using System.Net;

namespace VitaCernita.Core.Api;

/// <summary>
/// Exception thrown when a Gmail API HTTP request fails.
/// </summary>
public class GmailApiException : Exception
{
    public HttpStatusCode? StatusCode { get; }
    public string? Endpoint { get; }
    public string? ResponseBody { get; }

    public GmailApiException(
        string message,
        HttpStatusCode? statusCode = null,
        string? endpoint = null,
        string? responseBody = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Endpoint = endpoint;
        ResponseBody = responseBody;
    }
}
