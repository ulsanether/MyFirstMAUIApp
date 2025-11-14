using MauiFirstUartApp.Core.Abstractions;

using System.Globalization;

namespace MauiFirstUartApp.Converters
{
    public class SerialTypeToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SerialType serialType && parameter is string paramStr)
            {
                if (Enum.TryParse(typeof(SerialType), paramStr, out var paramEnum) && paramEnum is SerialType paramSerialType)
                    return serialType == paramSerialType;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked && parameter is string paramStr)
            {
                if (Enum.TryParse(typeof(SerialType), paramStr, out var paramEnum) && paramEnum is SerialType paramSerialType)
                    return paramSerialType;
            }
            return Binding.DoNothing;
        }
    }
}