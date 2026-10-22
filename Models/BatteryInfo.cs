using System;
using System.Collections.Generic;

namespace BatteryLens.Models;

public class BatteryInfo
{
    public bool HasPhysicalBattery { get; set; } = true;
    public string DeviceName { get; set; } = "Internal Battery";
    public string Manufacturer { get; set; } = "OEM Manufacturer";
    public string SerialNumber { get; set; } = "N/A";
    public string Chemistry { get; set; } = "Lithium-Ion";
    public string ModelName { get; set; } = "Standard Battery Pack";

    // Capacities in milliWatt-hours (mWh)
    public double DesignCapacityMwh { get; set; } = 54000;
    public double FullChargeCapacityMwh { get; set; } = 50000;
    public double CurrentCapacityMwh { get; set; } = 45000;

    // Percentages (0 - 100)
    public double ChargePercentage
        => FullChargeCapacityMwh > 0
            ? Math.Min(100.0, Math.Max(0.0, Math.Round((CurrentCapacityMwh / FullChargeCapacityMwh) * 100.0, 1)))
            : 0;

    public double HealthPercentage
        => DesignCapacityMwh > 0
            ? Math.Min(100.0, Math.Max(0.0, Math.Round((FullChargeCapacityMwh / DesignCapacityMwh) * 100.0, 1)))
            : 0;

    public double WearLevelPercentage
        => Math.Max(0.0, Math.Round(100.0 - HealthPercentage, 1));

    // Cycle Count
    public int CycleCount { get; set; } = 85;
    public int MaxDesignCycles { get; set; } = 500;
    public double CycleLifespanRemainingPercentage => MaxDesignCycles > 0
        ? Math.Max(0.0, Math.Round((1.0 - (double)CycleCount / MaxDesignCycles) * 100.0, 1))
        : 100.0;

    // Power Status
    public bool IsAcOnline { get; set; } = true;
    public string BatteryStatusString { get; set; } = "Charging"; // Charging, Discharging, AC Connected - Idle, Full
    public bool IsCharging { get; set; } = true;
    public bool IsDischarging { get; set; } = false;

    // Electrical Measurements
    public double VoltageMillivolts { get; set; } = 11400; // e.g. 11.4V nominal
    public double ChargeRateMilliwatts { get; set; } = 25000; // Positive for charging, negative for discharge

    public double VoltageVolts => Math.Round(VoltageMillivolts / 1000.0, 2);
    public double RateWatts => Math.Round(Math.Abs(ChargeRateMilliwatts) / 1000.0, 1);

    // Time Estimates
    public TimeSpan? EstimatedRemainingRunTime { get; set; } = TimeSpan.FromHours(4).Add(TimeSpan.FromMinutes(30));
    public TimeSpan? EstimatedTimeToFullCharge { get; set; } = TimeSpan.FromMinutes(45);

    // Temperature (if supported)
    public double? TemperatureCelsius { get; set; } = 28.5;

    // DataSource info
    public string DataSource { get; set; } = "WMI & Windows Power Subsystem";
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    public List<BatteryHistoryPoint> RecentHistory { get; set; } = new();

    public BatteryInfo Clone()
    {
        return new BatteryInfo
        {
            HasPhysicalBattery = this.HasPhysicalBattery,
            DeviceName = this.DeviceName,
            Manufacturer = this.Manufacturer,
            SerialNumber = this.SerialNumber,
            Chemistry = this.Chemistry,
            ModelName = this.ModelName,
            DesignCapacityMwh = this.DesignCapacityMwh,
            FullChargeCapacityMwh = this.FullChargeCapacityMwh,
            CurrentCapacityMwh = this.CurrentCapacityMwh,
            CycleCount = this.CycleCount,
            MaxDesignCycles = this.MaxDesignCycles,
            IsAcOnline = this.IsAcOnline,
            BatteryStatusString = this.BatteryStatusString,
            IsCharging = this.IsCharging,
            IsDischarging = this.IsDischarging,
            VoltageMillivolts = this.VoltageMillivolts,
            ChargeRateMilliwatts = this.ChargeRateMilliwatts,
            EstimatedRemainingRunTime = this.EstimatedRemainingRunTime,
            EstimatedTimeToFullCharge = this.EstimatedTimeToFullCharge,
            TemperatureCelsius = this.TemperatureCelsius,
            DataSource = this.DataSource,
            LastUpdated = this.LastUpdated,
            RecentHistory = new List<BatteryHistoryPoint>(this.RecentHistory)
        };
    }
}

public class BatteryHistoryPoint
{
    public DateTime Timestamp { get; set; }
    public double CapacityMwh { get; set; }
    public double Percentage { get; set; }
    public bool IsCharging { get; set; }

    public string StateString => IsCharging ? "Charging" : "Discharging";
    public string StateColor => IsCharging ? "#10B981" : "#38BDF8";
}
