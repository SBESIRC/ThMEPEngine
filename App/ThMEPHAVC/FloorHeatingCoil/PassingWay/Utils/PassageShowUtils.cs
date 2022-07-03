using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FloorHeatingCoil.PassageWay
{
    class PassageShowUtils
    {
        public static void ShowPoint(Point3d p, int color_index = 2)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var circle = new Circle(p, Vector3d.ZAxis, 50);
                circle.ColorIndex = color_index;
                acadDatabase.ModelSpace.Add(circle);
            }
        }
        public static void ShowPoints(List<Point3d> points, int color_index = 2)
        {
            for (int i = 0; i < points.Count; ++i)
                ShowText(points[i], i.ToString(), color_index);
        }
        public static void ShowEntity(Entity entity, int color_index = 2)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                entity.ColorIndex = color_index;
                acadDatabase.ModelSpace.Add(entity);
            }
        }
        public static void ShowText(Point3d p, string str, int color_index = 2)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                DBText text = new DBText();
                text.Position = p;
                text.TextString = str;
                text.Rotation = 0;
                text.Height = 200;
                text.ColorIndex = color_index;
                acadDatabase.ModelSpace.Add(text);
            }
        }
        public static void PrintMassage(string str)
        {
            System.Diagnostics.Trace.WriteLine(str);
        }
    }
}
