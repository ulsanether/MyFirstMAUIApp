using System.Globalization;

namespace MauiFirstUartApp.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string colorParam)
        {
            var colors = colorParam.Split('|');
            if (colors.Length == 2)
            {
                return boolValue ? Color.FromArgb(colors[0]) : Color.FromArgb(colors[1]);
            }
        }
        return Colors.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
