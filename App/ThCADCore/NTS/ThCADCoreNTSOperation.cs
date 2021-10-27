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
            return results.ToCollection<DBObject>();
        }

        public static DBObjectCollection BuildArea(this DBObjectCollection objs, bool dissolveSharedEdges = true)
        {
            var poylgons = new DBObjectCollection();
            var filters = new DBObjectCollection();
            objs.OfType<Entity>().ForEach(o =>
            {
                if (o is Polyline polyline)
                {
                    polyline.Fix().OfType<Entity>().ForEach(e => filters.Add(e));
                }
                else
                {
                    filters.Add(o);
                }
            });
            if(filters.Count > 0)
            {
                var geometry = filters.BuildAreaGeometry(dissolveSharedEdges);
                if (geometry is Polygon polygon)
                {
                    poylgons.Add(polygon.ToDbEntity());
                }
                else if (geometry is MultiPolygon mPolygons)
                {
                    // 若仅给定固定精度，则无法处理狭长区域的舍入问题，故利用区域长度和面积进行判断
                    // 狭长区域底边近似于Polygon.Length的一半，利用近似面积公式S=l*h，忽略平均高度小于2的区域
                    mPolygons.Geometries.OfType<Polygon>().Where(o => o.Area > 1.0 && o.Area > o.Length).ForEach(o =>
                    {
                        o.ToDbCollection().OfType<Entity>().ForEach(e => poylgons.Add(e));
                    });
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return poylgons;
        }

        public static MPolygon BuildMPolygon(this DBObjectCollection objs)
        {
            Geometry geometry = objs.BuildAreaGeometry();
            if (geometry is Polygon polygon)
            {
                return polygon.ToDbMPolygonEx().OfType<MPolygon>().First();
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

        public static Geometry BuildAreaGeometry(this DBObjectCollection objs, bool dissolveSharedEdges = true)
        {
            var builder = new ThCADCoreNTSBuildArea();
            builder.DissolveSharedEdges = dissolveSharedEdges;
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
