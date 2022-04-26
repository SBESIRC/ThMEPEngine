using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace TianHua.Electrical.PDS.UI.Converters
{
    public class EnumValuesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is "SkipZero")
            {
                var list = new List<object>();
                foreach (var v in Enum.GetValues(value.GetType()))
                {
                    if (System.Convert.ToInt32(v) != 0) list.Add(v);
                }
                return list;
            }
            if (parameter is "SkipOne")
            {
                var list = new List<object>();
                foreach (var v in Enum.GetValues(value.GetType()))
                {
                    if (System.Convert.ToInt32(v) != 1) list.Add(v);
                }
                return list;
            }
            if (parameter is "SkipNone")
            {
                var list = new List<object>();
                foreach (var v in Enum.GetValues(value.GetType()))
                {
                    if (v.ToString() is not "None") list.Add(v);
                }
                return list;
            }
            return Enum.GetValues(value.GetType());
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
