using System;
using NFox.Cad;
using System.Linq;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
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

        public static DBObjectCollection Buffer(this Polyline polyline, double distance)
        {
            var buffer = new BufferOp(polyline.ToNTSPolygon(), new BufferParameters()
            {
                JoinStyle = NTSJoinStyle.Mitre,
            });
            return buffer.GetResultGeometry(distance).ToDbCollection();
        }
        public static DBObjectCollection Buffer(this Polyline polyline, double distance,bool keepHole)
        {
            var buffer = new BufferOp(polyline.ToNTSPolygon(), new BufferParameters()
            {
                JoinStyle = NTSJoinStyle.Mitre,
            });
            return buffer.GetResultGeometry(distance).ToDbCollection(keepHole);
        }

        public static Polyline Buffer(this Line line, double distance)
        {
            return line.ToNTSLineString().Buffer(distance, EndCapStyle.Flat).ToDbObjects()[0] as Polyline;
        }

        public static DBObjectCollection BufferPL(this Polyline polyline, double distance)
        {
            var buffer = new BufferOp(polyline.ToNTSLineString(), new BufferParameters()
            {
                JoinStyle = NTSJoinStyle.Mitre,
                EndCapStyle = EndCapStyle.Square,
            });
            return buffer.GetResultGeometry(distance).ToDbCollection();
        }

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

            // 对每个Polygon进行修复
            var filters = new DBObjectCollection();
            objs.OfType<Polyline>().ForEach(o =>
            {
                o.Fix().OfType<Polyline>().ForEach(e => filters.Add(e));
            });
            if (filters.Count == 0)
            {
                return new DBObjectCollection();
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
            return builder.Build(objs.ToMultiLineString());
        }
    }
    public enum ThBufferEndCapStyle
    {
        //
        // 摘要:
        //     Map NTS EndCapStyle's Round style.
        Round = 1,
        //
        // 摘要:
        //     Map NTS EndCapStyle's Flat style.
        Flat = 2,
        //
        // 摘要:
        //     Map NTS EndCapStyle's Square style.
        Square = 3
    }
}
