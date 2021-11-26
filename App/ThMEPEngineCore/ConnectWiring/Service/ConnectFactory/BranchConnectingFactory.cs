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
        double expandLength = 300;
        public void BranchConnect(Polyline wiring, BlockReference block, List<LoopBlockInfos> loopBlockInfos, double scale)
        {
            double range = GetBlockRange(block) + expandLength;
            var blockInfo = loopBlockInfos.Where(x => x.blockName == block.Name).FirstOrDefault();
            ConnectBaseService connectService;
            if (blockInfo == null)
            {
                switch (blockInfo.blcokShape)
                {
                    case BlockShape.Rectangle:
                        connectService = new RectangleConnectService();
                        break;
                    case BlockShape.Capsule:
                        break;
                    case BlockShape.Square:
                        break;
                    case BlockShape.Trapezoid:
                        break;
                    case BlockShape.Circle:
                        break;
                    default:
                        break;
                }
            }
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
