using System;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPElectrical.SystemDiagram.Extension;
using ThMEPEngineCore.CAD;

namespace TianHua.Electrical.PDS.Service
{
    public static class ThPDSBufferService
    {
        public static Polyline Buffer(Entity entity, Database database, double distance = ThPDSCommon.ALLOWABLE_TOLERANCE)
        {
            if (entity is Curve curve)
            {
                if (curve is Line line)
                {
                    return line.ExtendLine(distance).Buffer(distance);
                }
                else if (curve is Arc arc)
                {
                    var objs = arc.TessellateArcWithArc(100.0).BufferPL(distance);
                    return objs[0] as Polyline;
                }
                else if (curve is Polyline polyline)
                {
                    var objs = polyline.BufferPL(distance);
                    return objs.OfType<Polyline>().OrderByDescending(o => o.Length).First();
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else if (entity is BlockReference blk)
            {
                var rectangle = database.GetBlockReferenceOBB(blk);
                if (rectangle.Area > 1.0)
                {
                    return rectangle.Buffer(distance)[0] as Polyline;
                }
                else
                {
                    return new Point3d(0.01, 0.01, 0.01).CreateSquare(0.01);
                }
            }
            else
            {
                //不支持其他的数据类型
                throw new NotSupportedException();
            }
        }

        public static Polyline Buffer(Entity entity, double distance = ThPDSCommon.ALLOWABLE_TOLERANCE)
        {
            if (entity is Curve curve)
            {
                if (curve is Line line)
                {
                    return line.ExtendLine(distance).Buffer(distance);
                }
                else if (curve is Arc arc)
                {
                    var objs = arc.TessellateArcWithArc(100.0).BufferPL(distance);
                    return objs[0] as Polyline;
                }
                else if (curve is Polyline polyline)
                {
                    var objs = polyline.BufferPL(distance);
                    return objs.OfType<Polyline>().OrderByDescending(o => o.Length).First();
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else
            {
                //不支持其他的数据类型
                throw new NotSupportedException();
            }
        }
    }
}
