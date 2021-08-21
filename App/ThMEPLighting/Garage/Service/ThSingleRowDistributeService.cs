using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service
{
    public class ThSingleRowDistributeService
    {
        private ThLightGraphService LightGraph { get; set; }
        private ThLightArrangeParameter ArrangeParameter { get; set; }
        private ThQueryLightBlockService QueryLightBlockService { get; set; }
        private ThCADCoreNTSSpatialIndex LineEdgeSpatialIndex { get; set; }
        private List<ThLightEdge> Edges { get; set; }
        private List<ThLightEdge> DistributedEdges { get; set; }        

        private ThSingleRowDistributeService(
            ThLightGraphService lightGraph,
            ThLightArrangeParameter arrangeParameter,
            ThQueryLightBlockService queryLightBlockService)
        {
            LightGraph = lightGraph;
            ArrangeParameter = arrangeParameter;
            QueryLightBlockService = queryLightBlockService;
            Edges = new List<ThLightEdge>();
            LightGraph.Links.ForEach(o =>
            {
                o.Path.ForEach(p => Edges.Add(p));
            });
            DistributedEdges = new List<ThLightEdge>();
            LineEdgeSpatialIndex = new ThCADCoreNTSSpatialIndex(Edges.Select(e=>e.Edge).ToCollection());
        }
        public static void Distribute(
            ThLightGraphService lightGraph,
            ThLightArrangeParameter arrangeParameter,
            ThQueryLightBlockService queryLightBlockService)
        {
            var instance = new ThSingleRowDistributeService(lightGraph, arrangeParameter, queryLightBlockService);
            instance.Distribute();
        }
        private void Distribute()
        {
            if(LightGraph!=null && ArrangeParameter!=null)
            {
                LightGraph.Links.ForEach(o => Distribute(o));
            }            
        }
        private void Distribute(ThLinkPath singleLinkPath)
        {
            Point3d start = singleLinkPath.Start;            
            for (int i=0;i< singleLinkPath.Path.Count; i++)
            {
                var edges = new List<ThLightEdge> { singleLinkPath.Path[i] };
                int j = i + 1;
                for(;j< singleLinkPath.Path.Count;j++)
                {
                    var preEdge = edges.Last();
                    var nextEdge = singleLinkPath.Path[j];
                    if (ThGeometryTool.IsCollinearEx(
                        nextEdge.Edge.StartPoint,
                        nextEdge.Edge.EndPoint,
                        preEdge.Edge.StartPoint, preEdge.Edge.EndPoint))
                    {
                        edges.Add(nextEdge);
                    }
                    else
                    {
                        break;  //拐弯
                    }
                }
                i = j - 1;
                //建造路线上的灯(计算或从图纸获取)
                var maxPts = edges.GetMaxPts(); //获取直段最大的范围点
                var distributeInstance = new ThAdjustSingleRowDistributePosService(
                    maxPts, ArrangeParameter, Edges, DistributedEdges);
                var splitPts = distributeInstance.Distribute(); //可以布置的区域
                splitPts = RepairDir(splitPts, start); //修复方向
                start = start.DistanceTo(maxPts.Item2) > start.DistanceTo(maxPts.Item1) ? maxPts.Item2 : maxPts.Item1; //调整起点到末端
                var buildSingleRowService = new ThBuildSingleRowPosService(
                    edges, splitPts, ArrangeParameter, QueryLightBlockService);
                buildSingleRowService.Build();
                DistributedEdges.AddRange(edges);
            }
        }
        private List<Tuple<Point3d, Point3d>> RepairDir(List<Tuple<Point3d, Point3d>> splitPts, Point3d startPt)
        {
            var results = new List<Tuple<Point3d, Point3d>>();
            splitPts.ForEach(o =>
            {
                if (startPt.DistanceTo(o.Item1) < startPt.DistanceTo(o.Item2))
                {
                    results.Add(Tuple.Create(o.Item1, o.Item2));
                }
                else
                {
                    results.Add(Tuple.Create(o.Item2, o.Item1));
                }
            });
            return results;
        }
    }
}
