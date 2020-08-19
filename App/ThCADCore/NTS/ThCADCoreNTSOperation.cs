using System;
using GeoAPI.Geometries;
using System.Collections.Generic;
using NetTopologySuite.Operation.Union;
using NetTopologySuite.Operation.Buffer;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Operation.Linemerge;
using NTSJoinStyle = GeoAPI.Operation.Buffer.JoinStyle;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSOperation
    {
        public static DBObjectCollection Trim(this Polyline polyline, Polyline curve)
        {
            var objs = new DBObjectCollection();
            var other = curve.ToNTSLineString();
            var polygon = polyline.ToNTSPolygon();
            var result = polygon.Intersection(other);
            if (result is IMultiLineString lineStrings)
            {
                foreach (ILineString lineString in lineStrings.Geometries)
                {
                    objs.Add(lineString.ToDbPolyline());
                }
            }
            else
            {
                throw new NotSupportedException();
            }
            return objs;
        }

        public static DBObjectCollection Merge(this DBObjectCollection lines)
        {
            var merger = new LineMerger();
            merger.Add(lines.ToNTSNodedLineStrings());
            var results = new DBObjectCollection();
            foreach (var geometry in merger.GetMergedLineStrings())
            {
                if (geometry is ILineString lineString)
                {
                    // 合并后的图元需要刷成合并前的图元的属性
                    // 假设合并的图元都有相同的属性
                    // 这里用集合中的第一个图元“刷”到合并后的图元
                    var result = lineString.Simplify();
                    result.SetPropertiesFrom(lines[0] as Entity);
                    results.Add(result);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return results;
        }

        public static IGeometry UnionGeometries(this DBObjectCollection curves)
        {
            return curves.ToNTSPolygonCollection().Buffer(0);
        }

        public static DBObjectCollection UnionPolygons(this DBObjectCollection curves)
        {
            var objs = new DBObjectCollection();
            var result = curves.UnionGeometries();
            if (result is IPolygon bufferPolygon)
            {
                objs.Add(bufferPolygon.Shell.ToDbPolyline());
            }
            else if (result is IMultiPolygon mPolygon)
            {
                foreach (IPolygon item in mPolygon.Geometries)
                {
                    objs.Add(item.Shell.ToDbPolyline());
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
            if (results is IMultiLineString geometries)
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

        public static DBObjectCollection Buffer(this Polyline polyline, double distance)
        {
            var objs = new DBObjectCollection();
            var polygon = polyline.ToNTSPolygon();
            var parameters = new BufferParameters()
            {
                JoinStyle = NTSJoinStyle.Mitre,
            };
            var buffer = BufferOp.Buffer(polygon, distance, parameters);
            if (buffer.IsEmpty)
            {
                return objs;
            }
            if (buffer is IPolygon bufferPolygon)
            {
                objs.Add(bufferPolygon.Shell.ToDbPolyline());
            }
            else if (buffer is IMultiPolygon mPolygon)
            {
                foreach(IPolygon item in mPolygon.Geometries)
                {
                    objs.Add(item.Shell.ToDbPolyline());
                }
            }
            else
            {
                throw new NotSupportedException();
            }
            return objs;
        }
    }
}
