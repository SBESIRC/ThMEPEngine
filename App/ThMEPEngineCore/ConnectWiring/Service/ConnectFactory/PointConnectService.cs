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
            var xDir = block.BlockTransform.CoordinateSystem3d.Xaxis.OrthoProjectTo(Vector3d.ZAxis).GetNormal();
            var yDir = block.BlockTransform.CoordinateSystem3d.Yaxis.OrthoProjectTo(Vector3d.ZAxis).GetNormal();
            double xScale = block.ScaleFactors.X / 100;
            double yScale = block.ScaleFactors.Y / 100;
            List<Point3d> connectPts1 = new List<Point3d>();
            List<Point3d> connectPts2 = new List<Point3d>();
            var blockPt = new Point3d(block.Position.X, block.Position.Y, 0);
            //块的position需要是块的中心点
            connectPts1.Add(blockPt + xDir * Infos.XRight * xScale);
            connectPts1.Add(blockPt - xDir * Infos.XLeft * xScale);
            connectPts2.Add(blockPt + yDir * Infos.YRight * yScale);
            connectPts2.Add(blockPt - yDir * Infos.YLeft * yScale);
            ConnectMethodService methodService = new ConnectMethodService();
            //var connectWiring = methodService.CennectToPoint(wiring, block, connectPts);
            var connectWiring = methodService.CennectToPoint(wiring, block, xDir, connectPts1, yDir, connectPts2);
            return connectWiring;
        }
    }
}
