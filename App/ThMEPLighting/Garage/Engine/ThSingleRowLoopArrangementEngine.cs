using NFox.Cad;
using System.Collections.Generic;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service;

namespace ThMEPLighting.Garage.Engine
{
    public class ThSingleRowLoopArrangementEngine : ThLoopArrangementEngine
    {
        public ThSingleRowLoopArrangementEngine(
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
            //过滤(单排布灯的边一定要在车道线上)
            lightRegion.Edges=ThGarageLightUtils.FilterDistributedEdges(
                lightRegion.Edges, lightRegion.LaneLines);

            var nodeLines =ThPreprocessLineService.Preprocess(lightRegion.Edges);
            // 创建灯和编号
            var lightEdges = new List<ThLightEdge>();
            nodeLines.ForEach(o => lightEdges.Add(new ThLightEdge(ThGarageLightUtils.NormalizeLaneLine(o))));
            var ports = ThFindCenterLinePortsService.Find(nodeLines);
            using (var buildNumberEngine = new ThSingleRowNumberEngine(ports, lightEdges, ArrangeParameter))
            {
                var queryLightBlockService = new ThQueryLightBlockService(lightRegion.Lights.ToCollection());
                buildNumberEngine.QueryLightBlockService = queryLightBlockService;
                buildNumberEngine.Build();
                //将创建的灯边返回给->regionBorder
                lightRegion.LightEdges = buildNumberEngine.DxLightEdges;
            }
        }       
    }
}
