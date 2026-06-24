using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace IMoRS.Converters;

public class BoolToBrushConverter : IValueConverter
{
    public static BoolToBrushConverter Instance { get; } = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
            return new SolidColorBrush(Color.Parse("#FF5500AA"));
        return new SolidColorBrush(Colors.Transparent);
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }
}