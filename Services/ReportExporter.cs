using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BatteryLens.Models;

namespace BatteryLens.Services;

public class ReportExporter
{
    public static async Task<string> ExportHtmlReportAsync(BatteryInfo battery, DiagnosticRecommendation recommendation, SystemHardwareInfo? system = null)
    {
        system ??= new SystemHardwareInfo();

        return await Task.Run(() =>
        {
            string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string fileName = $"BatteryLens_{(system.IsDesktop ? "Desktop" : "Laptop")}_Report_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            string filePath = Path.Combine(desktopDir, fileName);

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("  <meta charset=\"UTF-8\">");
            sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine($"  <title>BatteryLens - {system.RoleString} Power &amp; Hardware Report</title>");
            sb.AppendLine("  <style>");
            sb.AppendLine("    :root { --bg: #0b0f19; --card: #111827; --card-alt: #1f2937; --text: #f8fafc; --muted: #94a3b8; --accent: #38bdf8; --border: #374151; }");
            sb.AppendLine("    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background: var(--bg); color: var(--text); padding: 32px; margin: 0; }");
            sb.AppendLine("    .container { max-width: 920px; margin: 0 auto; }");
            sb.AppendLine("    .header { display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid var(--border); padding-bottom: 20px; margin-bottom: 26px; }");
            sb.AppendLine("    .brand { font-size: 26px; font-weight: 800; color: #38bdf8; display: flex; align-items: center; gap: 10px; }");
            sb.AppendLine("    .badge { display: inline-block; padding: 6px 14px; border-radius: 9999px; font-size: 13px; font-weight: 700; text-transform: uppercase; }");
            sb.AppendLine("    .verdict-box { background: rgba(56, 189, 248, 0.08); border-left: 6px solid #38bdf8; border-radius: 10px; padding: 22px; margin-bottom: 26px; }");
            sb.AppendLine("    .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 18px; margin-bottom: 26px; }");
            sb.AppendLine("    .card { background: var(--card); border: 1px solid var(--border); border-radius: 12px; padding: 20px; }");
            sb.AppendLine("    .metric-value { font-size: 30px; font-weight: 800; margin: 8px 0 4px; color: #38bdf8; }");
            sb.AppendLine("    .metric-label { font-size: 12px; color: var(--muted); text-transform: uppercase; letter-spacing: 0.5px; font-weight: 700; }");
            sb.AppendLine("    .bar-wrap { background: #334155; height: 18px; border-radius: 8px; overflow: hidden; margin: 15px 0 10px; display: flex; }");
            sb.AppendLine("    .bar-actual { background: #10b981; height: 100%; }");
            sb.AppendLine("    .bar-wear { background: #ef4444; height: 100%; }");
            sb.AppendLine("    table { width: 100%; border-collapse: collapse; margin-top: 15px; }");
            sb.AppendLine("    th, td { text-align: left; padding: 12px 14px; border-bottom: 1px solid var(--border); font-size: 13.5px; }");
            sb.AppendLine("    th { color: var(--muted); }");
            sb.AppendLine("    .tag-pass { color: #10b981; font-weight: 600; }");
            sb.AppendLine("    .tag-warn { color: #f59e0b; font-weight: 600; }");
            sb.AppendLine("    .tag-fail { color: #ef4444; font-weight: 600; }");
            sb.AppendLine("    .action-list { padding-left: 20px; line-height: 1.6; }");
            sb.AppendLine("    .footer { text-align: center; color: var(--muted); font-size: 12px; margin-top: 40px; border-top: 1px solid var(--border); padding-top: 20px; }");
            sb.AppendLine("    @media print { body { background: white; color: black; } .card, .verdict-box { border: 1px solid #ccc; background: white; color: black; } }");
            sb.AppendLine("  </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("  <div class=\"container\">");

            // Header
            sb.AppendLine("    <div class=\"header\">");
            sb.AppendLine("      <div>");
            sb.AppendLine("        <div class=\"brand\">⚡ BatteryLens</div>");
            sb.AppendLine($"        <div style=\"color: var(--muted); margin-top: 4px;\">{system.RoleString} Power Subsystem &amp; Health Analysis</div>");
            sb.AppendLine("      </div>");
            sb.AppendLine($"      <div style=\"text-align: right; color: var(--muted); font-size: 13px;\">Host: <strong>{system.ComputerName}</strong><br>Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</div>");
            sb.AppendLine("    </div>");

            // Verdict Box
            string urgencyBg = recommendation.Urgency switch
            {
                UrgencyLevel.Critical => "#ef4444",
                UrgencyLevel.High => "#f97316",
                UrgencyLevel.Medium => "#f59e0b",
                UrgencyLevel.Low => "#3b82f6",
                _ => "#10b981"
            };

            sb.AppendLine($"    <div class=\"verdict-box\" style=\"border-color: {urgencyBg};\">");
            sb.AppendLine($"      <div style=\"display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px;\">");
            sb.AppendLine($"        <span class=\"badge\" style=\"background: {urgencyBg}; color: white;\">{recommendation.Verdict}</span>");
            sb.AppendLine($"        <span style=\"font-size: 13px; color: var(--muted);\">Diagnostic Confidence: {recommendation.ConfidenceScore}%</span>");
            sb.AppendLine($"      </div>");
            sb.AppendLine($"      <h2 style=\"margin: 0 0 8px 0; font-size: 22px;\">{recommendation.Headline}</h2>");
            sb.AppendLine($"      <p style=\"color: #cbd5e1; margin: 0 0 12px 0; font-size: 15px; font-weight: 500;\">{recommendation.Subtitle}</p>");
            sb.AppendLine($"      <p style=\"color: var(--muted); margin: 0; font-size: 14px; line-height: 1.5;\">{recommendation.SummaryText}</p>");
            sb.AppendLine("    </div>");

            // Host System Hardware Summary Card
            sb.AppendLine("    <div class=\"card\" style=\"margin-bottom: 24px;\">");
            sb.AppendLine("      <div class=\"metric-label\">HOST COMPUTER ARCHITECTURE &amp; TELEMETRY</div>");
            sb.AppendLine("      <div style=\"display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 14px; margin-top: 12px;\">");
            sb.AppendLine($"        <div><span style=\"color: var(--muted); font-size: 12px;\">Form Factor</span><br><strong>{system.RoleString} ({system.ChassisType})</strong></div>");
            sb.AppendLine($"        <div><span style=\"color: var(--muted); font-size: 12px;\">Motherboard</span><br><strong>{system.MotherboardManufacturer} {system.MotherboardProduct}</strong></div>");
            sb.AppendLine($"        <div><span style=\"color: var(--muted); font-size: 12px;\">Processor</span><br><strong>{system.ProcessorName}</strong></div>");
            sb.AppendLine($"        <div><span style=\"color: var(--muted); font-size: 12px;\">RAM &amp; Power Plan</span><br><strong>{system.TotalPhysicalMemoryGb:F0} GB | {system.ActivePowerPlan}</strong></div>");
            sb.AppendLine("      </div>");
            sb.AppendLine("    </div>");

            // Decision Trio (Do you need a Charger, Battery, or PC?)
            sb.AppendLine("    <h3 style=\"margin-top: 24px; margin-bottom: 14px;\">🎯 Component Assessment: Do You Need Replacement?</h3>");
            sb.AppendLine("    <div class=\"grid\">");

            // Charger / PSU
            sb.AppendLine("      <div class=\"card\">");
            sb.AppendLine($"        <div class=\"metric-label\">{recommendation.ChargerVerdict.ComponentName}</div>");
            sb.AppendLine($"        <div style=\"color: {recommendation.ChargerVerdict.StatusBadgeColor}; font-weight: 800; font-size: 18px; margin: 8px 0;\">{recommendation.ChargerVerdict.ActionRecommendation}</div>");
            sb.AppendLine($"        <div style=\"font-size: 13px; color: var(--muted); line-height: 1.4;\">{recommendation.ChargerVerdict.Reason}</div>");
            sb.AppendLine($"        <div style=\"margin-top: 12px; font-size: 12px; color: #38bdf8;\">Est. Cost: <strong>{recommendation.ChargerVerdict.CostEstimate}</strong></div>");
            sb.AppendLine("      </div>");

            // Battery
            sb.AppendLine("      <div class=\"card\">");
            sb.AppendLine($"        <div class=\"metric-label\">{recommendation.BatteryVerdict.ComponentName}</div>");
            sb.AppendLine($"        <div style=\"color: {recommendation.BatteryVerdict.StatusBadgeColor}; font-weight: 800; font-size: 18px; margin: 8px 0;\">{recommendation.BatteryVerdict.ActionRecommendation}</div>");
            sb.AppendLine($"        <div style=\"font-size: 13px; color: var(--muted); line-height: 1.4;\">{recommendation.BatteryVerdict.Reason}</div>");
            sb.AppendLine($"        <div style=\"margin-top: 12px; font-size: 12px; color: #38bdf8;\">Est. Cost: <strong>{recommendation.BatteryVerdict.CostEstimate}</strong></div>");
            sb.AppendLine("      </div>");

            // Laptop/PC
            sb.AppendLine("      <div class=\"card\">");
            sb.AppendLine($"        <div class=\"metric-label\">{recommendation.PcVerdict.ComponentName}</div>");
            sb.AppendLine($"        <div style=\"color: {recommendation.PcVerdict.StatusBadgeColor}; font-weight: 800; font-size: 18px; margin: 8px 0;\">{recommendation.PcVerdict.ActionRecommendation}</div>");
            sb.AppendLine($"        <div style=\"font-size: 13px; color: var(--muted); line-height: 1.4;\">{recommendation.PcVerdict.Reason}</div>");
            sb.AppendLine($"        <div style=\"margin-top: 12px; font-size: 12px; color: #38bdf8;\">Est. Cost: <strong>{recommendation.PcVerdict.CostEstimate}</strong></div>");
            sb.AppendLine("      </div>");

            sb.AppendLine("    </div>");

            // Capacity Comparison (only if physical battery is present)
            if (battery.HasPhysicalBattery)
            {
                sb.AppendLine("    <div class=\"card\" style=\"margin-bottom: 26px;\">");
                sb.AppendLine("      <h4 style=\"margin: 0 0 10px 0;\">Original Design vs. Current Usable Capacity</h4>");
                sb.AppendLine("      <div style=\"display: flex; justify-content: space-between; font-size: 14px;\">");
                sb.AppendLine($"        <span>Full Charge Capacity: <strong>{battery.FullChargeCapacityMwh:N0} mWh</strong></span>");
                sb.AppendLine($"        <span>Original Design: <strong>{battery.DesignCapacityMwh:N0} mWh</strong></span>");
                sb.AppendLine("      </div>");
                sb.AppendLine("      <div class=\"bar-wrap\">");
                sb.AppendLine($"        <div class=\"bar-actual\" style=\"width: {battery.HealthPercentage}%;\" title=\"Usable: {battery.HealthPercentage}%\"></div>");
                sb.AppendLine($"        <div class=\"bar-wear\" style=\"width: {battery.WearLevelPercentage}%;\" title=\"Wear Gap: {battery.WearLevelPercentage}%\"></div>");
                sb.AppendLine("      </div>");
                sb.AppendLine($"      <div style=\"font-size: 12px; color: var(--muted); text-align: right;\">Permanent Degradation: -{(battery.DesignCapacityMwh - battery.FullChargeCapacityMwh):N0} mWh ({battery.WearLevelPercentage:F1}% lost)</div>");
                sb.AppendLine("    </div>");
            }

            // Diagnostics Checklist Table
            sb.AppendLine("    <div class=\"card\" style=\"margin-bottom: 26px;\">");
            sb.AppendLine("      <h4 style=\"margin: 0 0 10px 0;\">Detailed Diagnostic Subsystem Checklist</h4>");
            sb.AppendLine("      <table>");
            sb.AppendLine("        <thead><tr><th>Category</th><th>Sensor / Parameter</th><th>Measured Value</th><th>Condition</th></tr></thead>");
            sb.AppendLine("        <tbody>");
            foreach (var check in recommendation.Checks)
            {
                string tagClass = check.Severity == "Critical" ? "tag-fail" : (check.Severity == "Warning" ? "tag-warn" : "tag-pass");
                sb.AppendLine($"          <tr><td>{check.Category}</td><td><strong>{check.Name}</strong></td><td>{check.Value}</td><td class=\"{tagClass}\">{check.Assessment}</td></tr>");
            }
            sb.AppendLine("        </tbody>");
            sb.AppendLine("      </table>");
            sb.AppendLine("    </div>");

            // Recommended Action Steps
            if (recommendation.ActionSteps.Count > 0)
            {
                sb.AppendLine("    <div class=\"card\" style=\"margin-bottom: 26px;\">");
                sb.AppendLine("      <h4 style=\"margin: 0 0 10px 0;\">Recommended Next Steps</h4>");
                sb.AppendLine("      <ul class=\"action-list\">");
                foreach (var step in recommendation.ActionSteps)
                {
                    sb.AppendLine($"        <li style=\"margin-bottom: 8px;\">{step}</li>");
                }
                sb.AppendLine("      </ul>");
                sb.AppendLine("    </div>");
            }

            // Footer
            sb.AppendLine("    <div class=\"footer\">");
            sb.AppendLine("      Generated by BatteryLens — Windows 11 Power &amp; Hardware Health Analyzer.<br>");
            sb.AppendLine("      Powered by WMI, ACPI, and Windows Kernel Power Subsystem.");
            sb.AppendLine("    </div>");

            sb.AppendLine("  </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            File.WriteAllText(filePath, sb.ToString());
            return filePath;
        });
    }
}
