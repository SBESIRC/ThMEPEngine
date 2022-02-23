using System;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPElectrical.SystemDiagram.Extension;

namespace TianHua.Electrical.PDS.Service
{
    public static class ThPDSBufferService
    {
        public static Polyline Buffer(Entity entity, Database database, double distance = ThPDSCommon.AllowableTolerance)
        {
            if (entity is Curve curve)
            {
                if (curve is Line line)
                {
                    return line.Buffer(distance);
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
                return rectangle.Buffer(distance)[0] as Polyline;
            }
            else
            {
                //不支持其他的数据类型
                throw new NotSupportedException();
            }
        }
    }
}
