using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    class ThTextSet
    {
        public static DBText ThText(Point3d position, string textString)
        {
            var text = new DBText
            {
                TextString = textString,
                Position = new Point3d(position.X + 50, position.Y + 50, 0),
                LayerId = DbHelper.GetLayerId("W-FRPT-HYDT-DIMS"),
                WidthFactor = 0.7,
                Height = 350,
                TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3")
            };
            return text;
        }

        public static DBText ThText(Point3d position, string textString, string layer)
        {
            var text = new DBText
            {
                TextString = textString,
                Position = new Point3d(position.X + 50, position.Y + 50, 0),
                LayerId = DbHelper.GetLayerId(layer),
                WidthFactor = 0.7,
                Height = 350,
                TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3")
            };
            return text;
        }


        public static Line ThTextLine(Point3d pt1, Point3d pt2)
        {
            var line = new Line(pt1, pt2)
            {
                LayerId = DbHelper.GetLayerId("W-FRPT-HYDT-DIMS")
            };
            return line;
        }

        public static DBText ThText(Point3d position, double rotation, string textString)
        {
            var text = new DBText
            {
                TextString = textString,
                Position = new Point3d(position.X + 50, position.Y + 50, 0),
                LayerId = DbHelper.GetLayerId("W-FRPT-HYDT-DIMS"),
                WidthFactor = 0.7,
                Height = 350,
                Rotation = rotation,
                TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3")
            };
            return text;
        }
    }
}
