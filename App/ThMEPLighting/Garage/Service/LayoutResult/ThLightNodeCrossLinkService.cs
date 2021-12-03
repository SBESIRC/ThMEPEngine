using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    /// <summary>
    /// 用于连接十字路口的灯
    /// </summary>
    internal sealed class ThLightNodeCrossLinkService: ThCrossLinkCalculator
    {
        /// <summary>
        /// 图的边
        /// 来源于双排布置
        /// </summary>
        private List<ThLightEdge> Edges { get; set; }
        private ThQueryLineService EdgeQuery { get; set; }
        
        public ThLightNodeCrossLinkService(List<ThLightEdge> edges, 
            Dictionary<Line, Tuple<List<Line>, List<Line>>> centerSideDicts):base(centerSideDicts)
        {
            Edges = edges;
            // 这里面的线和Edges不是1对1的关系，
            // Value中存的线和Edges中的线有几何的对应关系
            CenterSideDicts = centerSideDicts;
            EdgeQuery = ThQueryLineService.Create(Edges.Select(o => o.Edge).ToList());
        }
        public List<ThLightNodeLink> Link()
        {
            var results = new List<ThLightNodeLink>();
            var crosses = GetCrosses();
            crosses.ForEach(c =>
            {
                var res = Sort(c);
                // 分区
                var partitions = CreatePartition(res);

                // 只有分区为偶数
                if(partitions.Count%2==0)
                {
                    // 获取中心线附带的边线
                    var sides = GetCenterSides(c);

                    // 通过sides找到Edges中的边
                    var edgeLines = sides.SelectMany(o => GetEdges(o)).ToList();

                    // 创建对角区域的灯Link
                    var edges = Edges.Where(o => edgeLines.Contains(o.Edge)).ToList();
                    var half = partitions.Count / 2;
                    for (int i=0;i< half; i++)
                    {
                        var current = partitions[i];
                        var currentArea = CreateParallelogram(current.Item1, current.Item2);
                        var currentEdges = GroupEdges(currentArea, edges); // 分组
                        var currentNodes = GetPartitionCloseNodes(current, currentEdges);

                        var opposite = partitions[i+ half];                        
                        var oppositeArea = CreateParallelogram(opposite.Item1, opposite.Item2);
                        var oppositeEdges = GroupEdges(oppositeArea, edges);
                        var oppositeNodes = GetPartitionCloseNodes(opposite, oppositeEdges);
                        results.AddRange(Link(currentNodes, oppositeNodes));
                    }
                }
            });
            return results;
        }

        private List<ThLightNodeLink> Link(List<ThLightNode> firstNodes, List<ThLightNode> secondNodes)
        {
            var results = new List<ThLightNodeLink>();
            firstNodes.ForEach(f =>
            {
                secondNodes.ForEach(s =>
                {
                    if (f.Number == s.Number)
                    {
                        var nodeLink = new ThLightNodeLink()
                        {
                            First = f,
                            Second = s,
                            IsCrossLink = true,
                        };
                        // 这儿的Edges是连不起来的,用于创建跳线使用
                        nodeLink.Edges.Add(FindLightNodeEdge(f.Id));
                        nodeLink.Edges.Add(FindLightNodeEdge(s.Id));
                        results.Add(nodeLink);
                    }
                });
            });
            return Filter(results); //只保留一个
        }

        private List<ThLightNodeLink> Filter(List<ThLightNodeLink> nodeLinks)
        {
            var results = new List<ThLightNodeLink>();
            var numbers = nodeLinks.Select(o => o.First.Number).Distinct().ToList();
            numbers.ForEach(o =>
            {
                var sorts = nodeLinks
                 .Where(l => l.First.Number == o)
                 .OrderBy(l => l.First.Position.DistanceTo(l.Second.Position))
                 .ToList();
                if (sorts.Count > 0)
                {
                    results.Add(sorts.First());
                }
            });
            return results;
        }

        private Line FindLightNodeEdge(string lightNodeId)
        {
           return Edges
                .Where(o => o.LightNodes.Select(n => n.Id).Contains(lightNodeId))
                .FirstOrDefault().Edge;
        }

        private List<ThLightNode> GetPartitionCloseNodes(
            Tuple<Line,Line> partition,List<ThLightEdge> edges)
        {
            var results = new List<ThLightNode>();
            var item1Node = GetClosestNode(partition.Item1, partition.Item2, edges);
            var item2Node = GetClosestNode(partition.Item2, partition.Item1, edges);
            if(item1Node!=null)
            {
                results.Add(item1Node);
            }
            if (item2Node != null)
            {
                results.Add(item2Node);
            }
            return results;
        }

        private ThLightNode GetClosestNode(Line adjacentA, Line adjacentB, List<ThLightEdge> edges)
        {
            var inters = adjacentA.IntersectWithEx(adjacentB);
            if (inters.Count == 0)
            {
                return null;
            }
            var projectionAxis = GetCenterProjectionAxis(adjacentA.StartPoint, adjacentA.EndPoint, inters[0]);
            var parallels = GetParallels(adjacentA, edges.Select(o => o.Edge).ToList());
            var parallelEdges = edges.Where(o => parallels.Contains(o.Edge)).ToList();
            var lightNodes = parallelEdges.SelectMany(o => o.LightNodes).ToList();
            lightNodes = lightNodes
                .OrderByDescending(o=> o.Position.GetProjectPtOnLine(
                    projectionAxis.Item1, projectionAxis.Item2)
                .DistanceTo(projectionAxis.Item1)).ToList();
            return lightNodes.Count > 0 ? lightNodes.First() : null;
        }

        private Tuple<Point3d,Point3d> GetCenterProjectionAxis(Point3d lineSp,Point3d lineEp, Point3d cornerPt)
        {
            var farwayPt = cornerPt.GetNextLinkPt(lineSp, lineEp);
            return Tuple.Create(farwayPt, cornerPt);
        }

        private List<Line> GetParallels(Line line,List<Line> lines)
        {
            return lines.Where(o => line.IsParallelToEx(o)).ToList();
        }

        private List<Line> GetEdges(Line line,double width=1.0)
        {
            var lines = EdgeQuery.QueryCollinearLines(line.StartPoint, line.EndPoint, width);
            return lines.Where(o => line.HasCommon(o)).ToList();
        }
        private List<ThLightEdge> GroupEdges(Polyline partition, List<ThLightEdge> edges)
        {
            var results = new List<ThLightEdge>();
            edges.ForEach(e =>
            {
                if (e.LightNodes.Select(n => n.Position).Where(n => partition.IsContains(n)).Any())
                {
                    results.Add(e);
                }
            });
            return results;
        }
    }
}
