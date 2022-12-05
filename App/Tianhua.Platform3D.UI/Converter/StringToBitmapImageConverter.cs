using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Tianhua.Platform3D.UI.Converter
{
    public class StringToBitmapImageConverter : System.Windows.Markup.MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string path = value?.ToString();
            if (string.IsNullOrEmpty(path))
                return new BitmapImage(new Uri("", UriKind.Relative)); // "/images/404.jpg"
            try
            {
                return new BitmapImage(new Uri(path));
            }
            catch (Exception)
            {
                return new BitmapImage(new Uri("", UriKind.Relative)); // "/images/404.jpg"
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new StringToBitmapImageConverter();
        }
    }
}
