using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace TianHua.PDS.UI.Converter
{
    public class VisibilityHiddenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? Visibility.Visible : Visibility.Hidden;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (Visibility)value == Visibility.Visible;
        }
    }
    public class VisibilityCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (Visibility)value == Visibility.Visible;
        }
    }
    public class RadioUiValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return Equals(value, parameter);
            if (value is bool b)
            {
                return b == bool.Parse((string)parameter);
            }
            if (value is string str)
            {
                return str == (string)parameter;
            }
            if (value.GetType().IsEnum)
            {
                return value.ToString() == (string)parameter;
            }
            return Equals(value, parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                if (targetType == typeof(bool))
                {
                    return bool.Parse((string)parameter);
                }
                if (targetType == typeof(string))
                {
                    return parameter;
                }
                if (targetType.IsEnum)
                {
                    return Enum.Parse(targetType, (string)parameter);
                }
            }
            return Binding.DoNothing;
        }
    }
}
