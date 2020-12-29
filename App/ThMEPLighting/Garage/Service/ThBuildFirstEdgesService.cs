using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service
{
    /// <summary>
    /// 根据中心线子路径，获取1号线对应的边
    /// </summary>
    public class ThBuildFirstEdgesService
    {
        private List<ThLinkPath> CenterLinkPaths { get; set; }
        public ThWireOffsetDataService WireOffsetDataService { get; set; }

        private List<ThFirstEdgeData> FirstEdgeDatas { get; set; }
        private double OffsetDis { get; set; }

        private ThBuildFirstEdgesService(
            double offsetDis,
            List<ThLinkPath> centerLinkPaths, 
            ThWireOffsetDataService wireOffsetDataService)
        {
            OffsetDis = offsetDis;
            CenterLinkPaths = centerLinkPaths;
            WireOffsetDataService = wireOffsetDataService;
            FirstEdgeDatas = new List<ThFirstEdgeData>();
        }
        public static List<ThFirstEdgeData> Build(
            List<ThLinkPath> centerLinkPaths,
            ThWireOffsetDataService wireOffsetDataService,
            double offsetDis)
        {
            var instance = new ThBuildFirstEdgesService(
                offsetDis, centerLinkPaths, wireOffsetDataService);
            instance.Build();
            return instance.FirstEdgeDatas;
        }
        private void Build()
        {
            CenterLinkPaths.ForEach(o=> Build(o));
        }
        private void Build(ThLinkPath centerLinkPath)
        {
            if(centerLinkPath.Path.Count==0)
            {
                return;
            }
            var firstEdge = centerLinkPath.Path.First().Edge;
            bool isStart = 
                centerLinkPath.Start.DistanceTo(firstEdge.StartPoint) <
                centerLinkPath.Start.DistanceTo(firstEdge.EndPoint);
            Point3d? outerStartPt = WireOffsetDataService.FindOuterStartPt(
                firstEdge, isStart);
                if(outerStartPt== null)
            {
                return;
            }
            var firstLightEdges = new List<ThLightEdge>();
            var firstSplitLines = new List<Line>();
            centerLinkPath.Path.ForEach(o =>
            {
                var first = WireOffsetDataService.FindFirstByCenter(o.Edge);
                firstSplitLines.AddRange(WireOffsetDataService.FindFirstSplitLines(first));
            });
            var links=ThFindLinkService.Find(firstSplitLines, WireOffsetDataService.FirstQueryInstance);
            links = ThFilterLinkService.Filter(links, centerLinkPath.Path.Select(o => o.Edge).ToList(), OffsetDis);
            firstSplitLines.AddRange(links);
            //后期若支持非灯线，则要把查找到的连接线，IsDX进行值查询
            //查找找到的连接线对应Center,再找到Center的Edge特性
            firstSplitLines.ForEach(o => firstLightEdges.Add(new ThLightEdge(o) { IsDX = true }));
            var firstEdgeData = new ThFirstEdgeData
            {
                CenterLinkPath= centerLinkPath,
                Start = outerStartPt.Value,
                FirstLightEdges = firstLightEdges,
            };
            FirstEdgeDatas.Add(firstEdgeData);
        }
    }
    /// <summary>
    /// 中心线子路径对应的1号线的边
    /// </summary>
    public class ThFirstEdgeData
    {
        public Point3d Start { get; set; }
        public List<ThLightEdge> FirstLightEdges { get; set; }
        public ThLinkPath CenterLinkPath { get; set; }
    }
}
