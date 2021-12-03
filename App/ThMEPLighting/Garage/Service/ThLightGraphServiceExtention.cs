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
        public static List<ThLightGraphService> CreateGraphs(this List<Line> lines)
        {
            var lightEdges = lines.Select(l => new ThLightEdge(l)).ToList();
            var results = new List<ThLightGraphService>();
            while (lightEdges.Count > 0)
            {
                if (lightEdges.Where(o => o.IsDX).Count() == 0)
                {
                    break;
                }
                Point3d findSp = lightEdges.Where(o => o.IsDX).First().Edge.StartPoint;

                //对灯线边建图,创建从findSp开始可以连通的图
                var lightGraph = new ThCdzmLightGraphService(lightEdges, findSp);
                lightGraph.Build();

                //找到从ports中的点出发拥有最长边的图
                var centerEdges = new List<ThLightEdge>();
                lightGraph.Links.ForEach(o => o.Edges.ForEach(p => centerEdges.Add(new ThLightEdge(p.Edge))));
                var centerStart = LaneServer.getMergedOrderedLane(centerEdges);
                centerEdges.ForEach(o => o.IsTraversed = false);

                // 使用珣若算的最优起点重新建图
                var newLightGraph = new ThCdzmLightGraphService(centerEdges, centerStart);
                newLightGraph.Build();
                //newLightGraph.Print();

                lightEdges = lightEdges.Where(o => o.IsTraversed == false).ToList();
                results.Add(newLightGraph);
            }
            return results;
        }
    }
}
