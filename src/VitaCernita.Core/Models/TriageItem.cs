using System;
using System.Collections.Generic;

namespace VitaCernita.Core.Models;

public sealed class TriageItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Title { get; set; } = string.Empty;
    public int Urgency { get; set; } = 5;
    public int Effort { get; set; } = 5;
    public double Score { get; set; }
    public string Category { get; set; } = "Unassigned";
    public List<string> Tags { get; set; } = new();
}
