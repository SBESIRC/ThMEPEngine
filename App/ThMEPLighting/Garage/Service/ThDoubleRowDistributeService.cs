using System;
using System.Linq;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service
{
    /// <summary>
    /// 对1号线进行布点
    /// 1、通过灯线中心线创建图,找到遍历的子路径
    /// 2、根据1的结果，获取1号线对应的边
    /// 3、根据2的结果，创建LightGraph
    /// 4、根据3的结果，计算或从图纸获取布灯点
    /// </summary>
    public class ThDoubleRowDistributeService
    {
        private List<ThFirstEdgeData> FirstEdgeDatas { get; set; }
        private ThLightArrangeParameter ArrangeParameter { get; set; }
        private ThWireOffsetDataService WireOffsetDataService { get; set; }
        /// <summary>
        /// 1号边已布点的边
        /// </summary>
        private List<ThLightEdge> FirstLightEdges { get; set; }
        
        private ThDoubleRowDistributeService(
            List<ThFirstEdgeData> firstEdgeDatas, 
            ThLightArrangeParameter arrangeParameter,
            ThWireOffsetDataService wireOffsetDataService)
        {
            FirstEdgeDatas = firstEdgeDatas;
            ArrangeParameter = arrangeParameter;
            WireOffsetDataService = wireOffsetDataService;
            FirstLightEdges = new List<ThLightEdge>();
        }
        public static List<ThLightEdge> Distribute(
            List<ThFirstEdgeData> firstEdgeDatas,
            ThLightArrangeParameter arrangeParameter,
            ThWireOffsetDataService wireOffsetDataService)
        {
            var instance = new ThDoubleRowDistributeService(
                firstEdgeDatas, arrangeParameter, wireOffsetDataService);
            instance.Distribute();
            return instance.FirstLightEdges;
        }
        private void Distribute()
        {
            FirstEdgeDatas.ForEach(o=> Distribute(o));
        }
        private void Distribute(ThFirstEdgeData firstEdgeData)
        {
            var firstLightGraph = ThLightGraphService.Build(firstEdgeData.FirstLightEdges, firstEdgeData.Start);
            firstLightGraph.Links.ForEach(o=>BuildEdgePoints(o));
        }
        private void BuildEdgePoints(ThLinkPath singleLinkPath)
        {
            var basePt = singleLinkPath.Start;
            for (int i = 0; i < singleLinkPath.Path.Count; i++)
            {
                var currentEdge = singleLinkPath.Path[i];
                if(!currentEdge.IsDX)
                {
                    //如果是非灯线的边，在其中点创建一个灯，用于传递起始灯编号
                    var midPt = ThGeometryTool.GetMidPt(currentEdge.Edge.StartPoint, currentEdge.Edge.EndPoint);
                    currentEdge.LightNodes.Add(new ThLightNode() { Position = midPt });
                    FirstLightEdges.Add(currentEdge);
                    continue;
                }
                var edges = new List<ThLightEdge> { currentEdge };
                int j = i + 1;
                for (; j < singleLinkPath.Path.Count; j++)
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
                var maxPts = edges.GetMaxPts();                
                //分析在线路上无需布灯的区域，返回可以布点的区域
                var splitPts=ThDoubleRowDistributeExService.Distribute(maxPts, ArrangeParameter, WireOffsetDataService, FirstLightEdges);
                ThBuildDoubleRowPosService.Build(edges, splitPts,ArrangeParameter);
                FirstLightEdges.AddRange(edges);
            }
        }      
    } 
}
