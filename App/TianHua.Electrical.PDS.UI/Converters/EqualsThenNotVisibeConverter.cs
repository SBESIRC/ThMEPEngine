using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

namespace TianHua.Electrical.PDS.UI.Converters
{
    public class EqualsThenNotVisibeConverter : IValueConverter
    {
        object target;
        public EqualsThenNotVisibeConverter(object target)
        {
            this.target = target;
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Equals(value, target) ? Visibility.Collapsed : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
