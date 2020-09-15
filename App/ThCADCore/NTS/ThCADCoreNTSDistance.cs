using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Algorithm.Distance;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSDistance
    {
        public static double Distance(this Point3d point, Polyline polyline)
        {
            var distance = new PointPairDistance();
            DistanceToPoint.ComputeDistance(polyline.ToNTSLineString(), 
                point.ToNTSCoordinate(), distance);
            return distance.Distance;
        }
    }
}
