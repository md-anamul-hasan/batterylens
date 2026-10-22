using System.Collections.Generic;

namespace BatteryLens.Models;

public class DiagnosticCheckItem
{
    public string Category { get; set; } = string.Empty; // "Battery Cell", "Charger / AC", "PC System"
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Assessment { get; set; } = string.Empty;
    public bool IsPassing { get; set; } = true;
    public string Severity { get; set; } = "Normal"; // Normal, Warning, Critical
}

public class ComponentVerdict
{
    public string ComponentName { get; set; } = string.Empty; // "Charger / AC Adapter", "Battery Pack", "Laptop / Motherboard"
    public string ActionRecommendation { get; set; } = string.Empty; // "KEEP - Operating Normally", "REPLACE - Severely Degraded", "INSPECT / SERVICE"
    public bool RequiresReplacement { get; set; }
    public string StatusBadgeColor { get; set; } = "#10B981"; // Hex color
    public string Reason { get; set; } = string.Empty;
    public string CostEstimate { get; set; } = string.Empty; // e.g. "$25 - $45", "$40 - $90", "$700+"
    public string IconKind { get; set; } = "CheckCircle";
}

public class DiagnosticRecommendation
{
    public DiagnosticVerdict Verdict { get; set; } = DiagnosticVerdict.Healthy;
    public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Normal;
    public string Headline { get; set; } = "Battery Health is Optimal";
    public string Subtitle { get; set; } = "No hardware replacement is currently needed.";
    public string SummaryText { get; set; } = string.Empty;
    public int ConfidenceScore { get; set; } = 95;

    public ComponentVerdict ChargerVerdict { get; set; } = new();
    public ComponentVerdict BatteryVerdict { get; set; } = new();
    public ComponentVerdict PcVerdict { get; set; } = new();

    public List<DiagnosticCheckItem> Checks { get; set; } = new();
    public List<string> ActionSteps { get; set; } = new();
    public List<string> MaintenanceTips { get; set; } = new();

    public string BadgeBackground => Urgency switch
    {
        UrgencyLevel.Critical => "#EF4444",
        UrgencyLevel.High => "#F97316",
        UrgencyLevel.Medium => "#F59E0B",
        UrgencyLevel.Low => "#3B82F6",
        _ => "#10B981"
    };
}
