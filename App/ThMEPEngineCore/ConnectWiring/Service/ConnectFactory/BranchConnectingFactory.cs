using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.ConnectWiring.Model;

namespace ThMEPEngineCore.ConnectWiring.Service.ConnectFactory
{
    public class BranchConnectingFactory
    {
        double expandLength = 50;
        double mergeDis = 5;
        public Polyline BranchConnect(Polyline wiring, List<BlockReference> blocks, List<LoopBlockInfos> loopBlockInfos)
        {
            var sBlocks = blocks.Where(x => x.Position.DistanceTo(wiring.StartPoint) < mergeDis).ToList();
            var eBlocks = blocks.Where(x => x.Position.DistanceTo(wiring.EndPoint) < mergeDis).ToList();
            Polyline resPoly = wiring;
            if (sBlocks.Count > 0)
            {
                resPoly = CreateBranch(resPoly, sBlocks.First(), loopBlockInfos);
            }
            if (eBlocks.Count > 0)
            {
                if (resPoly.NumberOfVertices > 1)
                {
                    resPoly.ReverseCurve();
                    resPoly = CreateBranch(resPoly, eBlocks.First(), loopBlockInfos);
                }
            }
            return resPoly;
        }

        private Polyline CreateBranch(Polyline wiring, BlockReference block, List<LoopBlockInfos> loopBlockInfos)
        {
            double range = GetBlockRange(block) + expandLength;
            var blockInfo = loopBlockInfos.Where(x => x.blockName == block.Name).FirstOrDefault();
            ConnectBaseService connectService = null;
            if (blockInfo != null)
            {
                switch (blockInfo.blcokShape)
                {
                    case BlockShape.Rectangle:
                        connectService = new PointConnectService();
                        break;
                    case BlockShape.Capsule:
                        connectService = new PointConnectService();
                        break;
                    case BlockShape.Square:
                        connectService = new PointConnectService();
                        break;
                    case BlockShape.Trapezoid:
                        connectService = new PointConnectService();
                        break;
                    case BlockShape.Circle:
                        connectService = new CircleConnectService();
                        break;
                    default:
                        break;
                }
            }

            if (connectService != null)
            {
                return connectService.Connect(wiring, block, blockInfo, range);
            }
            return wiring;
        }

        /// <summary>
        /// 获得块的大致长度
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        private double GetBlockRange(BlockReference block)
        {
            var bounds = block.Bounds;
            if (bounds.HasValue)
            {
                return bounds.Value.MaxPoint.DistanceTo(bounds.Value.MinPoint);
            }
            else
            {
                return 1000;
            }
        }
    }
}
