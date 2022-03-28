using System;
using System.Windows.Data;
using System.Globalization;

namespace TianHua.Electrical.PDS.UI.Converters
{
    public class GlyphsUnicodeStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value?.ToString();
            if (string.IsNullOrEmpty(s)) return " ";
            return s;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
