using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Assistant;

namespace ThMEPWSS.FireProtectionSystemDiagram
{
    class FireProtectionSysCommon
    {
        public static string LayoutPipeInterruptedBlcokName = "水管中断";
        public static DBText GetAddDBText(string str,double height, Point3d position, string layerName, string styleName) 
        {
            DBText infotext = new DBText()
            {
                TextString = str,
                Height = height,
                WidthFactor = 0.7,
                HorizontalMode = TextHorizontalMode.TextLeft,
                Oblique = 0,
                Position = position,
                Rotation = 0,
            };
            if (!string.IsNullOrEmpty(layerName))
                infotext.Layer = layerName;
            if (!string.IsNullOrEmpty(styleName))
            {
                var styleId = DrawUtils.GetTextStyleId(styleName);
                if (null != styleId && styleId.IsValid)
                {
                    infotext.TextStyleId = styleId;
                }
            }
            return infotext;
        }
        public static void GetTextHeightWidth(List<string> textString,double textHeight,string textStyle,out double height,out double width) 
        {
            height = 0.0;
            width = 0.0;
            var listDbTexts = new List<DBText>();
            foreach (var str in textString) 
            {
                var dbText = GetAddDBText(str, textHeight, new Point3d(), "0", textStyle);
                if (null != dbText)
                    listDbTexts.Add(dbText);
            }
            GetTextHeightWidth(listDbTexts, out height, out width);
        }
        public static void GetTextHeightWidth(List<DBText> dBTexts, out double height, out double width)
        {
            height = 0;
            width = 0;
            foreach (var item in dBTexts)
            {
                var text = item;
                var maxPoint = text.GeometricExtents.MaxPoint;
                var minPoint = text.GeometricExtents.MinPoint;
                var xDis = Math.Abs(maxPoint.X - minPoint.X);
                var yDis = Math.Abs(maxPoint.Y - minPoint.Y);
                height += yDis;
                width = Math.Max(width, xDis);
            }
        }
    }
}
