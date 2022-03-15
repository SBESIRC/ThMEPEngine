using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.ViewModel;

namespace TianHua.Plumbing.WPF.UI.Converter
{
    public class SingleRowSettingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SingleRowSettingEnum s = (SingleRowSettingEnum)value;
            return s == (SingleRowSettingEnum)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (SingleRowSettingEnum)int.Parse(parameter.ToString());
        }
    }
}
