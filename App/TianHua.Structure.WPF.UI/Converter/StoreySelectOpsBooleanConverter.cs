using System;
using System.Windows.Data;
using System.Globalization;
using TianHua.Structure.WPF.UI.StructurePlane;

namespace TianHua.Structure.WPF.UI.Converter
{
    public class StoreySelectOpsBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            StoreySelectOps s = (StoreySelectOps)value;
            return s == (StoreySelectOps)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (StoreySelectOps)int.Parse(parameter.ToString());
        }
    }
}
