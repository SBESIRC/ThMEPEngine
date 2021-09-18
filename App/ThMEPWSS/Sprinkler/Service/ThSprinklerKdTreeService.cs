using System.Linq;
using ThCADCore.NTS;
using Catel.Collections;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace ThMEPWSS.Sprinkler.Service
{
    public static class ThSprinklerKdTreeService
    {
        public static List<Point3d> QueryOther(ThCADCoreNTSKdTree kdTree, Point3d pt, double tolerance)
        {
            var list = new List<Point3d>();
            kdTree.Nodes.Where(o => o.Value[0].DistanceTo(pt) < tolerance)
                 .ForEach(o =>
                 {
                     list.Add(o.Key.Coordinate.ToAcGePoint3d());
                 });
            return list;
        }
    }
}
