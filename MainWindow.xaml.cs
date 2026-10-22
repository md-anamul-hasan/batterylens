using System;
using System.Windows;
using System.Windows.Interop;
using BatteryLens.Services;
using BatteryLens.ViewModels;

namespace BatteryLens;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Mdah.Windowing.MaximizeFix.Attach(this);
        Mdah.Windowing.ResponsiveScale.Attach(this, Body);
        DataContext ??= new MainViewModel();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var handle = new WindowInteropHelper(this).Handle;
        NativeMethods.EnableWindows11DarkTheme(handle);
    }

    private void OnGoToBuyerGuideClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.SelectedTabIndex = 2; // Index 2 is "Buyer's Decision Guide"
        }
    }

    private void OnGoToSimulationLabClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.SelectedTabIndex = 4; // Index 4 is "Simulation & Scenario Lab"
        }
    }
}