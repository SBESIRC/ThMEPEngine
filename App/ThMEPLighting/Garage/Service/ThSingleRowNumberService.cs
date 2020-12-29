using System.Linq;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Worker;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service
{
    public class ThSingleRowNumberService: ThNumberService
    {        
        private ThSingleRowNumberService(
            ThLightGraphService lightGraph,
            ThLightArrangeParameter arrangeParameter)
            :base(lightGraph, arrangeParameter)
        {            
        }
        public static void Number(ThLightGraphService lightGraph,
            ThLightArrangeParameter arrangeParameter)
        {
            var instance = new ThSingleRowNumberService(lightGraph, arrangeParameter);
            instance.Number();
        }
        protected override void Number()
        {
            LightGraph.Links.ForEach(o => Number(o));
        }
        private void Number(ThLinkPath singleLinkPath)
        {
            var findStartIndex = ThFindStartIndexService.Find(LightGraph, singleLinkPath, ArrangeParameter.LoopNumber,true);  
            if(!findStartIndex.IsFind)
            {
                return;
            }
            int startIndex = findStartIndex.StartIndex;
            if(!singleLinkPath.Path[0].IsDX)
            {
                startIndex = findStartIndex.FindIndex;
            }
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
                //对当前直段编号
                var numberInstance = ThSingleRowLightNumber.Build(edges, ArrangeParameter.LoopNumber, startIndex);
                startIndex = numberInstance.LastIndex; //下一段的起始序号
            }
        }   
    }
}
