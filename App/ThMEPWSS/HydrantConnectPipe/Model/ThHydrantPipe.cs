using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.HydrantConnectPipe.Model
{
    public class ThHydrantPipe : ThIfcElement
    {
        public Point3d PipePosition { set; get; }
        public static ThHydrantPipe Create(Entity data)
        {
            var fireHydrantPipe = new ThHydrantPipe();
            fireHydrantPipe.Uuid = Guid.NewGuid().ToString();
            fireHydrantPipe.Outline = data;
            return fireHydrantPipe;
        }
    }
}
