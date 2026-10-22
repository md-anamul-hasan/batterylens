using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using BatteryLens.Models;

namespace BatteryLens.Converters;

public class HealthToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double health)
        {
            if (health >= 80) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")); // Green
            if (health >= 65) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")); // Amber
            if (health >= 45) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F97316")); // Orange
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")); // Red
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class UrgencyToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is UrgencyLevel urgency)
        {
            return urgency switch
            {
                UrgencyLevel.Critical => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                UrgencyLevel.High => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F97316")),
                UrgencyLevel.Medium => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
                UrgencyLevel.Low => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"))
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            }
            catch { }
        }
        return new SolidColorBrush(Colors.SlateGray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BooleanToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; } = false;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool b = value is bool boolVal && boolVal;
        if (Invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class TimeSpanFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TimeSpan ts)
        {
            if (ts.TotalMinutes < 1) return "< 1 min";
            if (ts.TotalHours < 1) return $"{ts.Minutes}m";
            return $"{(int)ts.TotalHours}h {ts.Minutes:D2}m";
        }
        return "Calculating...";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class DoubleToStarGridLengthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d && !double.IsNaN(d) && !double.IsInfinity(d))
        {
            return new GridLength(Math.Max(0.01, d), GridUnitType.Star);
        }
        return new GridLength(1, GridUnitType.Star);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class IntToRadioBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intVal && int.TryParse(parameter?.ToString(), out int paramVal))
        {
            return intVal == paramVal;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolVal && boolVal && int.TryParse(parameter?.ToString(), out int paramVal))
        {
            return paramVal;
        }
        return Binding.DoNothing;
    }
}

public class IntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intVal && int.TryParse(parameter?.ToString(), out int targetVal))
        {
            return intVal == targetVal ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class PercentageToHeightConverter : IValueConverter
{
    public double MaxHeight { get; set; } = 60.0;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double pct && !double.IsNaN(pct) && !double.IsInfinity(pct))
        {
            double max = MaxHeight;
            if (parameter != null && double.TryParse(parameter.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double pMax))
            {
                max = pMax;
            }
            double clamped = Math.Max(0.0, Math.Min(100.0, pct));
            return Math.Max(4.0, (clamped / 100.0) * max);
        }
        return 4.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
