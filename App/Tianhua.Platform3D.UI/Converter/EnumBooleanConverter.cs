using System;
using System.Windows.Data;
using System.Globalization;
using Tianhua.Platform3D.UI.StructurePlane;
using ThPlatform3D.Model;

namespace Tianhua.Platform3D.UI.Converter
{
    public class StoreySelectOpsBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            StoreySelectOps s = (StoreySelectOps)value;
            return s == (StoreySelectOps)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (StoreySelectOps)int.Parse(parameter.ToString());
        }
    }
    public class FileFormatOpsBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            FileFormatOps s = (FileFormatOps)value;
            return s == (FileFormatOps)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (FileFormatOps)int.Parse(parameter.ToString());
        }
    }
    public class DrawingTypeOpsBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DrawingTypeOps s = (DrawingTypeOps)value;
            return s == (DrawingTypeOps)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (DrawingTypeOps)int.Parse(parameter.ToString());
        }
    }
    public class ViewDirectionBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ViewDirection s = (ViewDirection)value;
            return s == (ViewDirection)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (ViewDirection)int.Parse(parameter.ToString());
        }
    }
}
