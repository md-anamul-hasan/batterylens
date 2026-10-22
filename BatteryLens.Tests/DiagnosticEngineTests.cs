using System;
using System.IO;
using System.Threading.Tasks;
using BatteryLens.Models;
using BatteryLens.Services;
using Xunit;

namespace BatteryLens.Tests;

public class DiagnosticEngineTests
{
    private readonly DiagnosticEngine _engine = new();

    [Fact]
    public void Healthy_Battery_Yields_Optimal_Verdict()
    {
        var battery = new BatteryInfo
        {
            HasPhysicalBattery = true,
            DesignCapacityMwh = 56000,
            FullChargeCapacityMwh = 52000, // 92.8%
            CurrentCapacityMwh = 45000,
            CycleCount = 45,
            IsAcOnline = true,
            IsCharging = true,
            IsDischarging = false,
            ChargeRateMilliwatts = 25000,
            VoltageMillivolts = 11400
        };

        var rec = _engine.Analyze(battery);

        Assert.Equal(DiagnosticVerdict.Healthy, rec.Verdict);
        Assert.False(rec.ChargerVerdict.RequiresReplacement);
        Assert.False(rec.BatteryVerdict.RequiresReplacement);
        Assert.False(rec.PcVerdict.RequiresReplacement);
        Assert.True(rec.ConfidenceScore >= 90);
    }

