using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using BatteryLens.Models;

namespace BatteryLens.Services;

public class HardwareAuditResult
{
    public SystemHardwareInfo System { get; set; } = new();
    public BatteryInfo Battery { get; set; } = new();
}

public class BatteryHardwareReader
{
    public async Task<HardwareAuditResult> AuditSystemAndBatteryAsync()
    {
        return await Task.Run(() =>
        {
            var result = new HardwareAuditResult();
            var sys = result.System;
            var bat = result.Battery;

            // 1. Audit Host Hardware Platform & Chassis
            ReadSystemPlatformInfo(sys);

            // 2. Read Active Windows Power Plan
            ReadActivePowerPlan(sys);

            // 3. Audit Battery / UPS Subsystem via Win32 Native API
            bool nativeSuccess = NativeMethods.GetSystemPowerStatus(out var status);
            if (nativeSuccess)
            {
                if (status.BatteryFlag == 128 || (status.ACLineStatus == 1 && status.BatteryFlag == 255 && status.BatteryLifePercent == 255))
                {
                    // No system battery reported by kernel
                    bat.HasPhysicalBattery = false;
                    bat.IsAcOnline = true;
                    bat.BatteryStatusString = "Direct AC Grid (No Battery)";
                }
                else
                {
                    bat.HasPhysicalBattery = true;
                    bat.IsAcOnline = status.ACLineStatus == 1;
                    bat.IsCharging = (status.BatteryFlag & 8) != 0;
                    bat.IsDischarging = !bat.IsCharging && !bat.IsAcOnline;

                    if (status.BatteryLifeTime > 0)
                    {
                        bat.EstimatedRemainingRunTime = TimeSpan.FromSeconds(status.BatteryLifeTime);
                    }
                }
            }

            // 4. Query WMI root\cimv2 Win32_Battery
            bool foundCimBattery = false;
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery");
                using var collection = searcher.Get();

                foreach (ManagementObject obj in collection)
                {
                    foundCimBattery = true;
                    bat.HasPhysicalBattery = true;

                    bat.DeviceName = obj["Name"]?.ToString() ?? bat.DeviceName;
                    bat.Manufacturer = obj["Manufacturer"]?.ToString() ?? bat.Manufacturer;
                    bat.SerialNumber = obj["DeviceID"]?.ToString() ?? bat.SerialNumber;

                    if (obj["Chemistry"] != null)
                    {
                        int chem = Convert.ToInt32(obj["Chemistry"]);
                        bat.Chemistry = chem switch
                        {
                            1 => "Other",
                            2 => "Unknown",
                            3 => "Lead Acid",
                            4 => "Nickel Cadmium",
                            5 => "Nickel Metal Hydride",
                            6 => "Lithium-ion",
                            7 => "Zinc air",
                            8 => "Lithium Polymer",
                            _ => "Lithium-ion"
                        };
                    }

                    if (obj["DesignCapacity"] != null && Convert.ToDouble(obj["DesignCapacity"]) > 0)
                    {
                        bat.DesignCapacityMwh = Convert.ToDouble(obj["DesignCapacity"]);
                    }

                    if (obj["FullChargeCapacity"] != null && Convert.ToDouble(obj["FullChargeCapacity"]) > 0)
                    {
                        bat.FullChargeCapacityMwh = Convert.ToDouble(obj["FullChargeCapacity"]);
                    }

                    if (obj["EstimatedChargeRemaining"] != null)
                    {
                        double percent = Convert.ToDouble(obj["EstimatedChargeRemaining"]);
                        bat.CurrentCapacityMwh = bat.FullChargeCapacityMwh * (percent / 100.0);
                    }

                    if (obj["EstimatedRunTime"] != null)
                    {
                        uint runTimeMin = Convert.ToUInt32(obj["EstimatedRunTime"]);
                        if (runTimeMin > 0 && runTimeMin < 71155)
                        {
                            bat.EstimatedRemainingRunTime = TimeSpan.FromMinutes(runTimeMin);
                        }
                    }

                    if (obj["BatteryStatus"] != null)
                    {
                        int bStatus = Convert.ToInt32(obj["BatteryStatus"]);
                        bat.BatteryStatusString = bStatus switch
                        {
                            1 => "Discharging",
                            2 => "Connected and charging",
                            3 => "Fully Charged",
                            4 => "Low",
                            5 => "Critical",
                            6 => "Charging",
                            7 => "Charging and High",
                            8 => "Charging and Low",
                            9 => "Charging and Critical",
                            10 => "Undefined",
                            11 => "Partially Charged",
                            _ => "Normal"
                        };
                    }
                    break;
                }
            }
            catch { }

            // 5. Query root\wmi for high-precision ACPI Battery metrics if physical battery exists
            if (foundCimBattery || bat.HasPhysicalBattery)
            {
                try
                {
                    var scope = new ManagementScope(@"\\.\root\wmi");
                    scope.Connect();

                    // Static Data
                    using (var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM BatteryStaticData")))
                    using (var collection = searcher.Get())
                    {
                        foreach (ManagementObject obj in collection)
                        {
                            if (obj["DesignedCapacity"] != null && Convert.ToDouble(obj["DesignedCapacity"]) > 0)
                            {
                                bat.DesignCapacityMwh = Convert.ToDouble(obj["DesignedCapacity"]);
                            }
                            if (obj["DeviceName"] != null)
                            {
                                bat.ModelName = obj["DeviceName"].ToString() ?? bat.ModelName;
                            }
                            if (obj["ManufactureName"] != null)
                            {
                                bat.Manufacturer = obj["ManufactureName"].ToString() ?? bat.Manufacturer;
                            }
                            if (obj["SerialNumber"] != null)
                            {
                                bat.SerialNumber = obj["SerialNumber"].ToString() ?? bat.SerialNumber;
                            }
                            break;
                        }
                    }

                    // Full Charged Capacity
                    using (var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM BatteryFullChargedCapacity")))
                    using (var collection = searcher.Get())
                    {
                        foreach (ManagementObject obj in collection)
                        {
                            if (obj["FullChargedCapacity"] != null && Convert.ToDouble(obj["FullChargedCapacity"]) > 0)
                            {
                                bat.FullChargeCapacityMwh = Convert.ToDouble(obj["FullChargedCapacity"]);
                            }
                            break;
                        }
                    }

                    // Cycle Count
                    using (var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM BatteryCycleCount")))
                    using (var collection = searcher.Get())
                    {
                        foreach (ManagementObject obj in collection)
                        {
                            if (obj["CycleCount"] != null)
                            {
                                bat.CycleCount = Convert.ToInt32(obj["CycleCount"]);
                            }
                            break;
                        }
                    }

                    // Battery Status
                    using (var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM BatteryStatus")))
                    using (var collection = searcher.Get())
                    {
                        foreach (ManagementObject obj in collection)
                        {
                            if (obj["RemainingCapacity"] != null && Convert.ToDouble(obj["RemainingCapacity"]) > 0)
                            {
                                bat.CurrentCapacityMwh = Convert.ToDouble(obj["RemainingCapacity"]);
                            }
                            if (obj["Voltage"] != null && Convert.ToDouble(obj["Voltage"]) > 0)
                            {
                                bat.VoltageMillivolts = Convert.ToDouble(obj["Voltage"]);
                            }
                            if (obj["ChargeRate"] != null)
                            {
                                bat.ChargeRateMilliwatts = Convert.ToDouble(obj["ChargeRate"]);
                            }
                            if (obj["Charging"] != null)
                            {
                                bat.IsCharging = Convert.ToBoolean(obj["Charging"]);
                            }
                            if (obj["Discharging"] != null)
                            {
                                bat.IsDischarging = Convert.ToBoolean(obj["Discharging"]);
                            }
                            if (obj["PowerOnline"] != null)
                            {
                                bat.IsAcOnline = Convert.ToBoolean(obj["PowerOnline"]);
                            }
                            break;
                        }
                    }
                }
                catch { }

                // Try enriching with powercfg XML if cycle count or design capacity is missing
                if (bat.CycleCount <= 0 || bat.DesignCapacityMwh <= 0)
                {
                    TryEnrichFromPowercfgXml(bat);
                }
            }

            // Check if there is an external UPS connected
            CheckForUpsDevice(sys, bat);

            // Desktop vs Laptop Data Source description
            if (!foundCimBattery && !bat.HasPhysicalBattery)
            {
                bat.HasPhysicalBattery = false;
                bat.DeviceName = "Continuous AC Grid Line";
                bat.Manufacturer = sys.Manufacturer;
                bat.ModelName = sys.MotherboardProduct;
                bat.BatteryStatusString = "Direct AC Mains (Desktop PSU)";
                bat.DataSource = $"AC Mains ({sys.RoleString})";
            }
            else
            {
                bat.DataSource = "Windows ACPI & WMI Subsystem";
            }

            bat.LastUpdated = DateTime.Now;
            return result;
        });
    }

    /// <summary>
    /// Lightweight periodic poll that queries only dynamic status metrics (voltage, charge rate, remaining capacity, AC state).
    /// Bypasses all static motherboard/CPU/BIOS queries and powercfg CLI executions to minimize CPU & I/O overhead.
    /// </summary>
    public async Task<bool> PollDynamicStatusAsync(BatteryInfo bat)
    {
        return await Task.Run(() =>
        {
            try
            {
                // 1. Fast Native Kernel Power API (direct unmanaged call, ~0ms)
                bool nativeSuccess = NativeMethods.GetSystemPowerStatus(out var status);
                if (nativeSuccess)
                {
                    if (status.BatteryFlag == 128 || (status.ACLineStatus == 1 && status.BatteryFlag == 255 && status.BatteryLifePercent == 255))
                    {
                        bat.HasPhysicalBattery = false;
                        bat.IsAcOnline = true;
                        bat.BatteryStatusString = "Direct AC Grid (No Battery)";
                        bat.LastUpdated = DateTime.Now;
                        return true;
                    }

                    bat.HasPhysicalBattery = true;
                    bat.IsAcOnline = status.ACLineStatus == 1;
                    bat.IsCharging = (status.BatteryFlag & 8) != 0;
                    bat.IsDischarging = !bat.IsCharging && !bat.IsAcOnline;

                    if (status.BatteryLifeTime > 0)
                    {
                        bat.EstimatedRemainingRunTime = TimeSpan.FromSeconds(status.BatteryLifeTime);
                    }
                }

                // 2. Query root\wmi BatteryStatus for high-precision live ACPI electrical measurements
                if (bat.HasPhysicalBattery)
                {
                    bool wmiSuccess = false;
                    try
                    {
                        var scope = new ManagementScope(@"\\.\root\wmi");
                        scope.Connect();

                        using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT RemainingCapacity, Voltage, ChargeRate, Charging, Discharging, PowerOnline FROM BatteryStatus"));
                        using var collection = searcher.Get();

                        foreach (ManagementObject obj in collection)
                        {
                            wmiSuccess = true;
                            if (obj["RemainingCapacity"] != null && Convert.ToDouble(obj["RemainingCapacity"]) > 0)
                            {
                                bat.CurrentCapacityMwh = Convert.ToDouble(obj["RemainingCapacity"]);
                            }
                            if (obj["Voltage"] != null && Convert.ToDouble(obj["Voltage"]) > 0)
                            {
                                bat.VoltageMillivolts = Convert.ToDouble(obj["Voltage"]);
                            }
                            if (obj["ChargeRate"] != null)
                            {
                                bat.ChargeRateMilliwatts = Convert.ToDouble(obj["ChargeRate"]);
                            }
                            if (obj["Charging"] != null)
                            {
                                bat.IsCharging = Convert.ToBoolean(obj["Charging"]);
                            }
                            if (obj["Discharging"] != null)
                            {
                                bat.IsDischarging = Convert.ToBoolean(obj["Discharging"]);
                            }
                            if (obj["PowerOnline"] != null)
                            {
                                bat.IsAcOnline = Convert.ToBoolean(obj["PowerOnline"]);
                            }
                            break;
                        }
                    }
                    catch { }

                    // Fallback to Win32_Battery dynamic fields if root\wmi was inaccessible
                    if (!wmiSuccess)
                    {
                        try
                        {
                            using var searcher = new ManagementObjectSearcher("SELECT EstimatedChargeRemaining, EstimatedRunTime, BatteryStatus FROM Win32_Battery");
                            using var collection = searcher.Get();

                            foreach (ManagementObject obj in collection)
                            {
                                if (obj["EstimatedChargeRemaining"] != null && bat.FullChargeCapacityMwh > 0)
                                {
                                    double percent = Convert.ToDouble(obj["EstimatedChargeRemaining"]);
                                    bat.CurrentCapacityMwh = bat.FullChargeCapacityMwh * (percent / 100.0);
                                }

                                if (obj["EstimatedRunTime"] != null)
                                {
                                    uint runTimeMin = Convert.ToUInt32(obj["EstimatedRunTime"]);
                                    if (runTimeMin > 0 && runTimeMin < 71155)
                                    {
                                        bat.EstimatedRemainingRunTime = TimeSpan.FromMinutes(runTimeMin);
                                    }
                                }

                                if (obj["BatteryStatus"] != null)
                                {
                                    int bStatus = Convert.ToInt32(obj["BatteryStatus"]);
                                    bat.BatteryStatusString = bStatus switch
                                    {
                                        1 => "Discharging",
                                        2 => "Connected and charging",
                                        3 => "Fully Charged",
                                        4 => "Low",
                                        5 => "Critical",
                                        6 => "Charging",
                                        7 => "Charging and High",
                                        8 => "Charging and Low",
                                        9 => "Charging and Critical",
                                        10 => "Undefined",
                                        11 => "Partially Charged",
                                        _ => "Normal"
                                    };
                                }
                                break;
                            }
                        }
                        catch { }
                    }

                    // Update human-readable status string if not already assigned by CIM
                    if (string.IsNullOrEmpty(bat.BatteryStatusString) || bat.BatteryStatusString == "Normal")
                    {
                        if (bat.IsAcOnline)
                        {
                            bat.BatteryStatusString = bat.IsCharging ? "Charging" : (bat.ChargePercentage >= 99 ? "Fully Charged (AC Connected)" : "AC Connected - Idle");
                        }
                        else
                        {
                            bat.BatteryStatusString = "Discharging";
                        }
                    }
                }

                bat.LastUpdated = DateTime.Now;
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    private void ReadSystemPlatformInfo(SystemHardwareInfo sys)
    {
        try
        {
            // Win32_ComputerSystem
            using var csSearcher = new ManagementObjectSearcher("SELECT Manufacturer, Model, PCSystemType, TotalPhysicalMemory FROM Win32_ComputerSystem");
            using var csCollection = csSearcher.Get();
            foreach (ManagementObject obj in csCollection)
            {
                sys.Manufacturer = obj["Manufacturer"]?.ToString() ?? sys.Manufacturer;
                sys.Model = obj["Model"]?.ToString() ?? sys.Model;

                if (obj["TotalPhysicalMemory"] != null)
                {
                    double bytes = Convert.ToDouble(obj["TotalPhysicalMemory"]);
                    sys.TotalPhysicalMemoryGb = Math.Round(bytes / (1024.0 * 1024.0 * 1024.0), 1);
                }

                if (obj["PCSystemType"] != null)
                {
                    int pcType = Convert.ToInt32(obj["PCSystemType"]);
                    sys.Role = pcType switch
                    {
                        1 => FormFactorRole.Desktop,
                        2 => FormFactorRole.Laptop,
                        3 => FormFactorRole.Workstation,
                        4 => FormFactorRole.Server,
                        5 => FormFactorRole.Server,
                        6 => FormFactorRole.Desktop,
                        7 => FormFactorRole.Server,
                        8 => FormFactorRole.Tablet,
                        _ => FormFactorRole.Desktop
                    };
                }
                break;
            }
        }
        catch { }

        // Win32_SystemEnclosure (Chassis type)
        try
        {
            using var encSearcher = new ManagementObjectSearcher("SELECT ChassisTypes FROM Win32_SystemEnclosure");
            using var encCollection = encSearcher.Get();
            foreach (ManagementObject obj in encCollection)
            {
                if (obj["ChassisTypes"] is ushort[] chassisArray && chassisArray.Length > 0)
                {
                    int c = chassisArray[0];
                    sys.ChassisType = c switch
                    {
                        3 => "Desktop",
                        4 => "Low Profile Desktop",
                        5 => "Pizza Box",
                        6 => "Mini Tower",
                        7 => "Tower",
                        8 => "Portable",
                        9 => "Laptop",
                        10 => "Notebook",
                        11 => "Handheld",
                        12 => "Docking Station",
                        14 => "Sub Notebook",
                        30 => "Tablet",
                        31 => "Convertible",
                        32 => "Detachable",
                        _ => "Desktop Chassis"
                    };

                    if (c == 9 || c == 10 || c == 14 || c == 30 || c == 31 || c == 32)
                    {
                        sys.Role = FormFactorRole.Laptop;
                    }
                }
                break;
            }
        }
        catch { }

        // Win32_BaseBoard (Motherboard)
        try
        {
            using var bbSearcher = new ManagementObjectSearcher("SELECT Manufacturer, Product FROM Win32_BaseBoard");
            using var bbCollection = bbSearcher.Get();
            foreach (ManagementObject obj in bbCollection)
            {
                sys.MotherboardManufacturer = obj["Manufacturer"]?.ToString() ?? sys.MotherboardManufacturer;
                sys.MotherboardProduct = obj["Product"]?.ToString() ?? sys.MotherboardProduct;
                break;
            }
        }
        catch { }

        // Win32_Processor
        try
        {
            using var procSearcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores, MaxClockSpeed, LoadPercentage FROM Win32_Processor");
            using var procCollection = procSearcher.Get();
            foreach (ManagementObject obj in procCollection)
            {
                sys.ProcessorName = obj["Name"]?.ToString()?.Trim() ?? sys.ProcessorName;
                if (obj["NumberOfCores"] != null) sys.ProcessorCores = Convert.ToInt32(obj["NumberOfCores"]);
                if (obj["MaxClockSpeed"] != null) sys.ProcessorMaxGhz = Math.Round(Convert.ToDouble(obj["MaxClockSpeed"]) / 1000.0, 2);
                if (obj["LoadPercentage"] != null) sys.CpuLoadPercentage = Convert.ToInt32(obj["LoadPercentage"]);
                break;
            }
        }
        catch { }

        // Win32_BIOS
        try
        {
            using var biosSearcher = new ManagementObjectSearcher("SELECT SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS");
            using var biosCollection = biosSearcher.Get();
            foreach (ManagementObject obj in biosCollection)
            {
                sys.BiosVersion = obj["SMBIOSBIOSVersion"]?.ToString() ?? sys.BiosVersion;
                if (obj["ReleaseDate"]?.ToString() is { Length: > 0 } rawBiosDate)
                {
                    try { sys.BiosDate = ManagementDateTimeConverter.ToDateTime(rawBiosDate).ToString("yyyy-MM-dd"); }
                    catch { sys.BiosDate = rawBiosDate; }
                }
                break;
            }
        }
        catch { }
    }

    private void ReadActivePowerPlan(SystemHardwareInfo sys)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powercfg",
                Arguments = "/getactivescheme",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            using var proc = Process.Start(psi);
            if (proc != null)
            {
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(2000);

                var match = Regex.Match(output, @"\(([^)]+)\)");
                if (match.Success)
                {
                    sys.ActivePowerPlan = match.Groups[1].Value.Trim();
                }
            }
        }
        catch { }
    }

    private void CheckForUpsDevice(SystemHardwareInfo sys, BatteryInfo bat)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE ClassGuid = '{e6f1aa1c-7f3b-4861-b44c-3575b6da3961}' OR Description LIKE '%UPS%' OR Name LIKE '%UPS%'");
            using var collection = searcher.Get();

            foreach (ManagementObject obj in collection)
            {
                string name = obj["Name"]?.ToString() ?? string.Empty;
                string desc = obj["Description"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(desc))
                {
                    sys.HasUpsBattery = true;
                    sys.UpsDescription = $"Connected: {name} ({desc})";
                    break;
                }
            }
        }
        catch { }
    }

    private void TryEnrichFromPowercfgXml(BatteryInfo info)
    {
        try
        {
            string tempXmlPath = Path.Combine(Path.GetTempPath(), $"batterylens_probe_{Guid.NewGuid():N}.xml");
            var psi = new ProcessStartInfo
            {
                FileName = "powercfg",
                Arguments = $"/batteryreport /output \"{tempXmlPath}\" /xml",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var proc = Process.Start(psi);
            if (proc != null)
            {
                proc.WaitForExit(4000);
                if (File.Exists(tempXmlPath))
                {
                    try
                    {
                        var doc = XDocument.Load(tempXmlPath);
                        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

                        var batteryElem = doc.Descendants(ns + "Battery").FirstOrDefault();
                        if (batteryElem != null)
                        {
                            var designCap = batteryElem.Element(ns + "DesignCapacity")?.Value;
                            if (!string.IsNullOrEmpty(designCap) && double.TryParse(designCap, out double dc) && dc > 0)
                            {
                                info.DesignCapacityMwh = dc;
                            }

                            var fullCap = batteryElem.Element(ns + "FullChargeCapacity")?.Value;
                            if (!string.IsNullOrEmpty(fullCap) && double.TryParse(fullCap, out double fc) && fc > 0)
                            {
                                info.FullChargeCapacityMwh = fc;
                            }

                            var cycleCount = batteryElem.Element(ns + "CycleCount")?.Value;
                            if (!string.IsNullOrEmpty(cycleCount) && int.TryParse(cycleCount, out int cc))
                            {
                                info.CycleCount = cc;
                            }

                            var mfg = batteryElem.Element(ns + "Manufacturer")?.Value;
                            if (!string.IsNullOrEmpty(mfg)) info.Manufacturer = mfg;

                            var sn = batteryElem.Element(ns + "SerialNumber")?.Value;
                            if (!string.IsNullOrEmpty(sn)) info.SerialNumber = sn;

                            var chem = batteryElem.Element(ns + "Chemistry")?.Value;
                            if (!string.IsNullOrEmpty(chem)) info.Chemistry = chem;
                        }
                    }
                    finally
                    {
                        try { File.Delete(tempXmlPath); } catch { }
                    }
                }
            }
        }
        catch { }
    }

    public async Task<string> GenerateOfficialHtmlReportAsync()
    {
        return await Task.Run(() =>
        {
            string outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                $"BatteryLens_Official_Report_{DateTime.Now:yyyyMMdd_HHmmss}.html"
            );

            var psi = new ProcessStartInfo
            {
                FileName = "powercfg",
                Arguments = $"/batteryreport /output \"{outputPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            using var proc = Process.Start(psi);
            proc?.WaitForExit(6000);

            return File.Exists(outputPath) ? outputPath : string.Empty;
        });
    }
}
