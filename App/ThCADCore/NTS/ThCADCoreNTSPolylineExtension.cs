using System;
using GeoAPI.Geometries;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSPolylineExtension
    {
        public static Circle MinimumBoundingCircle(this Polyline polyline)
        {
            var mbc = new MinimumBoundingCircle(polyline.ToNTSLineString());
            return new Circle(mbc.GetCentre().ToAcGePoint3d(), Vector3d.ZAxis, mbc.GetRadius());
        }

        public static Polyline MinimumBoundingBox(this Polyline polyline)
        {
            var geometry = polyline.ToNTSLineString().Envelope;
            if (geometry is ILineString lineString)
            {
                return lineString.ToDbPolyline();
            }
            else if (geometry is ILinearRing linearRing)
            {
                return linearRing.ToDbPolyline();
            }
            else if (geometry is IPolygon polygon)
            {
                return polygon.Shell.ToDbPolyline();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static Polyline ConvexHull(this Polyline polyline)
        {
            var convexHull = new ConvexHull(polyline.ToNTSLineString());
            var geometry = convexHull.GetConvexHull();
            if (geometry is IPolygon polygon)
            {
                return polygon.Shell.ToDbPolyline();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static Polyline GetOctagonalEnvelope(this Polyline polyline)
        {
            var geometry = OctagonalEnvelope.GetOctagonalEnvelope(polyline.ToNTSLineString());
            if (geometry is IPolygon polygon)
            {
                return polygon.Shell.ToDbPolyline();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static DBObjectCollection VoronoiDiagram(this Polyline polyline)
        {
            var objs = new DBObjectCollection();
            var voronoiDiagram = new VoronoiDiagramBuilder();
            voronoiDiagram.SetSites(LineString.Empty.Union(polyline.ToNTSLineString()));
            var geometries = voronoiDiagram.GetDiagram(ThCADCoreNTSService.Instance.GeometryFactory);
            foreach(var geometry in geometries.Geometries)
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

        public static DBObjectCollection Difference(this Polyline region, DBObjectCollection curves)
        {
            var objs = new DBObjectCollection();
            var geometry = region.ToNTSPolygon().Difference(curves.UnionGeometries());
            if (geometry.IsEmpty)
            {
                return objs;
            }
            if (geometry is IPolygon polygon)
            {
                objs.Add(polygon.Shell.ToDbPolyline());
                foreach(var hole in polygon.Holes)
                {
                    objs.Add(hole.ToDbPolyline());
                }
            }
            else if (geometry is IMultiPolygon mPolygon)
            {
                foreach(IPolygon subPolygon in mPolygon.Geometries)
                {
                    objs.Add(subPolygon.Shell.ToDbPolyline());
                    foreach (var hole in subPolygon.Holes)
                    {
                        objs.Add(hole.ToDbPolyline());
                    }
                }
            }
            return objs;
        }

        public static bool Contains(this Polyline thisPline, Polyline otherPline)
        {
            return thisPline.ToNTSPolygon().Contains(otherPline.ToNTSPolygon());
        }

        public static bool Contains(this Polyline thisPline, Point3d pt)
        {
            return thisPline.PointInPolygon(pt) == LocateStatus.Interior;
        }
    }
}
