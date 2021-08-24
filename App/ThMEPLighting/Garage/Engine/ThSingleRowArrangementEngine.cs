using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using ThMEPLighting.Garage.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using System.Linq;
using ThMEPEngineCore.CAD;

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
            TrimAndShort(regionBorder);

            // 合并车道线
            var mergeDxLines = MergeDxLine(regionBorder.RegionBorder, DxLines);
            DxLines = Explode(mergeDxLines); //把合并的车道线重新设成

            // 清洗和过滤短线
            CleanAndFilter(); //对DxLines操作 
            DxLines.Cast<Entity>().ToList().CreateGroup(AcHelper.Active.Database,1);

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
