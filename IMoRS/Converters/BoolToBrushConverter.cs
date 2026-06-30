using System;
 using System.Globalization;
 using Avalonia;
 using Avalonia.Data.Converters;
 using Avalonia.Media;
 
 namespace IMoRS.Converters;
 
 /// <summary>
 /// Конвертер bool в кисть для подсветки выбранного элемента в списке
 /// </summary>
 public class BoolToBrushConverter : IValueConverter
 {
     public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
     {
         if (value is bool isSelected && isSelected)
         {
             return new LinearGradientBrush
             {
                 StartPoint = new RelativePoint(1.0, 0.3, RelativeUnit.Relative),
                 EndPoint = new RelativePoint(0.0, 0.0, RelativeUnit.Relative),
                 GradientStops = new GradientStops
                 {
                     new GradientStop(Color.Parse("#FFFF1899"), 0.0),
                     new GradientStop(Color.Parse("#FFB93CFF"), 0.25),
                     new GradientStop(Color.Parse("#FFB71CFF"), 0.5),
                     new GradientStop(Color.Parse("#FF6F1899"), 0.75),
                     new GradientStop(Color.Parse("#FF4F1899"), 1.0)
                 }
             };
         }
         
         // Если не выбрана - прозрачная рамка
         return new SolidColorBrush(Colors.Transparent);
     }
     
     public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
     {
         // Обратная конвертация нам не нужна
         return value;
     }
 }