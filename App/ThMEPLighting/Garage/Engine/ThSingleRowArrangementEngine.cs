using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Engine
{
    public class ThSingleRowArrangementEngine : ThArrangementEngine
    {       
        public ThSingleRowArrangementEngine(
            ThLightArrangeParameter arrangeParameter,
            ThRacewayParameter racewayParameter)
            :base(arrangeParameter, racewayParameter)
        {
        }
        public override void Arrange(List<ThRegionBorder> regionBorders)
        {
            regionBorders.ForEach(o => Arrange(o));
        }
        private void Arrange(ThRegionBorder regionBorder)
        {
            // 预处理
            Preprocess(regionBorder);

            // 根据桥架中心线建立线槽
            var ports = new List<Point3d>();
            var cableCenterLines = new List<Line>();
            cableCenterLines.AddRange(DxLines);
            cableCenterLines.AddRange(FdxLines);
            using (var buildRacywayEngine = new ThBuildRacewayEngineEx(cableCenterLines, ArrangeParameter.Width))
            {
                buildRacywayEngine.Build();
                ports = buildRacywayEngine.GetPorts();
                //电缆桥架的边线和中线及配对的结果返回给->regionBorder
                //便于后期打印
                regionBorder.CableTrayCenters = buildRacywayEngine.SplitCenters;
                regionBorder.CableTraySides = buildRacywayEngine.SplitSides;
                regionBorder.CableTrayGroups = buildRacywayEngine.CenterWithSides;
                regionBorder.CableTrayPorts = buildRacywayEngine.CenterWithPorts;
            }

            // 创建灯和编号
            var lightEdges = new List<ThLightEdge>();
            DxLines.ForEach(o => lightEdges.Add(new ThLightEdge(o)));
            using (var buildNumberEngine = new ThSingleRowNumberEngine(ports, lightEdges, ArrangeParameter))
            {
                buildNumberEngine.Build();
                //将创建的灯边返回给->regionBorder
                regionBorder.LightEdges = buildNumberEngine.DxLightEdges;
            }
        }    
    }
}
