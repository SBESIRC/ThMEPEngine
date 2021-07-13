using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;

namespace ThMEPWSS.HydrantConnectPipe.Model
{
    public class ThHydrant : ThIfcElement
    {
        public Polyline FireHydrantObb { get; set; }
        public ThHydrantPipe FireHydrantPipe { set; get; }
        public ThHydrant()
        {
            FireHydrantObb = new Polyline();
            FireHydrantPipe = new ThHydrantPipe();
        }
        public static ThHydrant Create(ThRawIfcDistributionElementData data)
        {
            var fireHydrant = new ThHydrant
            {
                Uuid = Guid.NewGuid().ToString(),
                Outline = data.Geometry,
                FireHydrantObb = data.Data as Polyline
            };
            return fireHydrant; 
        }

        public bool IsContainsPipe(ThHydrantPipe pipe,double buff)
        {

            var objcets = FireHydrantObb.Buffer(buff);
            var ployLine = objcets[0] as Polyline;
            if(!ployLine.IsNull())
            {
                if(ployLine.Contains(pipe.PipePosition))
                {
                    return true;
                }
            }
            return false;
        }
        public double GetRotationAngle()
        {
            var blk = Outline as BlockReference;
            return blk.Rotation;
        }
    }
}
