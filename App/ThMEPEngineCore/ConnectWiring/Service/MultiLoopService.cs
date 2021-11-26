using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm.DijkstraAlgorithm;
using ThMEPEngineCore.ConnectWiring.Model;

namespace ThMEPEngineCore.ConnectWiring.Service
{
    public class MultiLoopService
    {
        double dis = 500;
        public Dictionary<LoopInfoModel, List<Polyline>> CreateLoop(WiringLoopModel wiringLoop, List<Polyline> wirings, List<BlockReference> resBlocks)
        {
            Dictionary<LoopInfoModel, List<Polyline>> loops = new Dictionary<LoopInfoModel, List<Polyline>>();
            if (wiringLoop.loopInfoModels.Count > 1)
            {
                loops.Add(wiringLoop.loopInfoModels[0], wirings);
                for (int i = 1; i < wiringLoop.loopInfoModels.Count; i++)
                {
                    var resWirings = new List<Polyline>();
                    LoopInfoModel loopInfo = wiringLoop.loopInfoModels[i];
                    var loopBlocks = resBlocks.Where(x => loopInfo.blocks.Select(y => y.blockName).Any(y => x.Name == y)).ToList();
                    var otherBlocks = resBlocks.Except(loopBlocks).ToList();
                    loopBlocks.AddRange(otherBlocks.Where(x => loopBlocks.Any(y => y.Position.DistanceTo(x.Position) < dis)));
                    if (loopBlocks.Count > 1)
                    {
                        while (loopBlocks.Count > 0)
                        {
                            var startBlock = loopBlocks.First();
                            DijkstraAlgorithm dijkstraAlgorithm = new DijkstraAlgorithm(wirings.Cast<Curve>().ToList());
                            var allPath = dijkstraAlgorithm.FindingAllMinPath(startBlock.Position);
                            if (allPath.Count == 0)
                            {
                                loopBlocks.Remove(startBlock);
                                continue;
                            }
                            var loopWirings = FindLoopWiring(loopBlocks, allPath, wirings);
                            resWirings.AddRange(loopWirings);
                            loopBlocks.Remove(startBlock);
                        }
                        resWirings = resWirings.Distinct().ToList();
                    }
                    loops.Add(loopInfo, resWirings);
                }

                loops = CleanLoopWirings(loops);
            }

            return loops;
        }

        /// <summary>
        /// 清除多余走线
        /// </summary>
        /// <param name="loops"></param>
        /// <returns></returns>
        private Dictionary<LoopInfoModel, List<Polyline>> CleanLoopWirings(Dictionary<LoopInfoModel, List<Polyline>> loops)
        {
            for (int i = loops.Count - 2; i >= 0; i--)
            {
                var firKey = loops.Keys.ToList()[i + 1];
                var secKey = loops.Keys.ToList()[i];
                loops[secKey] = loops[secKey].Except(loops[firKey]).ToList();
            }

            return loops;
        }

        /// <summary>
        /// 找到对应回路需要的走线
        /// </summary>
        /// <param name="loopBlocks"></param>
        /// <param name="allPath"></param>
        /// <param name="wirings"></param>
        /// <returns></returns>
        private List<Polyline> FindLoopWiring(List<BlockReference> loopBlocks, Dictionary<Point3d, List<Point3d>> allPath, List<Polyline> wirings)
        {
            var resWirings = new List<Polyline>();
            foreach (var block in loopBlocks)
            {
                if (!allPath.Keys.Contains(block.Position))
                {
                    continue;
                }
                var path = allPath[block.Position];
                if (path != null)
                {
                    var loopWiring = wirings.Where(x => path.Any(y => y.IsEqualTo(x.StartPoint, new Tolerance(1, 1))) 
                        && path.Any(y => y.IsEqualTo(x.EndPoint, new Tolerance(1, 1)))).ToList();
                    resWirings.AddRange(loopWiring);
                }
            }

            return resWirings;
        }
    }
}
