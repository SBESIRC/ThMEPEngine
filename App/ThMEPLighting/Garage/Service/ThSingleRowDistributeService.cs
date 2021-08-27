using System;
using System.Linq;
using ThCADExtension;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThSingleRowDistributeService : ThDistributeMainService
    {      

        public ThSingleRowDistributeService(
            ThLightGraphService lightGraph,
            ThLightArrangeParameter arrangeParameter,
            ThQueryLightBlockService queryLightBlockService)
            :base(lightGraph, arrangeParameter, queryLightBlockService)
        {
        }
        public override void Distribute()
        {
            if(LightGraph!=null && ArrangeParameter!=null)
            {
                LightGraph.Links.ForEach(o => Distribute(o));
            }            
        }
        private void Distribute(ThLinkPath singleLinkPath)
        {
            Point3d startPt = singleLinkPath.Start;            
            for (int i=0;i< singleLinkPath.Path.Count; i++)
            {
                var edges = new List<ThLightEdge> { singleLinkPath.Path[i] };
                int j = i + 1;
                for(;j< singleLinkPath.Path.Count;j++)
                {
                    var preEdge = edges.Last();
                    var nextEdge = singleLinkPath.Path[j];
                    if (ThGarageUtils.IsLessThan45Degree(preEdge.Edge.StartPoint, preEdge.Edge.EndPoint,
                                                         nextEdge.Edge.StartPoint, nextEdge.Edge.EndPoint))
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
                var lines = edges.Select(o => o.Edge).ToList();
                RepairLineDir(lines, startPt);
                var segments = GetCanLayoutSegments(lines);//可以布置的区域
                startPt = lines[lines.Count - 1].EndPoint;//调整起点到末端
                var buildSingleRowService = new ThBuildSingleRowPosService(
                    edges, segments, ArrangeParameter, QueryLightBlockService);
                buildSingleRowService.Build();
                DistributedEdges.AddRange(edges);
            }
        }
        
        protected override List<List<Point3d>> GetCanLayoutSegments(List<Line> lines)
        {
            var linearrangment = new List<ThAdjustSingleRowDistributePosService>();
            for (int i = 0; i < lines.Count; i++)
            {
                linearrangment.Add(new ThAdjustSingleRowDistributePosService(
                    Tuple.Create(lines[i].StartPoint, lines[i].EndPoint),
                    ArrangeParameter, GraphEdges, DistributedEdges));
            }
            return MergeLayoutSegments(linearrangment.Cast<ThAdjustLightDistributePosService>().ToList());
        }
    }
}