    [Fact]
    public void Degraded_Battery_Under_65_Percent_Health_Recommends_Battery_Replacement()
    {
        var battery = new BatteryInfo
        {
            HasPhysicalBattery = true,
            DesignCapacityMwh = 50000,
            FullChargeCapacityMwh = 24000, // 48% Health, 52% Wear
            CurrentCapacityMwh = 15000,
            CycleCount = 680,
            IsAcOnline = false,
            IsCharging = false,
            IsDischarging = true,
            ChargeRateMilliwatts = -15000,
            VoltageMillivolts = 10500
        };

        var rec = _engine.Analyze(battery);

        Assert.Equal(DiagnosticVerdict.ReplaceBattery, rec.Verdict);
        Assert.True(rec.BatteryVerdict.RequiresReplacement);
        Assert.False(rec.ChargerVerdict.RequiresReplacement);
        Assert.False(rec.PcVerdict.RequiresReplacement);
        Assert.Contains("Replace", rec.BatteryVerdict.ActionRecommendation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Faulty_Charger_Draining_On_AC_Recommends_Charger_Replacement()
    {
        var battery = new BatteryInfo
        {
            HasPhysicalBattery = true,
            DesignCapacityMwh = 60000,
            FullChargeCapacityMwh = 54000, // 90% Health
            CurrentCapacityMwh = 18000,
            CycleCount = 120,
            IsAcOnline = true, // Plugged in
            IsCharging = false,
            IsDischarging = true, // But draining!
            ChargeRateMilliwatts = 0,
            VoltageMillivolts = 10800
        };

        var rec = _engine.Analyze(battery);

        Assert.Equal(DiagnosticVerdict.ReplaceCharger, rec.Verdict);
        Assert.True(rec.ChargerVerdict.RequiresReplacement);
        Assert.False(rec.BatteryVerdict.RequiresReplacement);
        Assert.Contains("REPLACE CHARGER", rec.ChargerVerdict.ActionRecommendation);
    }

    [Fact]
    public void Massive_Idle_Power_Draw_Recommends_PC_Service_Or_Upgrade()
    {
        var battery = new BatteryInfo
        {
            HasPhysicalBattery = true,
            DesignCapacityMwh = 60000,
            FullChargeCapacityMwh = 22000,
            CurrentCapacityMwh = 10000,
            CycleCount = 910,
            IsAcOnline = false,
            IsCharging = false,
            IsDischarging = true,
            ChargeRateMilliwatts = -65000, // 65W abnormal discharge load
            VoltageMillivolts = 9800
        };

        var rec = _engine.Analyze(battery);

        Assert.Equal(DiagnosticVerdict.UpgradeOrServicePc, rec.Verdict);
        Assert.True(rec.PcVerdict.RequiresReplacement);
        Assert.Contains("SERVICE OR UPGRADE", rec.PcVerdict.ActionRecommendation);
    }

    [Fact]
    public void No_Physical_Battery_Yields_Desktop_Verdict()
    {
        var battery = new BatteryInfo
        {
            HasPhysicalBattery = false
        };

        var rec = _engine.Analyze(battery);

        Assert.Equal(DiagnosticVerdict.NoBatteryDetected, rec.Verdict);
        Assert.False(rec.BatteryVerdict.RequiresReplacement);
        Assert.Contains("Desktop", rec.Headline);
    }

    [Fact]
    public void Capacity_Metrics_Calculate_Accurately()
    {
        var battery = new BatteryInfo
        {
            DesignCapacityMwh = 50000,
            FullChargeCapacityMwh = 40000,
            CurrentCapacityMwh = 20000,
            CycleCount = 250,
            MaxDesignCycles = 500
        };

        Assert.Equal(80.0, battery.HealthPercentage);
        Assert.Equal(20.0, battery.WearLevelPercentage);
        Assert.Equal(50.0, battery.ChargePercentage);
        Assert.Equal(50.0, battery.CycleLifespanRemainingPercentage);
    }

    [Fact]
    public void Default_Presets_Are_All_Valid()
    {
        var presets = BatteryPreset.GetDefaultPresets();
        Assert.NotEmpty(presets);
        Assert.True(presets.Length >= 5);

        foreach (var preset in presets)
        {
            Assert.False(string.IsNullOrWhiteSpace(preset.Name));
            Assert.False(string.IsNullOrWhiteSpace(preset.Description));
            Assert.True(preset.Info.DesignCapacityMwh > 0);
            Assert.True(preset.Info.FullChargeCapacityMwh > 0);

            var rec = _engine.Analyze(preset.Info);
            Assert.NotNull(rec);
            Assert.NotEmpty(rec.Headline);
        }
    }

    [Fact]
    public async Task ReportExporter_Generates_Valid_Html_File()
    {
        var preset = BatteryPreset.GetDefaultPresets()[0];
        var rec = _engine.Analyze(preset.Info);

        string path = await ReportExporter.ExportHtmlReportAsync(preset.Info, rec);

        Assert.True(File.Exists(path));
        string html = File.ReadAllText(path);
        Assert.Contains("BatteryLens", html);
        Assert.Contains("Original Design vs. Current Usable Capacity", html);
        Assert.Contains("Component Assessment", html);

        // Clean up
        try { File.Delete(path); } catch { }
    }

    [Fact]
    public void Desktop_PC_Audit_Correctly_Identifies_Hardware_And_AC_Mains()
    {
        var battery = new BatteryInfo { HasPhysicalBattery = false };
        var system = new SystemHardwareInfo
        {
            Manufacturer = "Gigabyte Technology Co., Ltd.",
            MotherboardProduct = "B760M DS3H AX",
            ProcessorName = "12th Gen Intel Core i7-12700K",
            Role = FormFactorRole.Desktop,
            TotalPhysicalMemoryGb = 32.0,
            ActivePowerPlan = "Balanced"
        };

        var rec = _engine.Analyze(battery, system);

        Assert.Equal(DiagnosticVerdict.NoBatteryDetected, rec.Verdict);
        Assert.False(rec.ChargerVerdict.RequiresReplacement);
        Assert.False(rec.BatteryVerdict.RequiresReplacement);
        Assert.False(rec.PcVerdict.RequiresReplacement);
        Assert.Contains("B760M DS3H AX", rec.Subtitle);
        Assert.Contains("i7-12700K", rec.Subtitle);
        Assert.Contains("Desktop", rec.Headline);
    }

    [Fact]
    public void Gauge_Desync_Preset_Yields_NeedsCalibration_Verdict()
    {
        var presets = BatteryPreset.GetDefaultPresets();
        var desynced = Array.Find(presets, p => p.Id == "desynced_gauge");
        Assert.NotNull(desynced);

        var rec = _engine.Analyze(desynced.Info);

        Assert.Equal(DiagnosticVerdict.NeedsCalibration, rec.Verdict);
        Assert.False(rec.ChargerVerdict.RequiresReplacement);
        Assert.False(rec.BatteryVerdict.RequiresReplacement);
        Assert.False(rec.PcVerdict.RequiresReplacement);
        Assert.Contains("CALIBRATE", rec.BatteryVerdict.ActionRecommendation);
        Assert.Equal(UrgencyLevel.Low, rec.Urgency);
    }

    [Fact]
    public void BatteryHistoryPoint_Exposes_Accurate_State_And_Color()
    {
        var chargingPt = new BatteryHistoryPoint
        {
            Timestamp = DateTime.Now,
            CapacityMwh = 42000,
            Percentage = 84.0,
            IsCharging = true
        };

        Assert.Equal("Charging", chargingPt.StateString);
        Assert.Equal("#10B981", chargingPt.StateColor);

        var dischargingPt = new BatteryHistoryPoint
        {
            Timestamp = DateTime.Now,
            CapacityMwh = 35000,
            Percentage = 70.0,
            IsCharging = false
        };

        Assert.Equal("Discharging", dischargingPt.StateString);
        Assert.Equal("#38BDF8", dischargingPt.StateColor);
    }
}
