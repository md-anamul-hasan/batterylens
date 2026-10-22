using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using BatteryLens.Models;
using BatteryLens.Services;

namespace BatteryLens.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly BatteryHardwareReader _hardwareReader = new();
    private readonly DiagnosticEngine _diagnosticEngine = new();
    private readonly DispatcherTimer _pollTimer;
    private readonly DispatcherTimer _toastTimer;


    private SystemHardwareInfo _systemInfo = new();
    private BatteryInfo _battery = new();
    private BatteryInfo _liveHardwareBattery = new();
    private DiagnosticRecommendation _recommendation = new();

    private bool _isSimulationMode = false;
    private bool _hasPhysicalBattery = false;
    private int _selectedTabIndex = 0;
    private string _statusBannerMessage = string.Empty;
    private string _statusBannerType = "Info";
    private bool _isStatusBannerVisible = false;
    private BatteryPreset? _selectedPreset;

    // Simulation Slider Properties
    private double _simDesignCapacity = 56000;
    private double _simFullChargeCapacity = 32000;
    private double _simCurrentCapacity = 20000;
    private int _simCycleCount = 680;
    private bool _simIsAcOnline = false;
    private bool _simIsCharging = false;
    private double _simRateWatts = 18.5;
    private double _simVoltage = 10.8;
    private string _simModelName = "Custom Test Pack";

    public MainViewModel()
    {
        // Commands
        RefreshHardwareCommand = new RelayCommand(async () => await RefreshHardwareAsync());
        SelectPresetCommand = new RelayCommand(param => SelectPreset(param as BatteryPreset));
        ResetToLiveHardwareCommand = new RelayCommand(() => ResetToLiveHardware());
        ExportHtmlReportCommand = new RelayCommand(async () => await ExportHtmlReportAsync());
        GenerateOfficialWindowsReportCommand = new RelayCommand(async () => await GenerateOfficialWindowsReportAsync());
        OpenOfficialReportCommand = new RelayCommand(param => OpenFile(param as string));
        DismissBannerCommand = new RelayCommand(() =>
        {
            _toastTimer?.Stop();
            StatusBannerMessage = string.Empty;
            IsStatusBannerVisible = false;
        });

        OpenRepositoryCommand = new RelayCommand(() => OpenUrl(RepositoryUrl));

        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(7) };
        _toastTimer.Tick += (s, e) =>
        {
            _toastTimer.Stop();
            IsStatusBannerVisible = false;
        };

        // Load Presets for the test lab
        Presets = new ObservableCollection<BatteryPreset>(BatteryPreset.GetDefaultPresets());

        // Setup periodic poll timer for live hardware telemetry
        _pollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _pollTimer.Tick += async (s, e) =>
        {
            if (!_isSimulationMode)
            {
                await PollTelemetryAsync();
            }
        };

        // Initial Load
        _ = InitializeAsync();
    }

    public const string RepositoryUrl = "https://github.com/md-anamul-hasan/batterylens";

    public string AppVersion { get; } =
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); } catch { /* no browser */ }
    }

    private async Task InitializeAsync()
    {
        await RefreshHardwareAsync();
        _pollTimer.Start();
    }

    public SystemHardwareInfo SystemInfo
    {
        get => _systemInfo;
        set
        {
            if (SetProperty(ref _systemInfo, value))
            {
                OnPropertyChanged(nameof(IsDesktopMode));
                OnPropertyChanged(nameof(IsLaptopMode));
            }
        }
    }

    public BatteryInfo Battery
    {
        get => _battery;
        set => SetProperty(ref _battery, value);
    }

    public DiagnosticRecommendation Recommendation
    {
        get => _recommendation;
        set => SetProperty(ref _recommendation, value);
    }

    public bool IsSimulationMode
    {
        get => _isSimulationMode;
        set => SetProperty(ref _isSimulationMode, value);
    }

    public bool HasPhysicalBattery
    {
        get => _hasPhysicalBattery;
        set
        {
            if (SetProperty(ref _hasPhysicalBattery, value))
            {
                OnPropertyChanged(nameof(IsDesktopMode));
                OnPropertyChanged(nameof(IsLaptopMode));
            }
        }
    }

    public bool IsDesktopMode => !_hasPhysicalBattery || _systemInfo.IsDesktop;
    public bool IsLaptopMode => _hasPhysicalBattery && !_systemInfo.IsDesktop;

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    public string StatusBannerMessage
    {
        get => _statusBannerMessage;
        set
        {
            if (SetProperty(ref _statusBannerMessage, value))
            {
                OnPropertyChanged(nameof(IsStatusBannerVisible));
            }
        }
    }

    public string StatusBannerType
    {
        get => _statusBannerType;
        set => SetProperty(ref _statusBannerType, value);
    }

    public bool IsStatusBannerVisible
    {
        get => _isStatusBannerVisible && !string.IsNullOrWhiteSpace(_statusBannerMessage);
        set
        {
            if (SetProperty(ref _isStatusBannerVisible, value))
            {
                if (!value) _statusBannerMessage = string.Empty;
            }
        }
    }

    public ObservableCollection<BatteryPreset> Presets { get; }

    public ObservableCollection<BatteryHistoryPoint> LiveHistory { get; } = new();

    public IEnumerable<BatteryHistoryPoint> RecentHistorySummary => LiveHistory.Reverse().Take(8);

    public bool HasHistoryPoints => LiveHistory.Count > 0;

    public BatteryHistoryPoint? LatestHistoryPoint => LiveHistory.Count > 0 ? LiveHistory[^1] : null;

    public BatteryPreset? SelectedPreset
    {
        get => _selectedPreset;
        set => SetProperty(ref _selectedPreset, value);
    }

    // Interactive Simulation Properties
    public double SimDesignCapacity
    {
        get => _simDesignCapacity;
        set
        {
            if (SetProperty(ref _simDesignCapacity, value))
            {
                if (_simFullChargeCapacity > _simDesignCapacity)
                    SimFullChargeCapacity = _simDesignCapacity;
                ApplyCustomSimulation();
            }
        }
    }

    public double SimFullChargeCapacity
    {
        get => _simFullChargeCapacity;
        set
        {
            if (SetProperty(ref _simFullChargeCapacity, value))
            {
                if (_simCurrentCapacity > _simFullChargeCapacity)
                    SimCurrentCapacity = _simFullChargeCapacity;
                ApplyCustomSimulation();
            }
        }
    }

    public double SimCurrentCapacity
    {
        get => _simCurrentCapacity;
        set
        {
            if (SetProperty(ref _simCurrentCapacity, value))
                ApplyCustomSimulation();
        }
    }

    public int SimCycleCount
    {
        get => _simCycleCount;
        set
        {
            if (SetProperty(ref _simCycleCount, value))
                ApplyCustomSimulation();
        }
    }

    public bool SimIsAcOnline
    {
        get => _simIsAcOnline;
        set
        {
            if (SetProperty(ref _simIsAcOnline, value))
            {
                if (!_simIsAcOnline) SimIsCharging = false;
                ApplyCustomSimulation();
            }
        }
    }

    public bool SimIsCharging
    {
        get => _simIsCharging;
        set
        {
            if (SetProperty(ref _simIsCharging, value))
                ApplyCustomSimulation();
        }
    }

    public double SimRateWatts
    {
        get => _simRateWatts;
        set
        {
            if (SetProperty(ref _simRateWatts, value))
                ApplyCustomSimulation();
        }
    }

    public double SimVoltage
    {
        get => _simVoltage;
        set
        {
            if (SetProperty(ref _simVoltage, value))
                ApplyCustomSimulation();
        }
    }

    public string SimModelName
    {
        get => _simModelName;
        set
        {
            if (SetProperty(ref _simModelName, value))
                ApplyCustomSimulation();
        }
    }

    // Commands
    public ICommand RefreshHardwareCommand { get; }
    public ICommand SelectPresetCommand { get; }
    public ICommand ResetToLiveHardwareCommand { get; }
    public ICommand ExportHtmlReportCommand { get; }
    public ICommand GenerateOfficialWindowsReportCommand { get; }
    public ICommand OpenOfficialReportCommand { get; }
    public ICommand DismissBannerCommand { get; }
    public ICommand OpenRepositoryCommand { get; }

    public async Task RefreshHardwareAsync(bool silent = false)
    {
        if (IsSimulationMode) return;

        try
        {
            var audit = await _hardwareReader.AuditSystemAndBatteryAsync();
            SystemInfo = audit.System;
            _liveHardwareBattery = audit.Battery;
            Battery = audit.Battery;
            HasPhysicalBattery = audit.Battery.HasPhysicalBattery;
            Recommendation = _diagnosticEngine.Analyze(Battery, SystemInfo);
            RecordHistoryPoint(Battery);

            if (!silent)
            {
                if (SystemInfo.IsDesktop)
                {
                    ShowBanner($"Audited {SystemInfo.Manufacturer} {SystemInfo.MotherboardProduct} Desktop ({SystemInfo.ProcessorName}) — Operating on direct AC mains power.", "Success");
                }
                else
                {
                    ShowBanner($"Audited laptop battery: {Battery.HealthPercentage:F0}% health, {Battery.CycleCount} cycles logged.", "Success");
                }
            }
        }
        catch (Exception ex)
        {
            if (!silent)
            {
                ShowBanner($"Failed reading hardware telemetry: {ex.Message}", "Warning");
            }
        }
    }

    private async Task PollTelemetryAsync()
    {
        if (IsSimulationMode) return;

        try
        {
            bool ok = await _hardwareReader.PollDynamicStatusAsync(_liveHardwareBattery);
            if (ok)
            {
                // If physical battery presence toggled, trigger full audit refresh
                if (_hasPhysicalBattery != _liveHardwareBattery.HasPhysicalBattery)
                {
                    await RefreshHardwareAsync(silent: true);
                    return;
                }

                Battery = _liveHardwareBattery;
                Recommendation = _diagnosticEngine.Analyze(Battery, SystemInfo);
                RecordHistoryPoint(Battery);
                OnPropertyChanged(nameof(Battery));
            }
        }
        catch { }
    }

    private void RecordHistoryPoint(BatteryInfo battery)
    {
        if (IsSimulationMode || !battery.HasPhysicalBattery) return;

        var point = new BatteryHistoryPoint
        {
            Timestamp = DateTime.Now,
            CapacityMwh = Math.Round(battery.CurrentCapacityMwh, 0),
            Percentage = Math.Round(battery.ChargePercentage, 1),
            IsCharging = battery.IsCharging
        };

        battery.RecentHistory.Add(point);
        while (battery.RecentHistory.Count > 60)
        {
            battery.RecentHistory.RemoveAt(0);
        }

        LiveHistory.Add(point);
        while (LiveHistory.Count > 60)
        {
            LiveHistory.RemoveAt(0);
        }

        OnPropertyChanged(nameof(HasHistoryPoints));
        OnPropertyChanged(nameof(LatestHistoryPoint));
        OnPropertyChanged(nameof(RecentHistorySummary));
    }

    public void SelectPreset(BatteryPreset? preset)
    {
        if (preset == null) return;

        SelectedPreset = preset;
        IsSimulationMode = true;

        var copy = preset.Info.Clone();
        Battery = copy;
        HasPhysicalBattery = true;
        Recommendation = _diagnosticEngine.Analyze(copy, new SystemHardwareInfo
        {
            Role = FormFactorRole.Laptop,
            Manufacturer = copy.Manufacturer,
            Model = copy.ModelName
        });

        // Clear live history from simulation view
        LiveHistory.Clear();
        OnPropertyChanged(nameof(HasHistoryPoints));
        OnPropertyChanged(nameof(LatestHistoryPoint));
        OnPropertyChanged(nameof(RecentHistorySummary));

        // Sync slider values
        _simDesignCapacity = copy.DesignCapacityMwh;
        _simFullChargeCapacity = copy.FullChargeCapacityMwh;
        _simCurrentCapacity = copy.CurrentCapacityMwh;
        _simCycleCount = copy.CycleCount;
        _simIsAcOnline = copy.IsAcOnline;
        _simIsCharging = copy.IsCharging;
        _simRateWatts = copy.RateWatts;
        _simVoltage = copy.VoltageVolts;
        _simModelName = copy.ModelName;

        OnPropertyChanged(nameof(SimDesignCapacity));
        OnPropertyChanged(nameof(SimFullChargeCapacity));
        OnPropertyChanged(nameof(SimCurrentCapacity));
        OnPropertyChanged(nameof(SimCycleCount));
        OnPropertyChanged(nameof(SimIsAcOnline));
        OnPropertyChanged(nameof(SimIsCharging));
        OnPropertyChanged(nameof(SimRateWatts));
        OnPropertyChanged(nameof(SimVoltage));
        OnPropertyChanged(nameof(SimModelName));

        ShowBanner($"Loaded Laptop Simulation Scenario: {preset.Name}", "Info");
    }

    public void ResetToLiveHardware()
    {
        IsSimulationMode = false;
        SelectedPreset = null;
        Battery = _liveHardwareBattery;
        HasPhysicalBattery = _liveHardwareBattery.HasPhysicalBattery;
        Recommendation = _diagnosticEngine.Analyze(Battery, SystemInfo);

        // Restore live hardware history
        LiveHistory.Clear();
        foreach (var pt in _liveHardwareBattery.RecentHistory)
        {
            LiveHistory.Add(pt);
        }
        OnPropertyChanged(nameof(HasHistoryPoints));
        OnPropertyChanged(nameof(LatestHistoryPoint));
        OnPropertyChanged(nameof(RecentHistorySummary));

        ShowBanner("Restored Live Computer Hardware Telemetry.", "Success");
    }

    public void ApplyCustomSimulation()
    {
        if (!IsSimulationMode) return;

        // Clear live history during simulation
        LiveHistory.Clear();
        OnPropertyChanged(nameof(HasHistoryPoints));
        OnPropertyChanged(nameof(LatestHistoryPoint));
        OnPropertyChanged(nameof(RecentHistorySummary));

        var sim = new BatteryInfo
        {
            HasPhysicalBattery = true,
            DeviceName = "Custom Test Pack",
            Manufacturer = "BatteryLens Lab",
            ModelName = _simModelName,
            SerialNumber = "SIM-9942",
            Chemistry = "Lithium-Ion Polymer",
            DesignCapacityMwh = Math.Max(1000, _simDesignCapacity),
            FullChargeCapacityMwh = Math.Max(500, Math.Min(_simDesignCapacity, _simFullChargeCapacity)),
            CurrentCapacityMwh = Math.Max(0, Math.Min(_simFullChargeCapacity, _simCurrentCapacity)),
            CycleCount = _simCycleCount,
            MaxDesignCycles = 500,
            IsAcOnline = _simIsAcOnline,
            IsCharging = _simIsAcOnline && _simIsCharging,
            IsDischarging = !_simIsAcOnline,
            BatteryStatusString = _simIsAcOnline ? (_simIsCharging ? "Charging" : "AC Connected - Not Charging") : "Discharging",
            VoltageMillivolts = _simVoltage * 1000,
            ChargeRateMilliwatts = (_simIsCharging ? 1 : -1) * (_simRateWatts * 1000),
            EstimatedRemainingRunTime = _simRateWatts > 0 ? TimeSpan.FromHours(_simCurrentCapacity / (_simRateWatts * 1000.0)) : TimeSpan.FromHours(4),
            EstimatedTimeToFullCharge = _simIsCharging && _simRateWatts > 0 ? TimeSpan.FromHours((_simFullChargeCapacity - _simCurrentCapacity) / (_simRateWatts * 1000.0)) : null,
            TemperatureCelsius = 31.5,
            DataSource = "Interactive Simulation Lab"
        };

        Battery = sim;
        Recommendation = _diagnosticEngine.Analyze(sim, new SystemHardwareInfo { Role = FormFactorRole.Laptop });
    }

    public async Task ExportHtmlReportAsync()
    {
        try
        {
            string path = await ReportExporter.ExportHtmlReportAsync(Battery, Recommendation, SystemInfo);
            ShowBanner($"Diagnostic Report exported to Desktop: {Path.GetFileName(path)}", "Success");
            OpenFile(path);
        }
        catch (Exception ex)
        {
            ShowBanner($"Could not export report: {ex.Message}", "Error");
        }
    }

    public async Task GenerateOfficialWindowsReportAsync()
    {
        try
        {
            ShowBanner("Generating Windows batteryreport via powercfg...", "Info");
            string path = await _hardwareReader.GenerateOfficialHtmlReportAsync();
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                ShowBanner($"Windows Battery Report saved to Desktop: {Path.GetFileName(path)}", "Success");
                OpenFile(path);
            }
            else
            {
                ShowBanner("Windows batteryreport generated successfully.", "Success");
            }
        }
        catch (Exception ex)
        {
            ShowBanner($"Failed running powercfg: {ex.Message}", "Error");
        }
    }

    private void OpenFile(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void ShowBanner(string message, string type)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            IsStatusBannerVisible = false;
            return;
        }
        StatusBannerMessage = message;
        StatusBannerType = type;
        IsStatusBannerVisible = true;
        _toastTimer.Stop();
        _toastTimer.Start();
    }
}
