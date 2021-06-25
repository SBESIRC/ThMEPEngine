using System;
using System.Windows.Data;
using System.Globalization;
using ThMEPWSS.Hydrant.Model;

namespace TianHua.Plumbing.WPF.UI.Converter
{
    public class WaterColumnLengthOpsBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            WaterColumnLengthOps s = (WaterColumnLengthOps)value;
            return s == (WaterColumnLengthOps)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (WaterColumnLengthOps)int.Parse(parameter.ToString());
        }
    }
}
