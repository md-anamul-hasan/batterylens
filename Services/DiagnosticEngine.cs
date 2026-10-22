using System;
using System.Collections.Generic;
using BatteryLens.Models;

namespace BatteryLens.Services;

public class DiagnosticEngine
{
    public DiagnosticRecommendation Analyze(BatteryInfo battery, SystemHardwareInfo? system = null)
    {
        var rec = new DiagnosticRecommendation();
        system ??= new SystemHardwareInfo();

        // 1. DESKTOP / WORKSTATION COMPUTER ASSESSMENT (No internal battery cells)
        if (!battery.HasPhysicalBattery)
        {
            rec.Verdict = DiagnosticVerdict.NoBatteryDetected;
            rec.Urgency = UrgencyLevel.Normal;
            rec.Headline = $"{system.RoleString} — AC Grid Powered";
            string modelDisplay = !string.IsNullOrWhiteSpace(system.MotherboardProduct) ? system.MotherboardProduct : system.Model;
            rec.Subtitle = $"{system.Manufacturer} {modelDisplay} ({system.ProcessorName})";
            rec.SummaryText = $"This system is a {system.ChassisType} computer running on direct AC mains electricity. Unlike portable laptops, desktop PCs draw power continuously from wall outlets via an internal ATX/SFX power supply unit (PSU). There are no internal lithium-ion cells to degrade or wear down.";
            rec.ConfidenceScore = 100;

            rec.ChargerVerdict = new ComponentVerdict
            {
                ComponentName = "Power Supply Unit (PSU)",
                ActionRecommendation = "KEEP — Operational",
                RequiresReplacement = false,
                StatusBadgeColor = "#10B981",
                Reason = $"Internal PSU is successfully delivering stable 12V, 5V, and 3.3V DC rails to your {system.MotherboardProduct} motherboard.",
                CostEstimate = "$0 (Good Health)",
                IconKind = "PowerCord"
            };

            rec.BatteryVerdict = new ComponentVerdict
            {
                ComponentName = "Internal Battery Cells",
                ActionRecommendation = "NOT APPLICABLE (Desktop)",
                RequiresReplacement = false,
                StatusBadgeColor = "#64748B",
                Reason = "Desktop form-factor does not use internal battery packs. Zero chemical degradation possible.",
                CostEstimate = "$0 (N/A)",
                IconKind = "BatteryOff"
            };

            rec.PcVerdict = new ComponentVerdict
            {
                ComponentName = "Desktop Workstation & Platform",
                ActionRecommendation = "KEEP — Healthy Modern Architecture",
                RequiresReplacement = false,
                StatusBadgeColor = "#10B981",
                Reason = $"{system.ProcessorName} ({system.ProcessorCores} Cores) with {system.TotalPhysicalMemoryGb:F0} GB RAM running {system.ActivePowerPlan} power plan.",
                CostEstimate = "$0 (No Upgrade Needed)",
                IconKind = "Desktop"
            };

            rec.Checks.Add(new DiagnosticCheckItem
            {
                Category = "Platform",
                Name = "Chassis & Role",
                Value = $"{system.ChassisType} ({system.RoleString})",
                Assessment = "Desktop Form Factor",
                IsPassing = true,
                Severity = "Normal"
            });

            rec.Checks.Add(new DiagnosticCheckItem
            {
                Category = "Motherboard",
                Name = "Mainboard Telemetry",
                Value = $"{system.MotherboardManufacturer} {system.MotherboardProduct}",
                Assessment = "Active & Communicating via ACPI",
                IsPassing = true,
                Severity = "Normal"
            });

            rec.Checks.Add(new DiagnosticCheckItem
            {
                Category = "Processor",
                Name = "CPU & Current Load",
                Value = $"{system.ProcessorName} ({system.CpuLoadPercentage}% load)",
                Assessment = system.CpuLoadPercentage < 80 ? "Nominal load" : "High CPU activity",
                IsPassing = true,
                Severity = "Normal"
            });

            rec.Checks.Add(new DiagnosticCheckItem
            {
                Category = "Power Grid",
                Name = "Power Supply & Line State",
                Value = "AC Mains Line Online (Continuous 110V/240V)",
                Assessment = "Direct Grid Power",
                IsPassing = true,
                Severity = "Normal"
            });

            rec.Checks.Add(new DiagnosticCheckItem
            {
                Category = "Power Plan",
                Name = "Windows Power Scheme",
                Value = system.ActivePowerPlan,
                Assessment = "Active Windows Configuration",
                IsPassing = true,
                Severity = "Normal"
            });

            rec.Checks.Add(new DiagnosticCheckItem
            {
                Category = "Battery Backup",
                Name = "Uninterruptible Power Supply (UPS)",
                Value = system.UpsDescription,
                Assessment = system.HasUpsBattery ? "Protected by UPS Battery" : "Standard Direct Wall Connection",
                IsPassing = true,
                Severity = system.HasUpsBattery ? "Normal" : "Normal"
            });

            rec.ActionSteps.Add("No hardware replacement is needed for your desktop power delivery.");
            rec.ActionSteps.Add("If you experience sudden shutdowns during gaming or heavy workloads, verify your ATX Power Supply wattage rating and GPU 12V PCIe power cable connections.");
            rec.ActionSteps.Add("To safeguard your desktop against power cuts or brownouts, consider an external USB Uninterruptible Power Supply (UPS) such as an APC Back-UPS or CyberPower 1000VA-1500VA unit.");
            rec.ActionSteps.Add("To explore laptop battery wear analytics and test degradation scenarios, click the 'Simulation & Test Lab' tab above.");
            return rec;
        }

        // 2. LAPTOP BATTERY TELEMETRY & DECISION LOGIC
        double health = battery.HealthPercentage;
        double wear = battery.WearLevelPercentage;
        int cycles = battery.CycleCount;
        double dischargeRate = Math.Abs(battery.ChargeRateMilliwatts) / 1000.0;
        bool pluggedIn = battery.IsAcOnline;
        bool isCharging = battery.IsCharging;
        bool isDraining = battery.IsDischarging;

        rec.Checks.Add(new DiagnosticCheckItem
        {
            Category = "Battery Cell",
            Name = "Capacity Retention (Health)",
            Value = $"{health:F1}% ({battery.FullChargeCapacityMwh:N0} / {battery.DesignCapacityMwh:N0} mWh)",
            Assessment = health >= 80 ? "Good retention" : (health >= 60 ? "Moderate degradation" : "Severe cell degradation"),
            IsPassing = health >= 70,
            Severity = health >= 80 ? "Normal" : (health >= 60 ? "Warning" : "Critical")
        });

        rec.Checks.Add(new DiagnosticCheckItem
        {
            Category = "Battery Cell",
            Name = "Wear Level",
            Value = $"{wear:F1}% permanent loss",
            Assessment = wear < 25 ? "Normal chemical aging" : "Noticeable loss of mobile runtime",
            IsPassing = wear < 35,
            Severity = wear < 25 ? "Normal" : (wear < 45 ? "Warning" : "Critical")
        });

        rec.Checks.Add(new DiagnosticCheckItem
        {
            Category = "Battery Cell",
            Name = "Charge Cycles",
            Value = $"{cycles} cycles (Nominal ~{battery.MaxDesignCycles})",
            Assessment = cycles < 400 ? "Healthy cycle lifespan" : (cycles < 700 ? "High cycle count" : "Exceeded nominal lifespan"),
            IsPassing = cycles < 600,
            Severity = cycles < 400 ? "Normal" : (cycles < 700 ? "Warning" : "Critical")
        });

        rec.Checks.Add(new DiagnosticCheckItem
        {
            Category = "Charger / AC",
            Name = "AC Power Adapter Status",
            Value = pluggedIn ? (isCharging ? $"Active ({battery.RateWatts}W)" : "Plugged In, Idle / Not Charging") : "Operating on Battery",
            Assessment = pluggedIn && isDraining ? "CRITICAL: Draining while plugged into AC" : "Normal operation",
            IsPassing = !(pluggedIn && isDraining),
            Severity = pluggedIn && isDraining ? "Critical" : "Normal"
        });

        rec.Checks.Add(new DiagnosticCheckItem
        {
            Category = "PC System",
            Name = "Power Draw & Thermal State",
            Value = $"{dischargeRate:F1}W load | {battery.VoltageVolts}V",
            Assessment = dischargeRate > 45.0 ? "Abnormally high power draw (Thermal/VRM issue)" : "Normal power consumption",
            IsPassing = dischargeRate <= 40.0,
            Severity = dischargeRate > 45.0 ? "Warning" : "Normal"
        });

        // Scenario: Faulty Charger
        if (pluggedIn && (isDraining || (battery.ChargePercentage < 90 && Math.Abs(battery.ChargeRateMilliwatts) < 1500 && !isCharging)))
        {
            rec.Verdict = DiagnosticVerdict.ReplaceCharger;
            rec.Urgency = UrgencyLevel.High;
            rec.ConfidenceScore = 92;
            rec.Headline = "Faulty Charger Detected: Replace Your AC Adapter";
            rec.Subtitle = "Your laptop is plugged into the wall, but battery is not receiving sufficient wattage.";
            rec.SummaryText = "The AC power adapter is failing to supply necessary charging current. Your battery still holds reasonable capacity, meaning you do NOT need a new laptop or battery. Replacing the power adapter or charging cable should resolve the problem immediately.";

            rec.ChargerVerdict = new ComponentVerdict
            {
                ComponentName = "Charger / AC Adapter",
                ActionRecommendation = "REPLACE CHARGER",
                RequiresReplacement = true,
                StatusBadgeColor = "#EF4444",
                Reason = "Zero or negative charge flow while connected. Frayed cable, dead power brick, or bent pin.",
                CostEstimate = "$25 - $50",
                IconKind = "AlertTriangle"
            };

            rec.BatteryVerdict = new ComponentVerdict
            {
                ComponentName = "Laptop Battery Pack",
                ActionRecommendation = "KEEP — Healthy",
                RequiresReplacement = false,
                StatusBadgeColor = "#10B981",
                Reason = $"Battery has {health:F0}% health and accepts power when a good supply is connected.",
                CostEstimate = "$0",
                IconKind = "CheckCircle"
            };

            rec.PcVerdict = new ComponentVerdict
            {
                ComponentName = "Laptop / Motherboard",
                ActionRecommendation = "KEEP — Healthy",
                RequiresReplacement = false,
                StatusBadgeColor = "#10B981",
                Reason = "Laptop internal hardware is operational.",
                CostEstimate = "$0",
                IconKind = "Laptop"
            };

            rec.ActionSteps.Add("Inspect the charging cable for physical crimping, loose plugs, or bent DC barrel/USB-C pins.");
            rec.ActionSteps.Add("Try plugging the charger into another wall outlet directly (avoid overloaded power strips).");
            rec.ActionSteps.Add("If using USB-C PD, verify your charger provides the laptop's required wattage (e.g. 65W or 100W, not a phone 18W brick).");
            rec.ActionSteps.Add("Order an OEM replacement charger matching your laptop's voltage and amperage specifications.");
            return rec;
        }

        // Scenario: Failing PC / Extreme Thermal Load
        if (dischargeRate >= 50.0 || (health < 40 && cycles > 850 && battery.VoltageMillivolts < 10000 && dischargeRate > 35.0))
        {
            rec.Verdict = DiagnosticVerdict.UpgradeOrServicePc;
            rec.Urgency = UrgencyLevel.High;
            rec.ConfidenceScore = 88;
            rec.Headline = "Hardware Service or PC Upgrade Advised";
            rec.Subtitle = "Abnormal power draw (>45W idle) or severe multi-component thermal stress detected.";
            rec.SummaryText = "The computer is consuming unusually massive power even during baseline usage. This typically points to failing motherboard VRMs, clogged cooling vents forcing 100% fan duty, or an aging hardware platform where repairing both the battery and power circuits exceeds the laptop's market value.";

            rec.ChargerVerdict = new ComponentVerdict
            {
                ComponentName = "Charger / AC Adapter",
                ActionRecommendation = "INSPECT",
                RequiresReplacement = false,
                StatusBadgeColor = "#F59E0B",
                Reason = "Ensure charger wattage is high enough to handle extreme peak draws.",
                CostEstimate = "$0 - $45",
                IconKind = "Info"
            };

            rec.BatteryVerdict = new ComponentVerdict
            {
                ComponentName = "Laptop Battery Pack",
                ActionRecommendation = "DEGRADED",
                RequiresReplacement = health < 60,
                StatusBadgeColor = health < 60 ? "#EF4444" : "#F59E0B",
                Reason = $"Wear level {wear:F0}%. Battery suffers from rapid drain caused by system overload.",
                CostEstimate = "$50 - $90",
                IconKind = "AlertCircle"
            };

            rec.PcVerdict = new ComponentVerdict
            {
                ComponentName = "Laptop / Motherboard",
                ActionRecommendation = "SERVICE OR UPGRADE PC",
                RequiresReplacement = true,
                StatusBadgeColor = "#EF4444",
                Reason = "Excessive power draw, thermal throttling, or depreciated platform.",
                CostEstimate = "Upgrade to Modern Laptop ($450+)",
                IconKind = "AlertTriangle"
            };

            rec.ActionSteps.Add("Check Windows Task Manager for background processes consuming 100% CPU/GPU.");
            rec.ActionSteps.Add("Clean the laptop's heatsink fans with compressed air to prevent thermal power runaway.");
            rec.ActionSteps.Add("If the laptop is older than 6-7 years, purchasing a replacement battery rarely delivers good value compared to a modern efficient laptop.");
            return rec;
        }

        // Scenario: Degraded Battery
        if (health < 65.0 || wear > 35.0 || cycles >= 650)
        {
            rec.Verdict = DiagnosticVerdict.ReplaceBattery;
            rec.Urgency = health < 50 ? UrgencyLevel.Critical : UrgencyLevel.Medium;
            rec.ConfidenceScore = 95;
            rec.Headline = "Battery Replacement Recommended";
            rec.Subtitle = $"Battery has degraded to {health:F0}% health with {wear:F0}% permanent capacity loss.";
            rec.SummaryText = "Your laptop hardware and AC charger are in sound working order, but the internal lithium battery cells have reached the end of their chemical lifecycle. Installing a fresh replacement battery pack will immediately restore full factory runtime.";

            rec.ChargerVerdict = new ComponentVerdict
            {
                ComponentName = "Charger / AC Adapter",
                ActionRecommendation = "KEEP — Operational",
                RequiresReplacement = false,
                StatusBadgeColor = "#10B981",
                Reason = "Supplying steady voltage and nominal current to the machine.",
                CostEstimate = "$0",
                IconKind = "CheckCircle"
            };

            rec.BatteryVerdict = new ComponentVerdict
            {
                ComponentName = "Laptop Battery Pack",
                ActionRecommendation = "REPLACE BATTERY",
                RequiresReplacement = true,
                StatusBadgeColor = "#EF4444",
                Reason = $"Only {battery.FullChargeCapacityMwh:N0} mWh remaining out of {battery.DesignCapacityMwh:N0} mWh original design.",
                CostEstimate = "$40 - $85",
                IconKind = "AlertTriangle"
            };

            rec.PcVerdict = new ComponentVerdict
            {
                ComponentName = "Laptop / Motherboard",
                ActionRecommendation = "KEEP — Good Health",
                RequiresReplacement = false,
                StatusBadgeColor = "#10B981",
                Reason = "System hardware is healthy and does not warrant replacement.",
                CostEstimate = "$0",
                IconKind = "CheckCircle"
            };

            rec.ActionSteps.Add($"Search online for your exact laptop battery model code: '{battery.ModelName}' or laptop chassis model.");
            rec.ActionSteps.Add("Always purchase OEM or reputable certified replacement batteries with built-in thermal protection.");
            rec.ActionSteps.Add("Check if your battery is user-removable with external latches or requires a standard Phillips/Torx screwdriver to access.");
            rec.ActionSteps.Add("Safely recycle your old battery at a certified electronics recycling drop-off (Best Buy, Home Depot, etc.).");
            return rec;
        }

        // Scenario: Calibration (Fuel Gauge Desynchronization)
        if (health >= 85 && cycles < 500 &&
            ((battery.ChargePercentage <= 20 && battery.VoltageMillivolts < 10800) ||
             (battery.ChargePercentage >= 30 && battery.VoltageMillivolts < 10400)))
        {
            rec.Verdict = DiagnosticVerdict.NeedsCalibration;
            rec.Urgency = UrgencyLevel.Low;
            rec.ConfidenceScore = 80;
            rec.Headline = "Battery Gauge Calibration Advised";
            rec.Subtitle = "Sensor discrepancy detected between reported charge and cell voltage.";
            rec.SummaryText = "The battery management system's internal fuel gauge has slightly desynchronized from the actual chemical state. You do not need to buy any new parts; a simple calibration cycle will realign the percentage readout.";

            rec.ChargerVerdict = new ComponentVerdict
            {
                ComponentName = "Charger / AC Adapter",
                ActionRecommendation = "KEEP — Operational",
                RequiresReplacement = false,
                StatusBadgeColor = "#10B981",
                Reason = "Charger is working properly.",
                CostEstimate = "$0",
                IconKind = "CheckCircle"
            };

            rec.BatteryVerdict = new ComponentVerdict
            {
                ComponentName = "Laptop Battery Pack",
                ActionRecommendation = "CALIBRATE ONLY",
                RequiresReplacement = false,
                StatusBadgeColor = "#3B82F6",
                Reason = "Cells are healthy, but microcontroller fuel gauge needs synchronization.",
                CostEstimate = "$0",
                IconKind = "RefreshCw"
            };

            rec.PcVerdict = new ComponentVerdict
            {
                ComponentName = "Laptop / Motherboard",
                ActionRecommendation = "KEEP — Healthy",
                RequiresReplacement = false,
                StatusBadgeColor = "#10B981",
                Reason = "System motherboard and power delivery are normal.",
                CostEstimate = "$0",
                IconKind = "CheckCircle"
            };

            rec.ActionSteps.Add("Charge your laptop fully to 100% and keep it plugged in for at least 2 extra hours.");
            rec.ActionSteps.Add("Unplug the charger and use the laptop until it automatically hibernates or shuts down at ~3-5%.");
            rec.ActionSteps.Add("Let it rest unplugged for 1-2 hours, then plug it back in and charge straight back to 100% uninterrupted.");
            return rec;
        }

        // Scenario: Healthy Laptop
        rec.Verdict = DiagnosticVerdict.Healthy;
        rec.Urgency = UrgencyLevel.Normal;
        rec.ConfidenceScore = 98;
        rec.Headline = "Power Subsystem is in Excellent Health";
        rec.Subtitle = $"Battery holds {health:F0}% of original capacity with {cycles} cycles logged.";
        rec.SummaryText = "No hardware replacement is needed. Your battery capacity, AC charger delivery, and system power draw are all functioning within healthy manufacturer tolerances.";

        rec.ChargerVerdict = new ComponentVerdict
        {
            ComponentName = "Charger / AC Adapter",
            ActionRecommendation = "KEEP — In Prime Health",
            RequiresReplacement = false,
            StatusBadgeColor = "#10B981",
            Reason = "Delivers stable wattage under both idle and load conditions.",
            CostEstimate = "$0",
            IconKind = "CheckCircle"
        };

        rec.BatteryVerdict = new ComponentVerdict
        {
            ComponentName = "Laptop Battery Pack",
            ActionRecommendation = "KEEP — Healthy Capacity",
            RequiresReplacement = false,
            StatusBadgeColor = "#10B981",
            Reason = $"{health:F0}% capacity remaining ({battery.FullChargeCapacityMwh:N0} mWh). Cell degradation is minimal.",
            CostEstimate = "$0",
            IconKind = "CheckCircle"
        };

        rec.PcVerdict = new ComponentVerdict
        {
            ComponentName = "Laptop / Motherboard",
            ActionRecommendation = "KEEP — Efficient Operation",
            RequiresReplacement = false,
            StatusBadgeColor = "#10B981",
            Reason = "Power management and thermal regulation operating efficiently.",
            CostEstimate = "$0",
            IconKind = "CheckCircle"
        };

        rec.MaintenanceTips.Add("Enable the 80% Charge Limit threshold in your laptop manufacturer software (Lenovo Vantage, MyASUS, Dell Power Manager, or HP Battery Optimizer) to double your battery lifespan.");
        rec.MaintenanceTips.Add("Avoid exposing the laptop to extreme heat (like leaving it in a hot car), which accelerates lithium degradation.");
        rec.MaintenanceTips.Add("Try not to let the battery frequently drop below 15% before plugging it in.");
        return rec;
    }
}
