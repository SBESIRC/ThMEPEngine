using System;
using System.Globalization;
using System.Windows.Data;
using ThMEPElectrical.Model;

namespace TianHua.Electrical.UI.BlockConvert
{
    public class CapitalOpsBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            CapitalOP s = (CapitalOP)value;
            return s == (CapitalOP)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (CapitalOP)int.Parse(parameter.ToString());
        }
    }
}
