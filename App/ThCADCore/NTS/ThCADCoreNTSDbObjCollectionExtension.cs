using System;
using NFox.Cad;
using System.Linq;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Union;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Geometries.Utilities;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSDbObjCollectionExtension
    {
        public static MultiPolygon ToNTSMultiPolygon(this DBObjectCollection objs)
        {
            return ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiPolygon(objs.ToNTSPolygons().ToArray());
        }

        public static List<Polygon> ToNTSPolygons(this DBObjectCollection curves)
        {
            var polygons = new List<Polygon>();
            curves.Cast<Entity>().ForEach(e => polygons.Add(e.ToNTSPolygon()));
            return polygons;
        }

        public static List<Geometry> ToNTSLineStrings(this DBObjectCollection curves)
        {
            var geometries = new List<Geometry>();
            curves.Cast<Entity>().ForEach(e => geometries.Add(e.ToNTSGeometry()));
            return geometries;
        }

        public static Geometry ToNTSNodedLineStrings(this DBObjectCollection curves)
        {
            return UnaryUnionOp.Union(curves.ToNTSLineStrings());
        }

        public static Geometry UnionGeometries(this DBObjectCollection curves)
        {
            // https://lin-ear-th-inking.blogspot.com/2007/11/fast-polygon-merging-in-jts-using.html
            return curves.ToNTSMultiPolygon().Union();
        }

        public static DBObjectCollection UnionPolygons(this DBObjectCollection curves)
        {
            return curves.UnionGeometries().ToDbCollection();
        }

        public static Polyline GetMinimumRectangle(this DBObjectCollection curves)
        {
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

        public static MultiLineString ToMultiLineString(this DBObjectCollection curves)
        {
            var geometries = new List<Geometry>();
            foreach (Curve curve in curves)
            {
                geometries.Add(curve.ToNTSGeometry());
            }
            // 暂时过滤掉Polygon
            var lineStrings = geometries.Where(o => o is LineString).Cast<LineString>();
            return ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiLineString(lineStrings.ToArray());
        }

        public static Geometry Combine(this DBObjectCollection curves)
        {
            var geometries = curves.ToNTSLineStrings();
            return GeometryCombiner.Combine(geometries);
        }

        /// <summary>
        /// 支持MPolygon 数据格式的流转
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public static DBObjectCollection ToDBCollectionMP(this Geometry geometry)
        {
            return geometry.ToDbObjectsMP().ToCollection<DBObject>();
        }

        public static DBObjectCollection ToDbCollection(this Geometry geometry)
        {
            return geometry.ToDbObjects().ToCollection<DBObject>();
        }
    }
}