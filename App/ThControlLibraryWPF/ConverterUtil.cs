using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ThControlLibraryWPF
{
    public class EnumIntToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var enumType = value.GetType();
            var enumItem = enumType.GetField(value.ToString());
            int enumValue = (int)enumItem.GetValue(value.ToString());
            return enumValue == int.Parse(parameter.ToString());
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            var enumItem = targetType.GetEnumValues();
            var intValue = int.Parse(parameter.ToString());
            foreach (var item in enumItem)
            {
                if (intValue == (int)item)
                    return item;
            }
            return enumItem.GetValue(0);
        }
    }
    public class EnumToVisibilty : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var enumType = value.GetType();
            var enumItem = enumType.GetField(value.ToString());
            int enumValue = (int)enumItem.GetValue(value.ToString());
            return enumValue == int.Parse(parameter.ToString()) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var visible = (Visibility)value;
            if (visible != Visibility.Visible)
                return null;
            var enumItem = targetType.GetEnumValues();
            var intValue = int.Parse(parameter.ToString());
            foreach (var item in enumItem)
            {
                if (intValue == (int)item)
                    return item;
            }
            return enumItem.GetValue(0);
        }
    }
    public class BoolToVisibilty : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool s = (bool)value;
            if (null != parameter)
            {
                var str = parameter.ToString();
                if (str == "1")
                    s = !s;
            }
            return s ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var visibility = (Visibility)value;
            var isTrue = visibility != Visibility.Visible;
            if (null != parameter)
            {
                var str = parameter.ToString();
                if (str == "1")
                    isTrue = !isTrue;
            }
            return isTrue;
        }
    }
    public class CheckTwoStringEqualsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var str1 = string.Empty;
                if (null != values[0])
                    str1 = (string)values[0];
                var str2 = string.Empty;
                if (null != values[0])
                    str2 = (string)values[1];
                if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
                    return false;
                return str1.Equals(str2);
            }
            catch { }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            string[] test = new string[2];
            return test;
        }
    }
}
