using System;
using System.Globalization;
using System.Windows.Data;
using ThCADExtension;

namespace Tianhua.Platform3D.UI.PropertyServices
{
    public class EnumDescriptionConverter<T> : IValueConverter where T : Enum
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Enum myEnum = (Enum)value;
            return myEnum.GetEnumDescription();
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string item = (string)value;
            return item.GetEnumName<T>();
        }
    }
}
