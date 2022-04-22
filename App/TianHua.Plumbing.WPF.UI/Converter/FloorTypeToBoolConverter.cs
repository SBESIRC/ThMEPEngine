using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using ThMEPWSS.Diagram.ViewModel;

namespace TianHua.Plumbing.WPF.UI.Converter
{
    public class FloorTypeToBoolConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            WaterMeterLocation s = (WaterMeterLocation)value;
            return s == (WaterMeterLocation)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (WaterMeterLocation)int.Parse(parameter.ToString());
        }
    }
}
