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
                return curve.ToNTSLinealGeometry();
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

        public static Polygon ToNTSPolygonalGeometry(this Entity entity)
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
            else if (entity is Ellipse ellipse)
            {
                return ellipse.ToNTSPolygon();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static LineString ToNTSLinealGeometry(this Curve curve)
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
            else if (curve is Ellipse ellipse)
            {
                return ellipse.ToNTSLineString();
            }
            else if (curve is Spline spline)
            {
                return spline.ToNTSLineString();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static DBObjectCollection Intersection(Entity first, Entity other, bool keepHoles = false)
        {
            return OverlayNGRobust.Overlay(
                first.ToNTSPolygonalGeometry(),
                other.ToNTSPolygonalGeometry(), 
                SpatialFunction.Intersection).ToDbCollection(keepHoles);
        }

        public static DBObjectCollection Intersection(Entity entity, DBObjectCollection objs, bool keepHoles = false)
        {
            return OverlayNGRobust.Overlay(
                entity.ToNTSPolygonalGeometry(),
                objs.UnionGeometries(),
                SpatialFunction.Intersection).ToDbCollection(keepHoles);
        }

        public static DBObjectCollection Difference(Entity entity, DBObjectCollection objs,bool keepHoles=false)
        {
            return OverlayNGRobust.Overlay(
                entity.ToNTSPolygonalGeometry(),
                objs.UnionGeometries(),
                SpatialFunction.Difference).ToDbCollection(keepHoles);
        }
    }
}
