using System.Collections.Generic;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.BoundaryRepresentation;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSHatchExtension
    {
        public static Polygon ConvexHull(this Hatch hatch)
        {
            var pts = hatch.Vertices();
            var convexHull = new ConvexHull(pts.ToArray(), 
                ThCADCoreNTSService.Instance.GeometryFactory);
            return convexHull.GetConvexHull() as Polygon;
        }

        private static List<Coordinate> Vertices(this Hatch hatch)
        {
            var coordinates = new List<Coordinate>();
            using (var brepHatch = new Brep(hatch))
            {
                foreach (var vertex in brepHatch.Vertices)
                {
                    coordinates.Add(vertex.Point.ToNTSCoordinate());
                }
            }
            return coordinates;
        }
    }
}
