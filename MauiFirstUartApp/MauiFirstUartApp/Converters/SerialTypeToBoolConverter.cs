using System.Globalization;

using MauiFirstUartApp.Core.Abstractions;

namespace MauiFirstUartApp.Converters;

public class SerialTypeToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SerialType serialType && parameter is string paramStr)
        {
            // 더 안전한 enum 파싱
            return Enum.TryParse<SerialType>(paramStr, true, out var targetSerialType) &&
                   serialType == targetSerialType;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter is string paramStr)
        {
            return Enum.TryParse<SerialType>(paramStr, true, out var serialType) ?
                   serialType :
                   Binding.DoNothing;
        }
        return Binding.DoNothing;
    }
}
