using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.HydrantConnectPipe.Model
{
    public class ThHydrantPipe : ThIfcElement
    {
        public Point3d PipePosition { set; get; }
        public Polyline Obb { set; get; }
        public ThHydrantPipe()
        {
            PipePosition = new Point3d();
            Obb = new Polyline();
        }
        public static ThHydrantPipe Create(Entity data)
        {
            var fireHydrantPipe = new ThHydrantPipe
            {
                Uuid = Guid.NewGuid().ToString(),
                Outline = data
            };
            if(data is Circle)
            {
                var circle = data as Circle;
                fireHydrantPipe.PipePosition = new Point3d(circle.Center.X, circle.Center.Y,0);
                fireHydrantPipe.Obb = circle.ToRectangle();
            }

            return fireHydrantPipe;
        }
        public static ThHydrantPipe Create(ThRawIfcDistributionElementData data)
        {
            var fireHydrantPipe = new ThHydrantPipe
            {
                Uuid = Guid.NewGuid().ToString(),
                Outline = data.Geometry,
                Obb = data.Data as Polyline
            };
            if (data.Geometry is BlockReference)
            {
                var blk = data.Geometry as BlockReference;
                fireHydrantPipe.PipePosition = new Point3d(blk.Position.X, blk.Position.Y, 0);
            }
            return fireHydrantPipe;
        }
    }
}
