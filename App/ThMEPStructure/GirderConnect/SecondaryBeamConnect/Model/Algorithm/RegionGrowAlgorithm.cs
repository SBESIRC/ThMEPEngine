using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.AFASRegion.Utls;

namespace ThMEPStructure.GirderConnect.SecondaryBeamConnect.Model.Algorithm
{
    /// <summary>
    /// 区域生长算法(RegionGrow)
    /// 原算法是为了处理图像识别，对图像的生长点和相似区域的相似性判断依据可以是灰度值、纹理、颜色等图像信息
    /// 此算法应用了区域生长的思想，为适应项目进行了演变
    /// </summary>

    /* <伪代码>
     * RegionGrow
     * input : A level-line field LLA, a seed pixel P, an angle tolerance τ , and a Status variable for each pixel.
     * output: A set of pixels: region
     * 1 Add P → region
     * 2 θregion ← LLA(P)
     * 3 Sx ← cos(θregion)
     * 4 Sy ← sin(θregion)
     * 5 foreach pixel P ∈ region do
     * 6    foreach pixel Q ∈ Neighborhood(P) and Status(Q) 6= USED do
     * 7        if AngleDiff θregion, LLA(Q)
     * 8            Add Q → regio
     * 9            Status(Q) ← USED.
     * 10           Sx ← Sx + cos(LLA(Q))
     * 11           Sy ← Sy + sin(LLA(Q))
     * 12           θregion ← arctan(Sy/Sx).
     * 13       end
     * 14   end
     * 15 end
     */

    public class RegionGrowAlgorithm
    {
        public List<ThBeamTopologyNode> Nodes { get; set; }
        public List<List<ThBeamTopologyNode>> space { get; set; }

        public void RegionGrow(List<ThBeamTopologyNode> nodes)
        {
            Nodes = nodes;
            space = new List<List<ThBeamTopologyNode>>();
            CreatRandomSeed();
            //RegionalIntegration();
            RegionalOptimization();
        }

        /// <summary>
        /// 生成随机数种子并生长成区域
        /// </summary>
        private void CreatRandomSeed()
        {
            var seed = Nodes.FirstOrDefault(o => o.LayoutLines.edges.Count>0 && o.HaveLayoutBackUp);
            while (!seed.IsNull())
            {
                var region = RegionGrowSmall(seed);
                space.Add(region);
                seed = Nodes.FirstOrDefault(o => o.LayoutLines.edges.Count>0 && o.HaveLayoutBackUp);
            }
        }

        /// <summary>
        /// 对剩余离散种子进行"融合"加入到周边区域
        /// </summary>
        private void RegionalIntegration()
        {
            bool Signal = true;
            while (Signal)
            {
                var newNodes = new List<ThBeamTopologyNode>();
                foreach (var node in Nodes)
                {
                    var neighbors = node.Neighbor.Select(o => o.Item2).Except(Nodes).ToList();
                    if(neighbors.Count == 0)
                    {
                        newNodes.Add(node);
                    }
                    else if(neighbors.Count == 1)
                    {
                        var list = space.First(o => o.Contains(neighbors[0]));
                        list.Add(node);
                    }
                    else
                    {
                        
                    }
                }
                Signal = Nodes.Count > newNodes.Count;
                Nodes = newNodes;
            }
        }

        /// <summary>
        /// 区域脱落，抖掉区域凸点
        /// </summary>
        private void RegionalOptimization()
        {
            for (int i = 0; i < space.Count; i++)
            {
                var list = space[i];
                bool Signal = true;
                while (Signal)
                {
                    List<ThBeamTopologyNode> removeNodes = new List<ThBeamTopologyNode>();
                    foreach (var node in list)
                    {
                        if (node.Neighbor.Count(o => list.Contains(o.Item2))<2)
                        {
                            removeNodes.Add(node);
                        }
                        else if (node.Neighbor.Count(o => list.Contains(o.Item2))==2 && node.Neighbor.Any(x => list.Contains(x.Item2) && x.Item2.Neighbor.Count(o => list.Contains(o.Item2))==2))
                        {
                            removeNodes.Add(node);
                        }
                    }
                    if (removeNodes.Count>0)
                    {
                        list = list.Except(removeNodes).ToList();
                        Nodes.AddRange(removeNodes);
                    }
                    else
                    {
                        Signal = false;
                    }
                }
                space[i] = list;
            }
            space.RemoveAll(o => o.Count == 0);
        }

        private List<ThBeamTopologyNode> RegionGrowSmall(ThBeamTopologyNode tempCall)
        {
            List<ThBeamTopologyNode> result = new List<ThBeamTopologyNode>();

            //堆栈
            Stack<ThBeamTopologyNode> Seed = new Stack<ThBeamTopologyNode>();

            //标记领域的循环变量
            //int k = 0;

            //----求重心
            //int seedSum = 0;

            //标记种子
            Nodes.Remove(tempCall);

            //种子进栈
            Seed.Push(tempCall);
            result.Add(tempCall);

            while (Seed.Count > 0)
            {
                var currentPixel = Seed.Pop();
                foreach (var NeighborCurrentPixel in currentPixel.Neighbor.Select(o => o.Item2))
                {
                    //判断点是否在图像区域内
                    if (!Nodes.Contains(NeighborCurrentPixel))
                    {
                        continue;
                    }
                    if (NeighborCurrentPixel.LayoutLines.edges.Count == 0)
                    {
                        continue;
                    }
                    if (CheckCurrentPixel(currentPixel, NeighborCurrentPixel))
                    {
                        Seed.Push(NeighborCurrentPixel);
                        result.Add(NeighborCurrentPixel);
                        Nodes.Remove(NeighborCurrentPixel);
                    }
                }
            }
            return result;
        }

        private bool CheckCurrentPixel(ThBeamTopologyNode currentPixel, ThBeamTopologyNode neighborCurrentPixel)
        {
            return currentPixel.LayoutLines.vector.IsParallelWithTolerance(neighborCurrentPixel.LayoutLines.vector, 35);
        }
    }

    
}
