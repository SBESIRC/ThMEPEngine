using System;
using System.Windows.Data;
using System.Globalization;
using ThMEPWSS.Sprinkler.Model;

namespace TianHua.Plumbing.WPF.UI.Converter
{
    public class CheckSprinklerTypeBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SprinklerType s = (SprinklerType)value;
            return s == (SprinklerType)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (SprinklerType)int.Parse(parameter.ToString());
        }
    }
}
