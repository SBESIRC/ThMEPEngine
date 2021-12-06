using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPEngineCore.UCSDivisionService.Utils
{
    public static class GeoUtils
    {
        public static Polyline GetLinesOutBox(List<Polyline> triangles)
        {
            var allLines = triangles.SelectMany(x => StructUtils.GetLinesByPolyline(x)).ToList();
            var outLines = allLines.Where(x => triangles.Where(y => y.Distance(x.StartPoint) < 1 && y.Distance(x.EndPoint) < 1).Count() < 2).ToList();
            var firLine = outLines.First();
            var sP = firLine.StartPoint;
            var eP = firLine.EndPoint;
            Polyline outBox = new Polyline() { Closed = true };
            int index = 0;
            while (outLines.Count > 0 && !firLine.EndPoint.IsEqualTo(sP, new Tolerance(1, 1)))
            {
                outLines = outLines.Where(x => !x.IsEqualLine(firLine)).ToList();
                firLine = outLines.Where(x => x.StartPoint.IsEqualTo(eP, new Tolerance(1, 1)) || x.EndPoint.IsEqualTo(eP, new Tolerance(1, 1))).FirstOrDefault();
                if (firLine == null) return outBox;

                var firEndPt = firLine.StartPoint.IsEqualTo(eP, new Tolerance(1, 1)) ? firLine.EndPoint : firLine.StartPoint;
                firLine = new Line(eP, firEndPt);
                outBox.AddVertexAt(index, eP.ToPoint2D(), 0, 0, 0);
                index++;
                eP = firEndPt;
            }
            outBox.AddVertexAt(index, sP.ToPoint2D(), 0, 0, 0);

            return outBox;
        }

        public static bool IsEqualLine(this Line line, Line otherLine)
        {
            return (line.StartPoint.IsEqualTo(otherLine.StartPoint) && line.EndPoint.IsEqualTo(otherLine.EndPoint)) ||
                (line.StartPoint.IsEqualTo(otherLine.EndPoint) && line.EndPoint.IsEqualTo(otherLine.StartPoint));
        }
    }
}
