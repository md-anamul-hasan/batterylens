using System;

namespace BatteryLens.Models;

public enum FormFactorRole
{
    Desktop,
    Laptop,
    Workstation,
    Server,
    Tablet,
    VirtualMachine,
    Unknown
}

public class SystemHardwareInfo
{
    public string ComputerName { get; set; } = Environment.MachineName;
    public string Manufacturer { get; set; } = "Unknown Manufacturer";
    public string Model { get; set; } = "Standard PC";
    public FormFactorRole Role { get; set; } = FormFactorRole.Desktop;
    public string RoleString => Role switch
    {
        FormFactorRole.Desktop => "Desktop Workstation / Tower",
        FormFactorRole.Laptop => "Mobile Laptop / Notebook",
        FormFactorRole.Workstation => "High-Performance Workstation",
        FormFactorRole.Server => "Enterprise Server",
        FormFactorRole.Tablet => "Tablet / Convertible PC",
        FormFactorRole.VirtualMachine => "Virtual Machine",
        _ => "Desktop System"
    };

    public string ChassisType { get; set; } = "Desktop Tower";
    public bool IsDesktop => Role == FormFactorRole.Desktop || Role == FormFactorRole.Workstation || Role == FormFactorRole.Server;
    public bool IsLaptop => Role == FormFactorRole.Laptop || Role == FormFactorRole.Tablet;

    // Motherboard
    public string MotherboardManufacturer { get; set; } = "Gigabyte Technology Co., Ltd.";
    public string MotherboardProduct { get; set; } = "B760M DS3H AX";

    // Processor
    public string ProcessorName { get; set; } = "Intel Core Processor";
    public int ProcessorCores { get; set; } = 8;
    public double ProcessorMaxGhz { get; set; } = 3.6;
    public int CpuLoadPercentage { get; set; } = 10;

    // Memory & OS
    public double TotalPhysicalMemoryGb { get; set; } = 32.0;
    public string OsVersion { get; set; } = "Windows 11 (64-bit)";
    public string BiosVersion { get; set; } = "N/A";
    public string BiosDate { get; set; } = "N/A";

    // Power Configuration
    public string ActivePowerPlan { get; set; } = "Balanced";
    public string PowerDeliverySource { get; set; } = "Direct AC Grid (110V - 240V Mains)";
    public string PsuDescription { get; set; } = "ATX Desktop Power Supply (Continuous Wall AC Power)";
    public bool HasUpsBattery { get; set; } = false;
    public string UpsDescription { get; set; } = "No USB/HID Uninterruptible Power Supply (UPS) detected";
}
