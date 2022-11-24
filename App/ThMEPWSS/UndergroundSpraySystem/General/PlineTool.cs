using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace ThMEPWSS.UndergroundSpraySystem.General
{
    public static class PlineTool
    {
        public static List<Line> Pline2Lines(this Polyline pline)
        {
            var lineList = new List<Line>();
            for (int i = 0; i < pline.NumberOfVertices - 1; i++)
            {
                Point3d ptPre = pline.GetPoint3dAt(i);
                Point3d ptNext = pline.GetPoint3dAt(i + 1);
                Point3d pt1 = new(ptPre.X, ptPre.Y, 0);
                Point3d pt2 = new(ptNext.X, ptNext.Y, 0);
                lineList.Add(new Line(pt1, pt2));
            }
            return lineList;
        }
    }
}
