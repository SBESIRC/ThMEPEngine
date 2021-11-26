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
    public class RectangleConnectService : ConnectBaseService
    {
        public override Polyline Connect(Polyline wiring, BlockReference block, LoopBlockInfos Infos, double range)
        {
            var xDir = block.BlockTransform.CoordinateSystem3d.Xaxis.GetNormal();
            var yDir = block.BlockTransform.CoordinateSystem3d.Yaxis.GetNormal();
            List<Point3d> connectPts = new List<Point3d>();
            //块的position需要是块的中心点
            connectPts.Add(block.Position + xDir * Infos.XRight);
            connectPts.Add(block.Position - xDir * Infos.XLeft);
            connectPts.Add(block.Position + yDir * Infos.YRight);
            connectPts.Add(block.Position - yDir * Infos.YLeft);

            ConnectMethodService methodService = new ConnectMethodService();
            var connectWiring = methodService.CennectToPoint(wiring, block, range, connectPts);
            return connectWiring;
        }
    }
}
