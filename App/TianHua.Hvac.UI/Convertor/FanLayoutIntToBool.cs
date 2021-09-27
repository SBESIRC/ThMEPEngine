using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TianHua.Hvac.UI.Convertor
{
    public class FanLayoutIntToBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int s = (int)value;
            return s == int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return int.Parse(parameter.ToString());
        }
    }

    public class FanLayoutIntToVisibilty : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int s = (int)value;
            return s == int.Parse(parameter.ToString())? Visibility.Visible:Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var visibility = (Visibility)value;
            if (visibility!=Visibility.Visible)
            {
                return null;
            }
            return int.Parse(parameter.ToString());
        }
    }

}
