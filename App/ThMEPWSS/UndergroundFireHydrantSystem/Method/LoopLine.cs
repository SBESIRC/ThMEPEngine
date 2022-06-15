using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Linq;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Method
{
    public class LoopLine
    {
        public static void Split(FireHydrantSystemOut fireHydrantSysOut, Point3d pt4, Point3d pt5)//横向管线打断
        {
            foreach (var line in fireHydrantSysOut.LoopLine.ToList())
            {
                if ((line.StartPoint.X - pt4.X) * (line.EndPoint.X - pt4.X) < 0 &&
                     line.StartPoint.Y > pt4.Y && line.StartPoint.Y < pt5.Y)
                {
                    fireHydrantSysOut.LoopLine.Remove(line);
                    Line line1, line2;
                    if (line.StartPoint.X < line.EndPoint.X)
                    {
                        line1 = new Line(line.StartPoint, new Point3d(pt4.X - 100, line.StartPoint.Y, 0));
                        line2 = new Line(line.EndPoint, new Point3d(pt4.X + 100, line.StartPoint.Y, 0));
                    }
                    else
                    {
                        line1 = new Line(line.EndPoint, new Point3d(pt4.X - 100, line.StartPoint.Y, 0));
                        line2 = new Line(line.StartPoint, new Point3d(pt4.X + 100, line.StartPoint.Y, 0));
                    }
                    fireHydrantSysOut.LoopLine.Add(line1);
                    fireHydrantSysOut.LoopLine.Add(line2);
                }
            }
        }
    }
}
