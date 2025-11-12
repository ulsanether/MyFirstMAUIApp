using System.Globalization;

namespace MauiFirstUartApp.Converters;

public class BoolToConnectionColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            return isConnected ? Color.FromArgb("#10B981") : Color.FromArgb("#EF4444");
        }
        return Color.FromArgb("#6B7280");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
