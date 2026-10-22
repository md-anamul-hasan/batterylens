using System;

namespace BatteryLens.Models;

public class BatteryPreset
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TargetVerdictSummary { get; set; } = string.Empty;
    public BatteryInfo Info { get; set; } = new();

    public static BatteryPreset[] GetDefaultPresets()
    {
        return new[]
        {
            new BatteryPreset
            {
                Id = "healthy",
                Name = "Healthy Ultrabook (Dell XPS 13)",
                Description = "Nearly new condition. 94% health, 52 cycles, stable 35W charging.",
                TargetVerdictSummary = "Keep everything. Battery and charger in prime health.",
                Info = new BatteryInfo
                {
                    HasPhysicalBattery = true,
                    DeviceName = "Dell XPS Battery Pack",
                    Manufacturer = "SMP-SDI4.5",
                    ModelName = "DELL 80K071",
                    SerialNumber = "8391A-2024",
                    Chemistry = "Lithium-Ion Polymer",
                    DesignCapacityMwh = 56000,
                    FullChargeCapacityMwh = 52800,
                    CurrentCapacityMwh = 43500,
                    CycleCount = 52,
                    MaxDesignCycles = 500,
                    IsAcOnline = true,
                    IsCharging = true,
                    IsDischarging = false,
                    BatteryStatusString = "Charging (Fast AC)",
                    VoltageMillivolts = 11850,
                    ChargeRateMilliwatts = 35200,
                    EstimatedRemainingRunTime = TimeSpan.FromHours(6).Add(TimeSpan.FromMinutes(45)),
                    EstimatedTimeToFullCharge = TimeSpan.FromMinutes(22),
                    TemperatureCelsius = 29.2,
                    DataSource = "Simulated Hardware Profile"
                }
            },
            new BatteryPreset
            {
                Id = "degraded_battery",
                Name = "Severe Battery Wear (ThinkPad T480)",
                Description = "Worn out battery: 32% health, 780 cycles. Rapid discharge, laptop dies in 35 mins.",
                TargetVerdictSummary = "Replace Battery ($45-$75). Charger & PC are fine.",
                Info = new BatteryInfo
                {
                    HasPhysicalBattery = true,
                    DeviceName = "Lenovo ThinkPad DualCell",
                    Manufacturer = "LGC-01AV421",
                    ModelName = "01AV421",
                    SerialNumber = "9402B-2020",
                    Chemistry = "Lithium-Ion",
                    DesignCapacityMwh = 48000,
                    FullChargeCapacityMwh = 15360,
                    CurrentCapacityMwh = 9200,
                    CycleCount = 782,
                    MaxDesignCycles = 500,
                    IsAcOnline = false,
                    IsCharging = false,
                    IsDischarging = true,
                    BatteryStatusString = "Discharging (Fast Drain)",
                    VoltageMillivolts = 10400,
                    ChargeRateMilliwatts = -22000, // rapid 22W drain
                    EstimatedRemainingRunTime = TimeSpan.FromMinutes(38),
                    EstimatedTimeToFullCharge = null,
                    TemperatureCelsius = 38.6,
                    DataSource = "Simulated Hardware Profile"
                }
            },
            new BatteryPreset
            {
                Id = "faulty_charger",
                Name = "Faulty Charger / Broken AC Cable (HP Spectre)",
                Description = "AC plugged in, but battery reports 0W charge or keeps draining. Charger cable or brick defective.",
                TargetVerdictSummary = "Replace Charger ($25-$45). Battery health is 87% (good).",
                Info = new BatteryInfo
                {
                    HasPhysicalBattery = true,
                    DeviceName = "HP LongLife Battery",
                    Manufacturer = "Dynapack HP",
                    ModelName = "HSTNN-DB9C",
                    SerialNumber = "4412C-2022",
                    Chemistry = "Lithium-Ion Polymer",
                    DesignCapacityMwh = 60000,
                    FullChargeCapacityMwh = 52200,
                    CurrentCapacityMwh = 12000,
                    CycleCount = 195,
                    MaxDesignCycles = 500,
                    IsAcOnline = true, // Plugged in
                    IsCharging = false, // But not charging!
                    IsDischarging = true, // Still draining on AC
                    BatteryStatusString = "AC Connected - Not Charging (Defective Supply)",
                    VoltageMillivolts = 10800,
                    ChargeRateMilliwatts = 0, // Zero charge rate
                    EstimatedRemainingRunTime = TimeSpan.FromMinutes(42),
                    EstimatedTimeToFullCharge = null,
                    TemperatureCelsius = 27.0,
                    DataSource = "Simulated Hardware Profile"
                }
            },
            new BatteryPreset
            {
                Id = "failing_pc",
                Name = "Severe Thermal / Motherboard Drain (Old Gaming Laptop)",
                Description = "System pulls 68W at idle, internal resistance high, battery degraded, age 7+ years.",
                TargetVerdictSummary = "Consider Upgrading PC. Repair costs exceed laptop value.",
                Info = new BatteryInfo
                {
                    HasPhysicalBattery = true,
                    DeviceName = "ASUS ROG PowerPack",
                    Manufacturer = "Simplo-ASUS",
                    ModelName = "B31N1726",
                    SerialNumber = "1182D-2018",
                    Chemistry = "Lithium-Ion",
                    DesignCapacityMwh = 64000,
                    FullChargeCapacityMwh = 24000,
                    CurrentCapacityMwh = 11000,
                    CycleCount = 920,
                    MaxDesignCycles = 500,
                    IsAcOnline = false,
                    IsCharging = false,
                    IsDischarging = true,
                    BatteryStatusString = "Discharging (Excessive 68W Load)",
                    VoltageMillivolts = 9950,
                    ChargeRateMilliwatts = -68500, // huge 68.5W drain
                    EstimatedRemainingRunTime = TimeSpan.FromMinutes(14),
                    EstimatedTimeToFullCharge = null,
                    TemperatureCelsius = 49.5,
                    DataSource = "Simulated Hardware Profile"
                }
            },
            new BatteryPreset
            {
                Id = "desynced_gauge",
                Name = "Gauge Needs Calibration (MacBook Air via BootCamp)",
                Description = "Full charge capacity reported doesn't match cell voltage. Calibration full cycle required.",
                TargetVerdictSummary = "Calibrate Gauge (Drain to 5%, charge to 100% uninterrupted).",
                Info = new BatteryInfo
                {
                    HasPhysicalBattery = true,
                    DeviceName = "Apple A1965 Battery",
                    Manufacturer = "DP-A1965",
                    ModelName = "A1965",
                    SerialNumber = "C0294-2021",
                    Chemistry = "Lithium-Ion Polymer",
                    DesignCapacityMwh = 49900,
                    FullChargeCapacityMwh = 49000,
                    CurrentCapacityMwh = 8000,
                    CycleCount = 210,
                    MaxDesignCycles = 1000,
                    IsAcOnline = false,
                    IsCharging = false,
                    IsDischarging = true,
                    BatteryStatusString = "Discharging (Voltage Mismatch)",
                    VoltageMillivolts = 10100, // Very low voltage for claimed 8000mWh remaining
                    ChargeRateMilliwatts = -16000,
                    EstimatedRemainingRunTime = TimeSpan.FromMinutes(25),
                    EstimatedTimeToFullCharge = null,
                    TemperatureCelsius = 31.0,
                    DataSource = "Simulated Hardware Profile"
                }
            }
        };
    }
}
