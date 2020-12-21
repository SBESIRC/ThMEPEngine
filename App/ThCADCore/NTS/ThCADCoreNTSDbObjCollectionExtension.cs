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
            // The buffer(0) trick is sometimes faster, 
            // but can be less robust and can sometimes take a long time to complete. 
            // This is particularly the case where there is a high degree of overlap between the polygons. 
            // In this case, buffer(0) is forced to compute with all line segments from the outset, 
            // whereas cascading can eliminate many segments at each stage of processing. 
            // The best situation for using buffer(0) is the trivial case where 
            // there is no overlap between the input geometries. 
            // However, this case is likely rare in practice.
            return curves.ToNTSMultiPolygon().Buffer(0);
        }

        public static DBObjectCollection UnionPolygons(this DBObjectCollection curves)
        {
            if (curves.Count <= 0)
            {
                return curves;
            }

            var objs = new DBObjectCollection();
            var result = CascadedPolygonUnion.Union(curves.ToNTSPolygons().ToArray());
            if (result is Polygon bufferPolygon)
            {
                foreach (var poly in bufferPolygon.ToDbPolylines())
                {
                    objs.Add(poly);
                }
            }
            else if (result is MultiPolygon mPolygon)
            {
                foreach (Polygon item in mPolygon.Geometries)
                {
                    foreach (var poly in item.ToDbPolylines())
                    {
                        objs.Add(poly);
                    }
                }
            }
            else
            {
                throw new NotSupportedException();
            }
            return objs;
        }

        public static DBObjectCollection UnionLineStrings(this DBObjectCollection curves)
        {
            // Unioning a set of LineStrings has the effect of noding and dissolving the input linework. 
            // In this context "fully noded" means that there will be an endpoint or 
            // node in the result for every endpoint or line segment crossing in the input. 
            // "Dissolved" means that any duplicate (i.e. coincident) line segments or 
            // portions of line segments will be reduced to a single line segment in the result.
            var results = UnaryUnionOp.Union(curves.ToNTSLineStrings());
            if (results is MultiLineString geometries)
            {
                var objs = new DBObjectCollection();
                geometries.ToDbPolylines().ForEach(o => objs.Add(o));
                return objs;
            }
            else
            {
                throw new NotSupportedException();
            }
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