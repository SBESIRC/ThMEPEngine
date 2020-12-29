using System;
using Linq2Acad;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Engine
{
    /// <summary>
    /// 外圈布灯
    /// </summary>
    public class ThOuterCircleDistributionEngine : IDisposable
    {
        //1.  通过灯线中心线创建图,找到遍历的子路径
        //2.  根据中心线子路径，获取1号线对应的边
        //3.  根据中心线子路径的起始点和对应1号线的边，分割的子路径，创建LightGraph
        //4.  根据中心线子路径对应1号线分割产生的路径，设置布灯点
        //4.1 对每一条直路径进行分析，如果支路已经不灯，则此区域跳过
        //4.2 再根据布灯逻辑进行布点
        private List<Point3d> CenterPorts { get; set; }
        private List<ThLightEdge> CenterLineEdges { get; set; }
        private ThLightArrangeParameter ArrangeParameter { get;  set; }
        private ThWireOffsetDataService WireOffsetDataService { get;  set; }
        private Point3d Start { get; set; }
        public List<ThLightEdge> FirstLightEdges { get; set; }
        public ThOuterCircleDistributionEngine(
            List<Point3d> centerPorts,
            List<ThLightEdge> centerLineEdges,
            ThLightArrangeParameter arrangeParameter,
            ThWireOffsetDataService wireOffsetDataService)            
        {
            CenterPorts = centerPorts;
            CenterLineEdges = centerLineEdges;
            ArrangeParameter = arrangeParameter;
            WireOffsetDataService = wireOffsetDataService;
            Start = centerPorts.First();
            FirstLightEdges = new List<ThLightEdge>();
        }
        public ThOuterCircleDistributionEngine(
            List<Point3d> centerPorts,
            List<ThLightEdge> centerLineEdges,
            ThLightArrangeParameter arrangeParameter,
            ThWireOffsetDataService wireOffsetDataService,
            Point3d start)
            : this(centerPorts, centerLineEdges, arrangeParameter, wireOffsetDataService)
        {
            Start = CenterPorts.OrderBy(o=>o.DistanceTo(start)).First();
        }
        public void Dispose()
        {           
        }
        public void Distribute()
        {
            using (var acadDatabase=AcadDatabase.Active())
            {
                // 获取灯线中心线所有连通子路径
                var centerLightGraph = ThLightGraphService.Build(CenterLineEdges, Start);
                //centerLightGraph.Print();

                // 根据中心线子路径，获取1号线对应的边
                var firstEdgeDatas=ThBuildFirstEdgesService.Build(centerLightGraph.Links, 
                    WireOffsetDataService,ArrangeParameter.RacywaySpace/2.0);

                //firstEdgeDatas.Print();

                //获取1号线已布点的边
                FirstLightEdges = ThDoubleRowDistributeService.Distribute(
                    firstEdgeDatas, ArrangeParameter, WireOffsetDataService);
            }
        }
    }
}
