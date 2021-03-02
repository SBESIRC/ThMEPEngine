using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.LaneLine;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service;

namespace ThMEPLighting.Garage.Engine
{
    public class ThDoubleRowLoopArrangementEngine : ThLoopArrangementEngine
    {
        public ThDoubleRowLoopArrangementEngine(
            ThLightArrangeParameter arrangeParameter)
            :base (arrangeParameter)
        {
            ArrangeParameter = arrangeParameter;
        }
        public override void Arrange(List<ThRegionLightEdge> lightRegions)
        {
            lightRegions.ForEach(o => Arrange(o));
        }
        private void Arrange(ThRegionLightEdge lightRegion)
        {
            //从小汤车道线合并服务中获取合并的主道线，辅道线  (***Pay Attention***) 
            var mergeCurves = ThMergeLightCenterLines.Merge(
                lightRegion.RegionBorder, 
                lightRegion.LaneLines.Select(o=> ThGarageLightUtils.NormalizeLaneLine(o)).ToList(), 
                ThGarageLightCommon.LaneMergeRange);
            var centerLines =ThLaneLineEngine.Explode(mergeCurves.ToCollection()).Cast<Line>().ToList();

            //根据车道线，已布置的灯线分割1号线、2号线
            var instance = new ThSeparateLightEdgeService(
                centerLines, lightRegion.Edges, ArrangeParameter.RacywaySpace/2.0);
            instance.Separate();
            centerLines = ThPreprocessLineService.Preprocess(centerLines);
            var firstLines = ThPreprocessLineService.Preprocess(instance.FirstLines);
            var secondLines = ThPreprocessLineService.Merge(instance.SecondLines);

            var buildService = new ThBuildFirstSecondPairService(firstLines,
                 secondLines, ArrangeParameter.RacywaySpace);
            buildService.Build();
            var firstPairs = buildService.Pairs;

            var centerPorts = ThFindCenterLinePortsService.Find(centerLines);
            var centerLightEdges = new List<ThLightEdge>();
            centerLines.ForEach(o => centerLightEdges.Add(new ThLightEdge(ThGarageLightUtils.NormalizeLaneLine(o))));

            var firstLightEdges = new List<ThLightEdge>();
            firstLines.ForEach(o => firstLightEdges.Add(new ThLightEdge(ThGarageLightUtils.NormalizeLaneLine(o))));

            //布灯
            using (var buildNumberEngine = new ThDoubleRowNumberEngine(
                centerPorts, centerLightEdges, firstLightEdges, ArrangeParameter))
            {
                var wireOffsetDataService = new ThWireOffsetDataService(firstPairs);
                buildNumberEngine.WireOffsetDataService = wireOffsetDataService;
                var queryLightBlockService = new ThQueryLightBlockService(lightRegion.Lights.ToCollection());
                buildNumberEngine.QueryLightBlockService = queryLightBlockService;
                buildNumberEngine.Build();
                lightRegion.LightEdges.AddRange(buildNumberEngine.FirstLightEdges);
                lightRegion.LightEdges.AddRange(buildNumberEngine.SecondLightEdges);
            }
        }        
    }
}
