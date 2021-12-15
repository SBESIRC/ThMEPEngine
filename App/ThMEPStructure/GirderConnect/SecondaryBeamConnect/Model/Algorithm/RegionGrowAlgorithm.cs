using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.AFASRegion.Utls;
using ThMEPEngineCore.CAD;
using ThMEPStructure.GirderConnect.SecondaryBeamConnect.Service;

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
        public List<List<ThBeamTopologyNode>> Aggregatespace { get; set; }
        public List<List<ThBeamTopologyNode>> Adjustmentspace { get; set; }

        public void RegionGrow(List<ThBeamTopologyNode> nodes)
        {
            Nodes = nodes.Where(o => o.Edges.Count != 3 || o.Edges[0].BeamType != BeamType.Scrap).ToList();
            Aggregatespace = new List<List<ThBeamTopologyNode>>();
            Adjustmentspace =new List<List<ThBeamTopologyNode>>();
            CreatRandomSeed();
            //RegionalIntegration();
            RegionalOptimization();
            EliminateDents();
            MergeUnchangeableArea();
            EliminateDents();
            MergeAdjacentRegion();
            DeleteSmallnSpace();
            EliminateDents();
            MergeChangeArea();
            MergeNotAdjacentRegion();
            EliminateNotAdjacentDents();
        }

        /// <summary>
        /// 生成随机数种子并生长成区域
        /// </summary>
        private void CreatRandomSeed()
        {
            var seed = Nodes.FirstOrDefault(o => o.LayoutLines.edges.Count>0 && o.HaveLayoutBackUp);
            while (!seed.IsNull())
            {
                var region = RegionGrowSmall(this.Nodes, seed);
                Aggregatespace.Add(region);
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
                    if (neighbors.Count == 0)
                    {
                        newNodes.Add(node);
                    }
                    else if (neighbors.Count == 1)
                    {
                        var list = Aggregatespace.First(o => o.Contains(neighbors[0]));
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
            var oldAggregatespaceCount = Aggregatespace.Count;
            for (int i = 0; i < oldAggregatespaceCount; i++)
            {
                var list = Aggregatespace[i];
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

                //Aggregatespace[i] = list;

                var newSpace = new List<List<ThBeamTopologyNode>>();
                var seed = list.FirstOrDefault();
                while (!seed.IsNull())
                {
                    var region = RegionGrowSmall(list, seed);
                    newSpace.Add(region);
                    seed = list.FirstOrDefault();
                }
                if (newSpace.Count>0)
                {
                    Aggregatespace[i] = newSpace[0];
                    Aggregatespace.AddRange(newSpace.Skip(1));
                }
                else
                {
                    Aggregatespace[i] = new List<ThBeamTopologyNode>();
                }
            }
            Aggregatespace.RemoveAll(o => o.Count == 0);
        }

        /// <summary>
        /// 合并不可更改区域
        /// </summary>
        private void MergeUnchangeableArea()
        {
            var UnchangeableNodes = Nodes.Where(o => !o.HaveLayoutBackUp && o.LayoutLines.SecondaryBeamLines.Count > 0).ToList();
            Nodes = Nodes.Except(UnchangeableNodes).ToList();
            List<List<ThBeamTopologyNode>> UnchangeableSpaces = new List<List<ThBeamTopologyNode>>();
            var seed = UnchangeableNodes.FirstOrDefault();
            while (!seed.IsNull())
            {
                var region = RegionGrowSmall(UnchangeableNodes, seed);
                UnchangeableSpaces.Add(region);
                seed = UnchangeableNodes.FirstOrDefault();
            }
            foreach (var space in UnchangeableSpaces)
            {
                //var Adjacentspace = Aggregatespace.Where(o => space.Any(x =>o.First().CheckCurrentPixel(space.First()) && x.Neighbor.Any(neighbor => o.Contains(neighbor.Item2)))).ToList();
                var Adjacentspace = Aggregatespace.Where(o => o.First().CheckCurrentPixel(space.First())).Where(o => space.Any(x => x.Neighbor.Any(neighbor => o.Contains(neighbor.Item2)))).ToList();
                var list = space.ToArray().ToList();
                foreach (var adjacent in Adjacentspace)
                {
                    Aggregatespace.Remove(adjacent);
                    list.AddRange(adjacent);
                }
                Aggregatespace.Add(list);
            }
        }

        /// <summary>
        /// 消除凹包
        /// </summary>
        private void EliminateDents()
        {
            var nodeDic = this.Nodes.ToDictionary(o => o.Boundary, o => o);
            ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(nodeDic.Keys.ToCollection());
            bool Signal = true;
            while (Signal)
            {
                Signal = false;
                for (int i = 0; i < Aggregatespace.Count; i++)
                {
                    var space = Aggregatespace[i];
                    var UnionPolygon = space.UnionPolygon();
                    var ConvexPolyline = UnionPolygon.ConvexHullPL();
                    var polyline = ConvexPolyline.Buffer(-1000)[0] as Polyline;
                    var objs = spatialIndex.SelectCrossingPolygon(polyline);
                    foreach (Polyline obj in objs)
                    {
                        var node = nodeDic[obj];
                        if (this.Nodes.Contains(node) && node.Neighbor.Any(o=> space.Contains(o.Item2)))
                        {
                            var NewUnionPolygon = node.UnionPolygon(UnionPolygon);
                            var NewConvexPolyline = NewUnionPolygon.ConvexHullPL();
                            if (NewUnionPolygon.Area / NewConvexPolyline.Area > UnionPolygon.Area / ConvexPolyline.Area)
                            {
                                if (!node.CheckCurrentPixel(space.First()))
                                    node.SwapLayout();
                                this.Nodes.Remove(node);
                                space.Add(node);
                                Signal = true;
                            }
                        }
                    }
                    Aggregatespace[i] = space;
                }
            }
        }
        
        /// <summary>
        /// 消除离散区域凹包
        /// </summary>
        private void EliminateNotAdjacentDents()
        {
            var nodeDic = Nodes.ToDictionary(o => o.Boundary, o => o);
            ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(nodeDic.Keys.ToCollection());
            bool Signal = true;
            while (Signal)
            {
                Signal = false;
                for (int i = 0; i < Aggregatespace.Count; i++)
                {
                    var space = Aggregatespace[i];
                    var UnionPolygon = space.UnionPolygon();
                    var ConvexPolyline = UnionPolygon.ConvexHullPL();
                    var polyline = ConvexPolyline.Buffer(-1000)[0] as Polyline;
                    var objs = spatialIndex.SelectCrossingPolygon(polyline);
                    foreach (Polyline obj in objs)
                    {
                        var node = nodeDic[obj];
                        if (this.Nodes.Contains(node) && node.Neighbor.Any(o => space.Contains(o.Item2)))
                        {
                            var NewUnionPolygon = node.UnionPolygon(UnionPolygon);
                            var NewConvexPolyline = NewUnionPolygon.ConvexHullPL();
                            if (NewUnionPolygon.Area / NewConvexPolyline.Area > UnionPolygon.Area / ConvexPolyline.Area)
                            {
                                if (!node.CheckCurrentPixel(space.First()))
                                    node.SwapLayout();
                                this.Nodes.Remove(node);
                                space.Add(node);
                                Signal = true;
                            }
                        }
                    }
                    Aggregatespace[i] = space;
                }
            }
            for (int i = 0; i < Adjustmentspace.Count; i++)
            {
                while (Signal)
                {
                    Signal = false;
                    var space = Adjustmentspace[i];
                    var UnionPolygon = space.UnionPolygon();
                    var ConvexPolyline = UnionPolygon.ConvexHullPL();
                    var polyline = ConvexPolyline.Buffer(-10)[0] as Polyline;
                    var objs = spatialIndex.SelectCrossingPolygon(polyline);
                    foreach (Polyline obj in objs)
                    {
                        var node = nodeDic[obj];
                        if (Nodes.Contains(node) && node.Neighbor.Any(o => space.Contains(o.Item2)))
                        {
                            var NewUnionPolygon = node.UnionPolygon(UnionPolygon);
                            var NewConvexPolyline = NewUnionPolygon.ConvexHullPL();
                            if (NewUnionPolygon.Area / NewConvexPolyline.Area > UnionPolygon.Area / ConvexPolyline.Area)
                            {
                                Nodes.Remove(node);
                                space.Add(node);
                                Signal = true;
                            }
                        }
                    }
                    Adjustmentspace[i] = space;
                }
            }
        }

        /// <summary>
        /// 合并临近区域
        /// </summary>
        private void MergeAdjacentRegion()
        {
            var deleteNodes = new List<ThBeamTopologyNode>();
            for (int i = 0; i < Nodes.Count; i++)
            {
                var node = Nodes[i];
                var neighbor = node.Neighbor;
                //if (node.LayoutLines.edges.Count > 0)
                //{
                //    neighbor =node.Neighbor.Where(o => node.LayoutLines.edges.Select(x => x.BeamSide).Contains(o.Item1)).ToList();
                //}
                //var needNeighbor = neighbor.Select(o => o.Item2).Where(o =>o.LayoutLines.edges.Count>0 && !Nodes.Contains(o));
                var needNeighbor = neighbor.Select(o => o.Item2).Where(o => !Nodes.Contains(o)).ToList();
                var needNeighborspace = needNeighbor.Select(o => Aggregatespace.FirstOrDefault(x => x.Contains(o))).Where(o => !o.IsNull()).Distinct().ToList();
                List<ThBeamTopologyNode> needNeighborspace1, needNeighborspace2;
                if (needNeighborspace.Count == 2)
                {
                    needNeighborspace1 = needNeighborspace[0];
                    needNeighborspace2 = needNeighborspace[1];
                }
                else if (needNeighborspace.Count == 3)
                {
                    if (needNeighborspace[0].First().CheckCurrentPixel(needNeighborspace[1].First()))
                    {
                        needNeighborspace1 = needNeighborspace[0];
                        needNeighborspace2 = needNeighborspace[1];
                    }
                    else if (needNeighborspace[0].First().CheckCurrentPixel(needNeighborspace[2].First()))
                    {
                        needNeighborspace1 = needNeighborspace[0];
                        needNeighborspace2 = needNeighborspace[2];
                    }
                    else
                    {
                        needNeighborspace1 = needNeighborspace[1];
                        needNeighborspace2 = needNeighborspace[2];
                    }
                }
                else
                {
                    continue;
                }
                if (needNeighborspace1.First().CheckCurrentPixel(needNeighborspace2.First()) && (node.LayoutLines.edges.Count == 0 || needNeighborspace1.First().CheckCurrentPixel(node)))
                {
                    deleteNodes.Add(node);
                    Aggregatespace.Remove(needNeighborspace1);
                    Aggregatespace.Remove(needNeighborspace2);
                    var newSpace = needNeighborspace1.Union(needNeighborspace2).ToList();
                    newSpace.Add(node);
                    Aggregatespace.Add(newSpace);
                }
            }
            deleteNodes.ForEach(x => Nodes.Remove(x));
        }

        /// <summary>
        /// 合并临近区域
        /// </summary>
        private void MergeNotAdjacentRegion()
        {
            var deleteNodes = new List<ThBeamTopologyNode>();
            for (int i = 0; i < Nodes.Count; i++)
            {
                var node = Nodes[i];
                if(node.Edges[0].BeamType == BeamType.Scrap)
                {
                    continue;
                }
                var neighbor = node.Neighbor;
                var needNeighbor = neighbor.Select(o => o.Item2).Where(o =>!Nodes.Contains(o)).ToList();
                var needNeighborspace = needNeighbor.Select(o => Adjustmentspace.FirstOrDefault(x => x.Contains(o))).Where(o => !o.IsNull()).Distinct().ToList();
                if(needNeighborspace.Count > 1)
                {
                    deleteNodes.Add(node);
                    Adjustmentspace = Adjustmentspace.Except(needNeighborspace).ToList();
                    var newSpace = needNeighborspace.SelectMany(o => o).ToList();
                    newSpace.Add(node);
                    Adjustmentspace.Add(newSpace);
                }
            }
            deleteNodes.ForEach(x => Nodes.Remove(x));
        }

        private void DeleteSmallnSpace()
        {
            var smallSpace = Aggregatespace.Where(o => o.Count == 1);
            Nodes.AddRange(smallSpace.SelectMany(o => o));
            Aggregatespace = Aggregatespace.Except(smallSpace).ToList();
        }

        /// <summary>
        /// 合并离散点
        /// </summary>
        private void MergeChangeArea()
        {
            var LayoutNodes = Nodes.Where(o => o.LayoutLines.edges.Count > 0).ToList();
            Nodes = Nodes.Where(o => o.LayoutLines.edges.Count == 0).ToList();
            var seed = LayoutNodes.FirstOrDefault();
            while (!seed.IsNull())
            {
                var region = RegionGrowSmall(LayoutNodes, seed, true);
                Adjustmentspace.Add(region);
                seed = LayoutNodes.FirstOrDefault();
            }
        }

        private List<ThBeamTopologyNode> RegionGrowSmall(List<ThBeamTopologyNode> nodes, ThBeamTopologyNode tempCall, bool IgnoreDir = false)
        {
            List<ThBeamTopologyNode> result = new List<ThBeamTopologyNode>();

            //堆栈
            Stack<ThBeamTopologyNode> Seed = new Stack<ThBeamTopologyNode>();

            //标记领域的循环变量
            //int k = 0;

            //----求重心
            //int seedSum = 0;

            //标记种子
            nodes.Remove(tempCall);

            //种子进栈
            Seed.Push(tempCall);
            result.Add(tempCall);

            while (Seed.Count > 0)
            {
                var currentPixel = Seed.Pop();
                foreach (var NeighborCurrentPixel in currentPixel.Neighbor.Select(o => o.Item2))
                {
                    //判断点是否在图像区域内
                    if (!nodes.Contains(NeighborCurrentPixel))
                    {
                        continue;
                    }
                    if (NeighborCurrentPixel.LayoutLines.edges.Count == 0 && !IgnoreDir)
                    {
                        continue;
                    }
                    if (currentPixel.CheckCurrentPixel(NeighborCurrentPixel) || IgnoreDir)
                    {
                        Seed.Push(NeighborCurrentPixel);
                        result.Add(NeighborCurrentPixel);
                        nodes.Remove(NeighborCurrentPixel);
                    }
                }
            }
            return result;
        }
    }
}
