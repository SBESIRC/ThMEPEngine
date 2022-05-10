using System;
using System.Windows.Data;
using System.Globalization;
using ThMEPEngineCore.Config;

namespace TianHua.Mep.UI.Converter
{
    public class BeamEngineOpsBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            BeamEngineOps s = (BeamEngineOps)value;
            return s == (BeamEngineOps)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (BeamEngineOps)int.Parse(parameter.ToString());
        }
    }
}
