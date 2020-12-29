using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Worker;

namespace ThMEPLighting.Garage.Service
{
    public class ThDoubleRowNumberService: ThNumberService
    {
        //是对1号线进行编号
        private ThWireOffsetDataService WireOffsetDataService { get; set; }
        private ThDoubleRowNumberService(
            ThLightGraphService lightGraph,
            ThLightArrangeParameter arrangeParameter,
            ThWireOffsetDataService wireOffsetDataService
            ) :base(lightGraph, arrangeParameter)
        {
            WireOffsetDataService = wireOffsetDataService;
        }
        public static void Number(
            ThLightGraphService lightGraph,
            ThLightArrangeParameter arrangeParameter,
            ThWireOffsetDataService wireOffsetDataService)
        {
            var instance = new ThDoubleRowNumberService(
                lightGraph, arrangeParameter, wireOffsetDataService);
            instance.Number();
        }
        protected override void Number()
        {
            LightGraph.Links.ForEach(o => Number(o));
        }
        private void Number(ThLinkPath singleLinkPath)
        {
            var findStartIndex = ThFindStartIndexService.Find(LightGraph, singleLinkPath, ArrangeParameter.LoopNumber, false);
            if (!findStartIndex.IsFind)
            {
                return;
            }
            int startIndex = findStartIndex.StartIndex;
            if (!singleLinkPath.Path[0].IsDX)
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
                //对当前直段编号
                var numberInstance = ThDoubleRowLightNumber.Build(edges, ArrangeParameter.LoopNumber, startIndex);
                startIndex = numberInstance.LastIndex; //下一段的起始序号
            }
        }
    }
}
