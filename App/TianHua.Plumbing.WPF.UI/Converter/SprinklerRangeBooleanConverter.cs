using System;
using System.Windows.Data;
using System.Globalization;
using ThMEPWSS.Sprinkler.Model;

namespace TianHua.Plumbing.WPF.UI.Converter
{
    public class SprinklerRangeBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SprinklerRange s = (SprinklerRange)value;
            return s == (SprinklerRange)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (SprinklerRange)int.Parse(parameter.ToString());
        }
    }
}
