﻿using System;
using NFox.Cad;
using System.Linq;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.OverlayNG;
using NetTopologySuite.Geometries.Utilities;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSDbObjCollectionExtension
    {
        public static MultiPolygon ToNTSMultiPolygon(this DBObjectCollection objs)
        {
            var polygons = objs.Cast<Entity>().Select(o => o.ToNTSPolygon());
            return ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiPolygon(polygons.ToArray());
        }

        public static MultiLineString ToMultiLineString(this DBObjectCollection objs)
        {
            var lineStrings = objs.Cast<Curve>().Select(o => o.ToNTSLineString());
            return ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiLineString(lineStrings.ToArray());
        }

        public static Geometry ToNTSNodedLineStrings(this MultiLineString linestrings)
        {
            // UnaryUnionOp.Union()有Robust issue
            // 会抛出"non-noded intersection" TopologyException
            // OverlayNGRobust.Union()在某些情况下仍然会抛出TopologyException (NTS 2.2.0)
            Geometry lineString = ThCADCoreNTSService.Instance.GeometryFactory.CreateLineString();
            return OverlayNGRobust.Overlay(linestrings, lineString, SpatialFunction.Union);
        }

        public static Geometry ToNTSNodedLineStrings(this DBObjectCollection objs)
        {
            return objs.ToMultiLineString().ToNTSNodedLineStrings();
        }

        public static Geometry UnionGeometries(this DBObjectCollection curves)
        {
            return OverlayNGRobust.Union(curves.ToNTSMultiPolygon());
        }

        public static DBObjectCollection UnionPolygons(this DBObjectCollection curves)
        {
            return curves.UnionGeometries().ToDbCollection();
        }

        public static Geometry Intersection(this DBObjectCollection curves, Curve curve)
        {
            return OverlayNGRobust.Overlay(
                curves.ToMultiLineString(),
                curve.ToNTSGeometry(),
                SpatialFunction.Intersection);
        }

        public static Polyline GetMinimumRectangle(this DBObjectCollection curves)
        {
            // GetMinimumRectangle()对于非常远的坐标（WCS下，>10E10)处理的不好
            // Workaround就是将位于非常远的图元临时移动到WCS原点附近，参与运算
            // 运算结束后将运算结果再按相同的偏移从WCS原点附近移动到其原始位置
            var geometry = curves.Combine();
            var rectangle = MinimumDiameter.GetMinimumRectangle(geometry);
            if (rectangle is Polygon polygon)
            {
                return polygon.Shell.ToDbPolyline();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static Geometry Combine(this DBObjectCollection curves)
        {
            return GeometryCombiner.Combine(curves.ToMultiLineString().Geometries);
        }

        public static DBObjectCollection ToDbCollection(this Geometry geometry, bool keepHoles = false)
        {
            return geometry.ToDbObjects(keepHoles).ToCollection();
        }

        public static Point3d GetMaximumInscribedCircleCenter(this DBObjectCollection curves)
        {
            var builder = new ThCADCoreNTSBuildArea();
            var geometry = builder.Build(curves.ToMultiLineString());
            if (geometry is Polygon polygon)
            {
                return polygon.GetMaximumInscribedCircleCenter();
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}