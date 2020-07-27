using System;
using GeoAPI.Geometries;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Algorithm;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate;
using System.Collections.Generic;

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

        //public static List<Polyline> Difference(this Polyline pRegion, Region sRegion)
        //{
        //    var regions = new List<Polyline>();
        //    var pGeometry = pRegion.ToNTSPolygon();
        //    var sGeometry = sRegion.ToNTSPolygon();
        //    if (pGeometry == null || sGeometry == null)
        //    {
        //        return regions;
        //    }

        //    // 检查是否相交
        //    if (!pGeometry.Intersects(sGeometry))
        //    {
        //        return regions;
        //    }

        //    // 若相交，则计算在pRegion，但不在sRegion的部分
        //    var rGeometry = pGeometry.Difference(sGeometry);
        //    if (rGeometry is IPolygon polygon)
        //    {
        //        regions.Add(polygon.Shell.ToDbPolyline());
        //    }
        //    else if (rGeometry is IMultiPolygon mPolygon)
        //    {
        //        regions.AddRange(mPolygon.ToDbPolylines());
        //    }
        //    else
        //    {
        //        // 为止情况，抛出异常
        //        throw new NotSupportedException();
        //    }
        //    return regions;
        //}
    }
}
