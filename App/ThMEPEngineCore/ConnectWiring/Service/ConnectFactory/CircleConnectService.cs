using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.ConnectWiring.Model;

namespace ThMEPEngineCore.ConnectWiring.Service.ConnectFactory
{
    public class CircleConnectService : ConnectBaseService
    {
        public override Polyline Connect(Polyline wiring, BlockReference block, LoopBlockInfos Infos, double range)
        {
            ConnectMethodService methodService = new ConnectMethodService();
            var connectWiring = methodService.ConnectByCircle(wiring, block, range);
            return connectWiring;
        }
    }
}
