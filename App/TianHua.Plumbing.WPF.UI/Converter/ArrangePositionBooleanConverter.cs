using System;
using System.Windows.Data;
using System.Globalization;
using ThMEPWSS.FlushPoint.Model;

namespace TianHua.Plumbing.WPF.UI.Converter
{
    public class ArrangePositionBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ArrangePositionOps s = (ArrangePositionOps)value;
            return s == (ArrangePositionOps)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (ArrangePositionOps)int.Parse(parameter.ToString());
        }
    }
}
