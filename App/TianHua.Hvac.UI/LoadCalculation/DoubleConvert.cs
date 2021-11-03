using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace TianHua.Hvac.UI.LoadCalculation
{
    public class DoubleConvert : IValueConverter
    {
        //当值从绑定源传播给绑定目标时，调用方法Convert
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        //当值从绑定目标传播给绑定源时，调用方法ConvertBack
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.IsNull()) return null;
            if (string.IsNullOrEmpty(value.ToString())) return null;
            return value;
        }
    }
}
