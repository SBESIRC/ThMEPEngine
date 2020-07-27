using GeoAPI.Geometries;
using NetTopologySuite.Algorithm;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.BoundaryRepresentation;

namespace ThCADCore.NTS
{
    public static class ThCADCoreHatchExtension
    {
        public static IPolygon ConvexHull(this Hatch hatch)
        {
            var pts = hatch.Vertices();
            var convexHull = new ConvexHull(pts.ToArray(), 
                ThCADCoreNTSService.Instance.GeometryFactory);
            return convexHull.GetConvexHull() as IPolygon;
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
