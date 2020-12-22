using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Garage.Model;

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

        private ThBuildFirstEdgesService(
            List<ThLinkPath> centerLinkPaths, 
            ThWireOffsetDataService wireOffsetDataService)
        {
            CenterLinkPaths = centerLinkPaths;
            WireOffsetDataService = wireOffsetDataService;
            FirstEdgeDatas = new List<ThFirstEdgeData>();
        }
        public static List<ThFirstEdgeData> Build(
            List<ThLinkPath> centerLinkPaths,
            ThWireOffsetDataService wireOffsetDataService)
        {
            var instance = new ThBuildFirstEdgesService(
                centerLinkPaths, wireOffsetDataService);
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
            centerLinkPath.Path.ForEach(o =>
            {
                var first = WireOffsetDataService.FindFirstByCenter(o.Edge);
                var firstSplitLines = WireOffsetDataService.FindFirstSplitLines(first);
                firstSplitLines.ForEach(f => firstLightEdges.Add(new ThLightEdge(f) { IsDX = o.IsDX }));
            });
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
