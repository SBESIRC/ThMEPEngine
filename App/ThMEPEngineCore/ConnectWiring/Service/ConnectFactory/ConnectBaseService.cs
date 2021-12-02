using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.ConnectWiring.Model;

namespace ThMEPEngineCore.ConnectWiring.Service.ConnectFactory
{
    public abstract class ConnectBaseService
    {
        public abstract Polyline Connect(Polyline wiring, BlockReference block, LoopBlockInfos Infos, double range);
    }
}
