using System;
using System.Windows.Data;
using System.Globalization;

namespace TianHua.Electrical.PDS.UI.Converters
{
    public class NormalValueConverter : IValueConverter
    {
        readonly Func<object, object> f;
        public NormalValueConverter(Func<object, object> f)
        {
            if (f is null) throw new ArgumentNullException();
            this.f = f;
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return f(value);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
