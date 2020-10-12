using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Model;

namespace ThMEPWSS.Bussiness.ConnectingPipeBussiness
{
    public class ConnectingPipeService
    {
        public void ConnectingPipe(List<SprayLayoutData> sprays, Line pipeLine, List<Polyline> parkingLines)
        {
            Vector3d xDir = Vector3d.XAxis;
            Vector3d yDir = Vector3d.YAxis;
        }

        public void CreateBranchPipeLine(List<SprayLayoutData> sprays)
        {

        }
    }
}
