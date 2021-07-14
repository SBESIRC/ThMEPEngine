using System;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Operation.OverlayNG;
using NetTopologySuite.Operation.Overlay;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSEntityExtension
    {
        public static Geometry ToNTSGeometry(this Entity obj)
        {
            if (obj is Curve curve)
            {
                return curve.ToNTSLineString();
            }
            else if (obj is DBPoint point)
            {
                return point.ToNTSPoint();
            }
            else if(obj is MPolygon mPolygon)
            {
                return mPolygon.ToNTSPolygon();
            }
            else if (obj is Region region)
            {
                return region.ToNTSPolygon();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static Polygon ToNTSPolygon(this Entity entity)
        {
            if (entity is Polyline polyline)
            {
                return polyline.ToNTSPolygon();
            }
            else if (entity is Circle circle)
            {
                return circle.ToNTSPolygon();
            }
            else if (entity is MPolygon mPolygon)
            {
                return mPolygon.ToNTSPolygon();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static LineString ToNTSLineString(this Curve curve)
        {
            if (curve is Line line)
            {
                return line.ToNTSLineString();
            }
            else if (curve is Polyline polyline)
            {
                return polyline.ToNTSLineString();
            }
            else if (curve is Polyline2d poly2d)
            {
                return poly2d.ToNTSLineString();
            }
            else if (curve is Circle circle)
            {
                return circle.ToNTSLineString();
            }
            else if (curve is Arc arc)
            {
                return arc.ToNTSLineString();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static DBObjectCollection Intersection(Entity first, Entity other, bool keepHoles = false)
        {
            return OverlayNGRobust.Overlay(
                first.ToNTSPolygon(),
                other.ToNTSPolygon(), 
                SpatialFunction.Intersection).ToDbCollection(keepHoles);
        }

        public static DBObjectCollection Difference(Entity entity, DBObjectCollection objs,bool keepHoles=false)
        {
            return OverlayNGRobust.Overlay(
                entity.ToNTSPolygon(),
                objs.UnionGeometries(),
                SpatialFunction.Difference).ToDbCollection(keepHoles);
        }
    }
}
