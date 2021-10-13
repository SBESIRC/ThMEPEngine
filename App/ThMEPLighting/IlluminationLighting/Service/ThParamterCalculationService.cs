using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPLighting.IlluminationLighting.Service
{
    class ThParamterCalculationService
    {
        public static List<Polyline> getPriorityBoundary(Dictionary<Point3d, Vector3d> layoutPts, double scale, (double, double) size)
        {
            var blkBoundary = new List<Polyline>();

            foreach (var blk in layoutPts)
            {
                var boundary = getBoundary(blk.Key, blk.Value, scale, size);
                blkBoundary.Add(boundary);
            }

            return blkBoundary;
        }

        private static Polyline getBoundary(Point3d pt, Vector3d dir, double scale, (double,double)size)
        {
            var xDir = dir.RotateBy(90 * Math.PI / 180, -Vector3d.ZAxis).GetNormal();
            var pt0 = pt + dir * (size.Item2 * scale / 2) - xDir * (size.Item1 * scale / 2);
            var pt1 = pt + dir * (size.Item2 * scale / 2) + xDir * (size.Item1 * scale / 2);
            var pt2 = pt - dir * (size.Item2 * scale / 2) + xDir * (size.Item1 * scale / 2);
            var pt3 = pt - dir * (size.Item2 * scale / 2) - xDir * (size.Item1 * scale / 2);

            var boundray = new Polyline();
            boundray.AddVertexAt(boundray.NumberOfVertices, pt0.ToPoint2D(), 0, 0, 0);
            boundray.AddVertexAt(boundray.NumberOfVertices, pt1.ToPoint2D(), 0, 0, 0);
            boundray.AddVertexAt(boundray.NumberOfVertices, pt2.ToPoint2D(), 0, 0, 0);
            boundray.AddVertexAt(boundray.NumberOfVertices, pt3.ToPoint2D(), 0, 0, 0);
            boundray.Closed = true;

            return boundray;
        }

        public static double getPriorityExtendValue(List<string> blkNameList, double scale)
        {
            double extend = -1;
            var size = new List<double>();
            size.AddRange(blkNameList.Select(x => ThIlluminationCommon.blk_size[x].Item1));
            size.AddRange(blkNameList.Select(x => ThIlluminationCommon.blk_size[x].Item2));

            extend = size.OrderByDescending(x => x).First();
            extend = extend * scale / 2;
            return extend;
        }

    }
}
