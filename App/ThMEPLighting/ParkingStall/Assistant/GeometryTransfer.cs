using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Geometry;

namespace ThMEPLighting.ParkingStall.Assistant
{
    public static class GeometryTransfer
    {
        public static List<Curve> Polylines2Curves(this List<Polyline> srcPolylines)
        {
            if (srcPolylines == null || srcPolylines.Count == 0)
                return null;
            var curves = new List<Curve>();

            foreach (var polyline in srcPolylines)
            {
                curves.Add(polyline);
            }

            return curves;
        }

        public static Polyline Points2Poly(List<Point3d> pts)
        {
            if (pts == null || pts.Count < 3)
                return null;

            var ptFirst = pts.First();
            var ptLast = pts.Last();

            if (GeomUtils.Point3dIsEqualPoint3d(ptFirst, ptLast))
                pts.Remove(ptLast);

            var poly = new Polyline()
            {
                Closed = true
            };

            for (int i = 0; i < pts.Count; i++)
            {
                poly.AddVertexAt(i, pts[i].ToPoint2D(), 0, 0, 0);
            }
            return poly;
        }
    }
}
