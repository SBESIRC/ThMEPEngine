using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ThControlLibraryWPF
{
    public class BackgroundMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {

            Brush background = null;
            if (null != values)
            {
                foreach (var item in values)
                {
                    try
                    {
                        background = (Brush)item;
                    }
                    catch (Exception ex) { }

                    if (null != background)
                        break;
                }
            }
            return background;

        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    public class ForegroundMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Brush foreground = null;
            if (null != values)
            {
                foreach (var item in values)
                {
                    try
                    {
                        foreground = (Brush)item;
                    }
                    catch (Exception ex) { }

                    if (null != foreground)
                        break;
                }
            }
            return foreground;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    public class BackgrougImageMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            ImageSource image = null;
            if (null != values)
            {
                foreach (var item in values)
                {
                    image = (ImageSource)item;
                    if (null != image)
                        break;
                }
            }
            return image;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ControlCornerRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var radius = (CornerRadius)value;
            string locatin = parameter.ToString().ToLower();
            var res = new CornerRadius(0);
            if (radius == null)
                return res;
            if (locatin == "all")
            {
                res = radius;
            }
            else if (locatin == "left")
            {
                res.TopLeft = radius.TopLeft;
                res.BottomLeft = radius.BottomLeft;
            }
            else if (locatin == "right")
            {
                res.TopRight = radius.TopRight;
                res.BottomRight = radius.BottomRight;
            }
            else if (locatin == "top")
            {
                res.TopRight = radius.TopRight;
                res.TopLeft = radius.TopLeft;
            }
            else if (locatin == "bottom")
            {
                res.BottomLeft = radius.BottomLeft;
                res.BottomRight = radius.BottomRight;
            }
            else if (locatin.Contains("tleft"))
            {
                res.TopLeft = radius.TopLeft;
            }
            else if (locatin.Contains("tright"))
            {
                res.TopRight = radius.TopRight;
            }
            else if (locatin.Contains("bleft"))
            {
                res.BottomLeft = radius.BottomLeft;
            }
            else if (locatin.Contains("brigth"))
            {
                res.BottomRight = radius.BottomRight;
            }
            return res;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
