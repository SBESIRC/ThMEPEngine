using System;
using System.Globalization;
using System.Windows.Data;
using ThControlLibraryWPF.ControlUtils;

namespace ThControlLibraryWPF
{
    public class EnumDescriptionConverter<T> : IValueConverter where T : Enum
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Enum myEnum = (Enum)value;
            return CommonUtil.GetEnumDescription(myEnum);
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string item = (string)value;
            return CommonUtil.GetEnumItemByDescription<T>(item);
        }
    }
}
