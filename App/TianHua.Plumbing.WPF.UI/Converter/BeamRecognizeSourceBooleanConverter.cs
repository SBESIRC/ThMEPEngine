using System;
using System.Windows.Data;
using System.Globalization;
using ThMEPWSS.ViewModel;

namespace TianHua.Plumbing.WPF.UI.Converter
{
    public class BeamRecognizeSourceBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            BeamRecognizeSource s = (BeamRecognizeSource)value;
            return s == (BeamRecognizeSource)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (BeamRecognizeSource)int.Parse(parameter.ToString());
        }
    }
}
