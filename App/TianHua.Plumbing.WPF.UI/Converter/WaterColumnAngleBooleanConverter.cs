using System;
using System.Windows.Data;
using System.Globalization;
using ThMEPWSS.Hydrant.Model;

namespace TianHua.Plumbing.WPF.UI.Converter
{
    public class WaterColumnAngleBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            WaterColumnAngleOps s = (WaterColumnAngleOps)value;
            return s == (WaterColumnAngleOps)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (WaterColumnAngleOps)int.Parse(parameter.ToString());
        }
    }
}
