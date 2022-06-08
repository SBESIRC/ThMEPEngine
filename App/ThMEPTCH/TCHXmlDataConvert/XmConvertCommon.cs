using Autodesk.AutoCAD.Geometry;
using System.Linq;

namespace ThMEPTCH.TCHXmlDataConvert
{
    class XmConvertCommon
    {
        public static Point3d StringToPoint3d(string str) 
        {
            if (string.IsNullOrEmpty(str))
                return Point3d.Origin;
            var ptValues = str.Split(',').ToList();
            if (ptValues.Count == 3)
            {
                var xValue = 0.0;
                var yValue = 0.0;
                var zValue = 0.0;
                double.TryParse(ptValues[0], out xValue);
                double.TryParse(ptValues[1], out yValue);
                double.TryParse(ptValues[2], out zValue);
                return new Point3d(xValue, yValue, zValue);
            }
            return Point3d.Origin;
        }
        public static double StringToDouble(string str) 
        {
            if (string.IsNullOrEmpty(str))
                return 0.0;
            double.TryParse(str, out double value);
            return value;
        }
        public static int StringToInt(string str) 
        {
            if (string.IsNullOrEmpty(str))
                return 0;
            int.TryParse(str, out int value);
            return value;
        }
    }
}
