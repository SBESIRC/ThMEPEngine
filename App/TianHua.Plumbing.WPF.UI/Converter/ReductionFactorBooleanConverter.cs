using System;
using System.Windows.Data;
using System.Globalization;
using ThMEPWSS.Hydrant.Model;

namespace TianHua.Plumbing.WPF.UI.Converter
{
    public class ReductionFactorBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ReductionFactorOps s = (ReductionFactorOps)value;
            return s == (ReductionFactorOps)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (ReductionFactorOps)int.Parse(parameter.ToString());
        }
    }
}
