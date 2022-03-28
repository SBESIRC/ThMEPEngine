using System;
using System.Windows.Data;
using System.Globalization;

namespace TianHua.Electrical.PDS.UI.Converters
{
    public class NormalValueConverter : IValueConverter
    {
        readonly Func<object, object> convert;
        readonly Func<object, object> convertBack;

        public NormalValueConverter(Func<object, object> convert, Func<object, object> convertBack = null)
        {
            if (convert is null) throw new ArgumentNullException();
            this.convert = convert;
            this.convertBack = convertBack;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return convert(value);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (convertBack is null) throw new NotSupportedException();
            return convertBack(value);
        }
    }
}
