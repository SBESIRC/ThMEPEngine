using System.Linq;
using ThCADExtension;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Worker;

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
            if (LightGraph == null || LightGraph.Links.Count == 0)
            {
                return;
            }
            LightGraph.Links.ForEach(o => Number(o));
        }
        private void Number(ThLinkPath singleLinkPath)
        {
            var findStartIndex = ThFindStartIndexService.Find(LightGraph, singleLinkPath, ArrangeParameter.LoopNumber,true);  
            if(!findStartIndex.IsFind)
            {
                findStartIndex.StartIndex = 1;
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
                    var nextEdge = singleLinkPath.Path[j];
                    if (ThGarageUtils.IsLessThan45Degree(preEdge.Edge.LineDirection(), nextEdge.Edge.LineDirection()))
                    {
                        edges.Add(nextEdge);
                    }
                    else
                    {
                        break;  //拐弯
                    }
                }
                i = j - 1;                
                //对当前直段编号
                var numberInstance = ThSingleRowLightNumber.Build(edges, ArrangeParameter.LoopNumber, startIndex);
                startIndex = numberInstance.LastIndex; //下一段的起始序号
            }
        }   
    }
}
