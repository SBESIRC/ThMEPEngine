using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    class ThTextSet
    {
        public static DBText ThText(Point3d position, string textString)
        {
            var text = new DBText();
            text.TextString = textString;
            text.Position = new Point3d(position.X + 50, position.Y + 50 ,0);
            text.LayerId = DbHelper.GetLayerId("W-FRPT-HYDT-DIMS");
            text.WidthFactor = 0.7;
            text.Height = 350;
            text.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
            return text;
        }

        public static Line ThTextLine(Point3d pt1, Point3d pt2)
        {
            var line = new Line(pt1, pt2);
            line.LayerId = DbHelper.GetLayerId("W-FRPT-HYDT-DIMS");
            return line;
        }

        
    }
        
}
