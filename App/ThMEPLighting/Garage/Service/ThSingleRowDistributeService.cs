using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Worker;

namespace ThMEPLighting.Garage.Service
{
    public class ThSingleRowDistributeService
    {
        private ThLightGraphService LightGraph { get; set; }
        private ThLightArrangeParameter ArrangeParameter { get; set; }
        private ThSingleRowDistributeService(
            ThLightGraphService lightGraph,
            ThLightArrangeParameter arrangeParameter)
        {
            LightGraph = lightGraph;
            ArrangeParameter = arrangeParameter;
        }
        public static void Distribute(ThLightGraphService lightGraph,
            ThLightArrangeParameter arrangeParameter)
        {
            var instance = new ThSingleRowDistributeService(lightGraph, arrangeParameter);
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
                    var currentEdge = singleLinkPath.Path[j];
                    if (ThGeometryTool.IsCollinearEx(
                        currentEdge.Edge.StartPoint,
                        currentEdge.Edge.EndPoint,
                        preEdge.Edge.StartPoint, preEdge.Edge.EndPoint))
                    {
                        edges.Add(currentEdge);
                    }
                    else
                    {
                        break;  //拐弯
                    }
                }
                i = j - 1;
                //建造路线上的灯(计算或从图纸获取)
                var singleRowNumberInstance=ThBuildSingleRowPosService.Build(start,edges, ArrangeParameter);
                start = singleRowNumberInstance.EndPt; //下一段的起始点是上一段的结束点
            }
        }   
    }
}
