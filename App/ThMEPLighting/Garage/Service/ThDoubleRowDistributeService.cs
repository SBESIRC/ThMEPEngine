using System;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    /// <summary>
    /// 在灯边线上布灯点
    /// 灯边不点的入口类
    /// </summary>
    public abstract class ThDistributeMainService
    {
        /// <summary>
        /// 建图的边
        /// </summary>
        protected List<ThLightEdge> GraphEdges { get; set; }
        /// <summary>
        /// 已布灯的边
        /// </summary>
        protected List<ThLightEdge> DistributedEdges { get; set; }
        /// <summary>
        /// 图
        /// </summary>
        protected ThLightGraphService LightGraph { get; set; }
        /// <summary>
        /// 布灯参数
        /// </summary>
        protected ThLightArrangeParameter ArrangeParameter { get; set; }
        /// <summary>
        /// 查询灯块的服务
        /// </summary>
        protected ThQueryLightBlockService QueryLightBlockService { get; set; }
        public ThDistributeMainService(
            ThLightGraphService lightGraph, 
            ThLightArrangeParameter arrangeParameter,
            ThQueryLightBlockService queryLightBlockService
            )
        {
            LightGraph = lightGraph;
            ArrangeParameter = arrangeParameter;
            QueryLightBlockService = queryLightBlockService;
            GraphEdges = new List<ThLightEdge>();
            DistributedEdges = new List<ThLightEdge>();
            LightGraph.Links.ForEach(o =>
            {
                o.Path.ForEach(p => GraphEdges.Add(p));
            });
        }
        public abstract void Distribute();
        protected void RepairLineDir(List<Line> lines,Point3d startPt)
        {
            var newStartPt = new Point3d(startPt.X, startPt.Y, startPt.Z);
            for (int i =0;i<lines.Count;i++)
            {
                if (newStartPt.DistanceTo(lines[i].StartPoint) >
                    newStartPt.DistanceTo(lines[i].EndPoint))
                {
                    var tempPt = lines[i].StartPoint;
                    lines[i].StartPoint = lines[i].EndPoint;
                    lines[i].EndPoint = tempPt;
                }
            }
        }
        protected abstract List<List<Point3d>> GetCanLayoutSegments(List<Line> lines);
        protected List<List<Point3d>> MergeLayoutSegments(List<ThAdjustLightDistributePosService> adjustServices)
        {
            var res = new List<List<Point3d>>();
            var data = new List<Tuple<Point3d, Point3d>>();
            adjustServices.ForEach(o => data.AddRange(o.Distribute()));
            for (int i = 0; i < data.Count; i++)
            {
                Point3d L = data[i].Item1, R = data[i].Item2;
                if (res.Count == 0 || res.Last().Last() != L)
                    res.Add(new List<Point3d>() { L, R });
                else
                    res.Last().Add(R);
            }
            return res;
        }
    }

    /// <summary>
    /// 对1号线进行布点
    /// 1、通过灯线中心线创建图,找到遍历的子路径
    /// 2、根据1的结果，获取1号线对应的边
    /// 3、根据2的结果，创建LightGraph
    /// 4、根据3的结果，计算或从图纸获取布灯点
    /// </summary>
    public class ThDoubleRowDistributeService: ThDistributeMainService
    {
        private ThWireOffsetDataService WireOffsetDataService { get; set; }
        public ThDoubleRowDistributeService(
            ThLightGraphService lightGraph, 
            ThLightArrangeParameter arrangeParameter,
            ThWireOffsetDataService wireOffsetDataService,
            ThQueryLightBlockService queryLightBlockService)
            :base(lightGraph, arrangeParameter, queryLightBlockService)
        {
            WireOffsetDataService = wireOffsetDataService;
        }
        public override void Distribute()
        {
            if (LightGraph != null && ArrangeParameter != null)
            {
                LightGraph.Links.ForEach(o => Distribute(o));
            }
        }
        private void Distribute(ThLinkPath singleLinkPath)
        {
            var startPt = singleLinkPath.Start;
            for (int i = 0; i < singleLinkPath.Path.Count; i++)
            {
                var currentEdge = singleLinkPath.Path[i];
                if(!currentEdge.IsDX)
                {
                    //如果是非灯线的边，在其中点创建一个灯，用于传递起始灯编号
                    var midPt = ThGeometryTool.GetMidPt(currentEdge.Edge.StartPoint, currentEdge.Edge.EndPoint);
                    currentEdge.LightNodes.Add(new ThLightNode() { Position = midPt });
                    DistributedEdges.Add(currentEdge);
                    continue;
                }
                var edges = new List<ThLightEdge> { currentEdge };
                int j = i + 1;
                for (; j < singleLinkPath.Path.Count; j++)
                {
                    var preEdge = edges.Last();
                    var nextEdge = singleLinkPath.Path[j];
                    if(ThGarageUtils.IsLessThan45Degree(
                        preEdge.Edge.LineDirection(),
                        nextEdge.Edge.LineDirection()))
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
                RepairLineDir(lines, startPt);
                var segments = GetCanLayoutSegments(lines);//可以布置的区域
                startPt = lines[lines.Count-1].EndPoint;//调整起点到末端
                var doubleRowService = new ThBuildDoubleRowPosService(
                    edges, segments, ArrangeParameter, QueryLightBlockService);
                doubleRowService.Build();
                DistributedEdges.AddRange(edges);
            }
        }

        protected override List<List<Point3d>> GetCanLayoutSegments(List<Line> lines)
        {
            var linearrangment = new List<ThAdjustDoubleRowDistributePosService>();
            for (int i = 0; i < lines.Count; i++)
            {
                linearrangment.Add(new ThAdjustDoubleRowDistributePosService(
                    Tuple.Create(lines[i].StartPoint, lines[i].EndPoint),
                    ArrangeParameter, GraphEdges, DistributedEdges, WireOffsetDataService));
            }
            return MergeLayoutSegments(linearrangment.Cast<ThAdjustLightDistributePosService>().ToList());
        }
    } 
}
