using System;
using System.Collections.Generic;
using VitaCernita.Core.AutoReply;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;

namespace VitaCernita.Core.Configuration;

/// <summary>
/// Represents a loaded Gmail configuration containing filters, labels, and auto-reply (vacation responder) settings.
/// </summary>
public class GmailConfiguration
{
    public IReadOnlyList<GmailFilter> Filters { get; init; } = Array.Empty<GmailFilter>();
    public IReadOnlyList<GmailLabel> Labels { get; init; } = Array.Empty<GmailLabel>();
    public AutoReply.AutoReply? AutoReply { get; init; }
    public string? CustomDateFormat { get; init; }
}

