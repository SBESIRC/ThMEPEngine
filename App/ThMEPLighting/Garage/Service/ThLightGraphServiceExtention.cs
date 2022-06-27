using System.Linq;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public static class ThLightGraphServiceExtention
    {
        public static List<List<Line>> GetLinks(this ThLightGraphService graph)
        {
            var results = new List<List<Line>>();
            graph.Links.ForEach(o =>
            {
                results.AddRange(o.GetLinks());
            });
            return results;
        }
        public static List<List<Line>> GetLinks(this ThLinkPath linkPath)
        {
            var results = new List<List<Line>>();
            var startPt = linkPath.Start;
            for (int i = 0; i < linkPath.Edges.Count; i++)
            {
                var currentEdge = linkPath.Edges[i];
                var edges = new List<ThLightEdge> { currentEdge };
                int j = i + 1;
                for (; j < linkPath.Edges.Count; j++)
                {
                    var preEdge = edges.Last();
                    var nextEdge = linkPath.Edges[j];
                    if (ThGarageUtils.IsLessThan45Degree(
                        preEdge.Edge.StartPoint, preEdge.Edge.EndPoint, nextEdge.Edge.StartPoint, nextEdge.Edge.EndPoint))
                    {
                        edges.Add(nextEdge);
                    }
                    else
                    {
                        break;  //拐弯
                    }
                }
                i = j - 1;
                //分析在线路上无需布灯的区域，返回可以布点的区域
                var lines = edges.Select(o => o.Edge).ToList();
                lines.RepairLineDir(startPt);
                startPt = lines[lines.Count - 1].EndPoint;//调整起点到末端
                results.Add(lines);
            }
            return results;
        }
        public static int CalculateLightNumber(this ThLightGraphService graph)
        {
            int numOfLights = 0;
            graph.Links.ForEach(l => l.Edges.ForEach(p => numOfLights += p.LightNodes.Count));
            return numOfLights;
        }
        public static int CalculateLightNumber(this List<ThLightEdge> edges)
        {
            return edges.Sum(e => e.LightNodes.Count);
        }
        private static bool IsExisted(List<Line> lines,Point3d port,double tolerance)
        {
            return lines
                .Where(o => o.StartPoint.DistanceTo(port) <= tolerance || o.EndPoint.DistanceTo(port) <= tolerance)
                .Any();
        }
        private static Point3d GetGraphStartPt(List<Line> lines)
        {
            var mergedLines = lines.CleanNoding();
            var edges = mergedLines.Select(o => new ThLightEdge(o)).ToList();
            return LaneServer.getMergedOrderedLane(edges);
        }
        public static List<ThLightGraphService> CreateGraphs(this List<ThLightEdge> lightEdges)
        {
            // 传入的Edges是
            var results = new List<ThLightGraphService>();
            while (lightEdges.Count > 0)
            {
                if (lightEdges.Where(o => o.IsDX).Count() == 0)
                {
                    break;
                }
                Point3d findSp = lightEdges.Where(o => o.IsDX).First().Edge.StartPoint;
                var priorityStart = lightEdges.Select(o => o.Edge).ToList().FindPriorityStart(ThGarageLightCommon.RepeatedPointDistance);
                if (priorityStart != null)
                {
                    findSp = priorityStart.Item2;
                }
                //对灯线边建图,创建从findSp开始可以连通的图
                var lightGraph = new ThCdzmLightGraphService(lightEdges, findSp);
                lightGraph.Build();
                var traversedLightEdges = lightGraph.GraphEdges;

                //找到从ports中的点出发拥有最长边的图
                var prioritySp = GetGraphStartPt(traversedLightEdges.Select(o => o.Edge).ToList());
                if (!IsExisted(traversedLightEdges.Select(o => o.Edge).ToList(), prioritySp, 1.0))
                {
                    prioritySp = findSp;
                }

                // 使用珣若算的最优起点重新建图
                traversedLightEdges.ForEach(e => e.IsTraversed = false);
                var newLightGraph = new ThCdzmLightGraphService(traversedLightEdges, prioritySp);
                newLightGraph.Build();
                //newLightGraph.Print();

                lightEdges = lightEdges.Where(o => o.IsTraversed == false).ToList();
                results.Add(newLightGraph);
            }
            return results;
        }

        public static List<ThLightGraphService> CreateCdzmGraphs(this List<ThLightEdge> lightEdges)
        {
            // 传入的Edges是
            var results = new List<ThLightGraphService>();
            while (lightEdges.Count > 0)
            {
                if (lightEdges.Where(o => o.IsDX).Count() == 0)
                {
                    break;
                }
                Point3d findSp = lightEdges.Where(o => o.IsDX).First().Edge.StartPoint;
                var priorityStart = lightEdges.Select(o => o.Edge).ToList().FindPriorityStart(ThGarageLightCommon.RepeatedPointDistance);
                if (priorityStart != null)
                {
                    findSp = priorityStart.Item2;
                }
                //对灯线边建图,创建从findSp开始可以连通的图
                var lightGraph = new ThCdzmLightGraphService(lightEdges, findSp);
                lightGraph.Build();

                lightEdges = lightEdges.Where(o => o.IsTraversed == false).ToList();
                results.Add(lightGraph);
            }
            return results;
        }
    }
}
