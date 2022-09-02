using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NFox.Cad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.IO.SVG;
using ThMEPEngineCore.Algorithm;
using ThCADCore.NTS;
using ThPlatform3D.Common;

namespace ThPlatform3D.ArchitecturePlane.Service
{
    internal static class ThArchPlaneUtils
    {
        public static string GetFillColor(this Dictionary<string, object> properties)
        {
            var value = properties.GetPropertyValue(ThSvgPropertyNameManager.FillColorPropertyName);
            if (value == null)
            {
                return "";
            }
            else
            {
                return (string)value;
            }
        }
        public static string GetLineType(this Dictionary<string, object> properties)
        {
            var value = properties.GetPropertyValue(ThSvgPropertyNameManager.LineTypePropertyName);
            if (value == null)
            {
                return "";
            }
            else
            {
                return (string)value;
            }
        }
        public static string GetCategory(this Dictionary<string, object> properties)
        {
            var value = properties.GetPropertyValue(ThSvgPropertyNameManager.CategoryPropertyName);
            if (value == null)
            {
                return "";
            }
            else
            {
                return (string)value;
            }
        }
        public static string GetMaterial(this Dictionary<string, object> properties)
        {
            var value = properties.GetPropertyValue(ThSvgPropertyNameManager.MaterialPropertyName);
            if (value == null)
            {
                return "";
            }
            else
            {
                return (string)value;
            }
        }
        public static bool IsKanXian(this Dictionary<string, object> properties)
        {
            var value = properties.GetPropertyValue(ThSvgPropertyNameManager.LPropertyName);
            if (value == null)
            {
                return false;
            }
            else
            {
                return (string)value == "kanxian";
            }
        }
        public static bool IsDuanMian(this Dictionary<string, object> properties)
        {
            var value = properties.GetPropertyValue(ThSvgPropertyNameManager.LPropertyName);
            if (value == null)
            {
                return false;
            }
            else
            {
                return (string)value == "duanmian";
            }
        }
        public static bool IsWindow(this string content)
        {
            return content == ThIfcCategoryManager.WindowCategory;
        }

        public static bool IsDoor(this string content)
        {
            return content == ThIfcCategoryManager.DoorCategory;
        }
        private static object GetPropertyValue(this Dictionary<string, object> properties, string key)
        {
            foreach (var item in properties)
            {
                if (item.Key.ToUpper() == key.ToUpper())
                {
                    return item.Value;
                }
            }
            return null;
        }      
        /// <summary>
        /// 检查AxB的规格,eg: 100x100
        /// </summary>
        /// <param name="spec"></param>
        /// <returns></returns>
        public static bool IsValidSpec(this string spec)
        {
            var newSpec = spec.Trim();
            string pattern = @"^\d+(.\d+)\s{0,}[Xx]{1}\s{0,}\d+(.\d+)$";
            return Regex.IsMatch(newSpec, pattern);
        }
        public static List<double> GetDoubles(this string content)
        {
            var datas = new List<double>();
            string pattern = @"\d+[.]?\d*";
            foreach (Match item in Regex.Matches(content, pattern))
            {
                datas.Add(double.Parse(item.Value));
            }
            return datas;
        }
        public static Point3d? ToPoint3d(this string point)
        {
            double x, y, z;
            var values = point.Split(',');
            if (point.IndexOf(",") > 0)
            {
                values = point.Split(',');
            }
            else
            {
                values = point.Split(' ');
            }
            if (values.Length == 2)
            {
                if (double.TryParse(values[0].Trim(), out x) && double.TryParse(values[1].Trim(), out y))
                {
                    return new Point3d(x, y, 0);
                }
            }
            if (values.Length == 3)
            {
                if (double.TryParse(values[0].Trim(), out x) &&
                    double.TryParse(values[1].Trim(), out y) &&
                    double.TryParse(values[2].Trim(), out z))
                {
                    return new Point3d(x, y, z);
                }
            }
            return null;
        }
        public static Vector3d? ToVector3d(this string point)
        {
            double x, y, z;
            var values = point.Split(',');
            if (values.Length == 2)
            {
                if (double.TryParse(values[0], out x) && double.TryParse(values[1], out y))
                {
                    return new Vector3d(x, y, 0);
                }
            }
            if (values.Length == 3)
            {
                if (double.TryParse(values[0], out x) &&
                    double.TryParse(values[1], out y) &&
                    double.TryParse(values[2], out z))
                {
                    return new Vector3d(x, y, z);
                }
            }
            return null;
        }
        public static bool IsVertical(this Vector3d first, Vector3d second, double angTolerance = 1.0)
        {
            var ang = first.GetAngleTo(second).RadToAng() % 180.0;
            return Math.Abs(ang - 90.0) <= angTolerance;
        }
        public static double GetProjectionDis(this Point3d pt, Point3d aixSp, Point3d aixEp)
        {
            return pt.GetProjectPtOnLine(aixSp, aixEp).DistanceTo(pt);
        }
        public static DBObjectCollection FilterSmallLines(this DBObjectCollection lines, double minimumLength)
        {
            var results = new DBObjectCollection();
            lines.OfType<Line>()
                .Where(o => o.Length >= minimumLength)
                .ForEach(o => results.Add(o));
            return results;
        }
        public static DBObjectCollection FilterVertical(this DBObjectCollection lines, Vector3d dir)
        {
            return lines.OfType<Line>()
                .Where(o => o.LineDirection().IsVertical(dir))
                .ToCollection();
        }
        public static Polyline GetBlkCurveMinimumRectangle(this BlockReference br)
        {
            var entities = ThDrawTool.Explode(br);
            var curves = entities.OfType<Curve>().ToCollection();
            var transformer = new ThMEPOriginTransformer(curves);
            transformer.Transform(curves);
            var rectangle = curves.GetMinimumRectangle();
            transformer.Reset(rectangle);
            entities.MDispose();
            return rectangle;
        }
        public static double GetWallThick(this string thickness, double scale = 1.0)
        {
            var values = thickness.GetDoubles();
            if (values.Count == 1)
            {
                return values[0] * scale;
            }
            else
            {
                return 0.0;
            }
        }
    }
}
