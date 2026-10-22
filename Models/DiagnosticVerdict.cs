namespace BatteryLens.Models;

public enum DiagnosticVerdict
{
    Healthy,
    ReplaceBattery,
    ReplaceCharger,
    UpgradeOrServicePc,
    NeedsCalibration,
    NoBatteryDetected
}

public enum UrgencyLevel
{
    Normal,
    Low,
    Medium,
    High,
    Critical
}
