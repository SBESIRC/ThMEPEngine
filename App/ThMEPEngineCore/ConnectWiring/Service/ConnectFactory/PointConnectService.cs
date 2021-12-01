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
    public class PointConnectService : ConnectBaseService
    {
        public override Polyline Connect(Polyline wiring, BlockReference block, LoopBlockInfos Infos, double range)
        {
            var xDir = block.BlockTransform.CoordinateSystem3d.Xaxis.GetNormal();
            var yDir = block.BlockTransform.CoordinateSystem3d.Yaxis.GetNormal();
            double xScale = block.ScaleFactors.X / 100;
            double yScale = block.ScaleFactors.Y / 100;
            List<Point3d> connectPts = new List<Point3d>();
            //块的position需要是块的中心点
            connectPts.Add(block.Position + xDir * Infos.XRight * xScale);
            connectPts.Add(block.Position - xDir * Infos.XLeft * xScale);
            connectPts.Add(block.Position + yDir * Infos.YRight * yScale);
            connectPts.Add(block.Position - yDir * Infos.YLeft * yScale);

            ConnectMethodService methodService = new ConnectMethodService();
            var connectWiring = methodService.CennectToPoint(wiring, block, range, connectPts);
            return connectWiring;
        }
    }
}
