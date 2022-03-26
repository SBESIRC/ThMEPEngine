using System;
using ThCADExtension;
using System.Windows.Data;
using System.Globalization;

namespace TianHua.Electrical.PDS.UI.Editors
{
    // https://stackoverflow.com/questions/15567913/wpf-how-to-bind-an-enum-with-description-to-a-combobox
    public class ThPDSEnumDescriptionConverter<T> : IValueConverter where T : Enum
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
