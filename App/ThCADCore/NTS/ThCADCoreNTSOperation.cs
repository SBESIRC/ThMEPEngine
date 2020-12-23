using System;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Buffer;
using Autodesk.AutoCAD.DatabaseServices;
using NTSJoinStyle = NetTopologySuite.Operation.Buffer.JoinStyle;
using NetTopologySuite.Operation.Linemerge;
using NFox.Cad;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSOperation
    {
        public static DBObjectCollection Trim(this Polyline polyline, Curve curve)
        {
            return ThCADCoreNTSGeometryClipper.Clip(polyline, curve);
        }

        public static DBObjectCollection Buffer(this Polyline polyline, double distance)
        {
            var buffer = new BufferOp(polyline.ToNTSPolygon(), new BufferParameters()
            {
                JoinStyle = NTSJoinStyle.Mitre,
            });
            return buffer.GetResultGeometry(distance).ToDbCollection();
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

        public static DBObjectCollection Buffer(this DBObjectCollection objs, double distance,
            EndCapStyle endCapStyle = EndCapStyle.Flat)
        {
            var buffer = new BufferOp(objs.ToMultiLineString(), new BufferParameters()
            {
                JoinStyle = NTSJoinStyle.Mitre,
                EndCapStyle = endCapStyle,
            });
            return buffer.GetResultGeometry(distance).ToDbCollection();
        }

        public static DBObjectCollection SingleSidedBuffer(this DBObjectCollection objs, double distance,
            EndCapStyle endCapStyle = EndCapStyle.Flat)
        {
            var buffer = new BufferOp(objs.ToMultiLineString(), new BufferParameters()
            {
                IsSingleSided = true,
                JoinStyle = NTSJoinStyle.Mitre,
                EndCapStyle = endCapStyle,
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

        public static DBObjectCollection BuildArea(this DBObjectCollection objs)
        {
            var poylgons = new DBObjectCollection();
            var builder = new ThCADCoreNTSBuildArea();
            Geometry geometry = builder.Build(objs.Explode().ToMultiLineString());
            if (geometry is Polygon polygon)
            {
                poylgons.Add(polygon.ToDbEntity());
            }
            else if (geometry is MultiPolygon mPolygons)
            {
                mPolygons.Geometries.Cast<Polygon>().ForEach(o =>
                {
                    poylgons.Add(o.ToDbEntity());
                });
            }
            else
            {
                throw new NotSupportedException();
            }
            return poylgons;
        }
    }
}
