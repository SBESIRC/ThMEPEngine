using System;
using GeoAPI.Geometries;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSTriangulate
    {
        public static DBObjectCollection VoronoiDiagram(this Polyline polyline)
        {
            var objs = new DBObjectCollection();
            var voronoiDiagram = new VoronoiDiagramBuilder();
            voronoiDiagram.SetSites(LineString.Empty.Union(polyline.ToNTSLineString()));
            var geometries = voronoiDiagram.GetDiagram(ThCADCoreNTSService.Instance.GeometryFactory);
            foreach (var geometry in geometries.Geometries)
            {
                if (geometry is IPolygon polygon)
                {
                    objs.Add(polygon.Shell.ToDbPolyline());
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return objs;
        }

        public static DBObjectCollection DelaunayTriangulation(this Polyline polyline)
        {
            var objs = new DBObjectCollection();
            var delaunayTriangulation = new DelaunayTriangulationBuilder();
            delaunayTriangulation.SetSites(LineString.Empty.Union(polyline.ToNTSLineString()));
            var triangles = delaunayTriangulation.GetTriangles(ThCADCoreNTSService.Instance.GeometryFactory);
            foreach (var geometry in triangles.Geometries)
            {
                if (geometry is IPolygon polygon)
                {
                    objs.Add(polygon.Shell.ToDbPolyline());
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return objs;
        }

        public static DBObjectCollection DelaunayTriangulation(this Point3dCollection points)
        {
            var objs = new DBObjectCollection();
            var delaunayTriangulation = new DelaunayTriangulationBuilder();
            delaunayTriangulation.SetSites(points.ToNTSCoordinates());
            var triangles = delaunayTriangulation.GetTriangles(ThCADCoreNTSService.Instance.GeometryFactory);
            foreach (var geometry in triangles.Geometries)
            {
                if (geometry is IPolygon polygon)
                {
                    objs.Add(polygon.Shell.ToDbPolyline());
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return objs;
        }
    }
}
