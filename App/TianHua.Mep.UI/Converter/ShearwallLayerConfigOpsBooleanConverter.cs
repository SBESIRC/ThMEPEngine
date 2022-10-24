using System;
using System.Windows.Data;
using System.Globalization;
using TianHua.Mep.UI.ViewModel;

namespace TianHua.Mep.UI.Converter
{
    public class ShearwallLayerConfigOpsBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ShearwallLayerConfigOps s = (ShearwallLayerConfigOps)value;
            return s == (ShearwallLayerConfigOps)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (ShearwallLayerConfigOps)int.Parse(parameter.ToString());
        }
    }
}
