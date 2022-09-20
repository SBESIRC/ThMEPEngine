using System;
using NFox.Cad;
using System.Linq;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using NetTopologySuite.Algorithm.Hull;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Operation.Linemerge;
using Autodesk.AutoCAD.DatabaseServices;
using NTSJoinStyle = NetTopologySuite.Operation.Buffer.JoinStyle;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSOperation
    {
        public static DBObjectCollection Trim(this Polyline polyline, Curve curve, bool inverted = false)
        {
            return ThCADCoreNTSGeometryClipper.Clip(polyline, curve, inverted);
        }

        /// <summary>
        /// 只支持闭合Polyline的Buffer
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="distance"></param>
        /// <param name="keepHole"></param>
        /// <returns></returns>
        public static DBObjectCollection Buffer(this Polyline polyline, double distance, bool keepHole = false)
        {
            var buffer = new BufferOp(polyline.ToNTSPolygon(), new BufferParameters()
            {
                JoinStyle = NTSJoinStyle.Mitre,
            });
            return buffer.GetResultGeometry(distance).ToDbCollection(keepHole);
        }

        /// <summary>
        /// 支持Polyline的Square类型Buffer
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static DBObjectCollection BufferPL(this Polyline polyline, double distance)
        {
            var buffer = new BufferOp(polyline.ToNTSLineString(), new BufferParameters()
            {
                JoinStyle = NTSJoinStyle.Mitre,
                EndCapStyle = EndCapStyle.Square,
            });
            return buffer.GetResultGeometry(distance).ToDbCollection();
        }

        /// <summary>
        /// 支持Polyline的Flat类型Buffer
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static DBObjectCollection BufferFlatPL(this Polyline polyline, double distance)
        {
            var buffer = new BufferOp(polyline.ToNTSLineString(), new BufferParameters()
            {
                JoinStyle = NTSJoinStyle.Mitre,
                EndCapStyle = EndCapStyle.Flat,
            });
            return buffer.GetResultGeometry(distance).ToDbCollection();
        }

        public static DBObjectCollection Buffer(this DBObjectCollection objs, double distance)
        {
            var buffer = new BufferOp(objs.ToMultiLineString(), new BufferParameters()
            {
                JoinStyle = NTSJoinStyle.Mitre,
                EndCapStyle = EndCapStyle.Flat,
            });
            return buffer.GetResultGeometry(distance).ToDbCollection();
        }

        public static DBObjectCollection SingleSidedBuffer(this DBObjectCollection objs, double distance)
        {
            var buffer = new BufferOp(objs.ToMultiLineString(), new BufferParameters()
            {
                IsSingleSided = true,
                JoinStyle = NTSJoinStyle.Mitre,
                EndCapStyle = EndCapStyle.Flat,
            });
            return buffer.GetResultGeometry(distance).ToDbCollection();
        }

        public static DBObjectCollection LineMerge(this DBObjectCollection objs)
        {
            var merger = new LineMerger();
            var results = new List<DBObject>();
            merger.Add(objs.ToNTSNodedLineStrings());
            merger.GetMergedLineStrings().ForEach(o =>
            {
                results.AddRange(o.ToDbObjects());
            });
            return results.ToCollection();
        }

        public static DBObjectCollection BuildArea(this DBObjectCollection objs)
        {
            // 处理弧线（椭圆）
            objs = objs.Tessellate();

            // 暂时只支持多段线
            // 对每个多段线进行修复
            var filters = new DBObjectCollection();
            objs.OfType<Polyline>().ForEach(o =>
            {
                foreach (Entity e in o.Fix())
                {
                    filters.Add(e);
                }
            });
            if (filters.Count == 0 || filters.Count == 1)
            {
                return filters;
            }

            // 获取面域
            var poylgons = new DBObjectCollection();
            var geometry = filters.BuildAreaGeometry();
            if (geometry is Polygon polygon)
            {
                poylgons.AddGeometryToDbCollection(polygon);
            }
            else if (geometry is MultiPolygon mPolygons)
            {
                mPolygons.Geometries.OfType<Polygon>().ForEach(o =>
                {
                    poylgons.AddGeometryToDbCollection(o);
                });
            }
            return poylgons;
        }

        private static void AddGeometryToDbCollection(this DBObjectCollection objs, Polygon polygon)
        {
            // 若仅给定固定精度，则无法处理狭长区域的舍入问题，故利用区域长度和面积进行判断
            // 狭长区域底边近似于Polygon.Length的一半，利用近似面积公式S=l*h，忽略平均高度小于2的区域
            if (polygon.Area > 1.0 && polygon.Area > polygon.Length)
            {
                if (polygon.NumInteriorRings > 0)
                {
                    polygon.ToDbMPolygonEx(1.0).OfType<Entity>().ForEach(o => objs.Add(o));
                }
                else if (polygon.Shell != null)
                {
                    objs.Add(polygon.Shell.ToDbPolyline());
                }
            }
        }

        public static MPolygon BuildMPolygon(this DBObjectCollection objs)
        {
            Geometry geometry = objs.BuildAreaGeometry();
            if (geometry is Polygon polygon)
            {
                return polygon.ToDbMPolygon();
            }
            else if (geometry is MultiPolygon mPolygons)
            {
                return mPolygons.ToDbMPolygon();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static Geometry BuildAreaGeometry(this DBObjectCollection objs)
        {
            var builder = new ThCADCoreNTSBuildArea
            {
                DissolveSharedEdges = false
            };
            return builder.Build(objs.ToGeometryCollection());
        }

        public static Geometry OuterOutline(this DBObjectCollection objs)
        {
            var plines = objs.Fix().OfType<Polyline>().ToCollection();
            if (plines.ToNTSMultiPolygon() is MultiPolygon polygons)
            {
                return ConcaveHullOfPolygons.ConcaveHullByLengthRatio(polygons, 0.05, true, false);
            }
            return ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon();
        }
    }
}
