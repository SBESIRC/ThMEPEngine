using DotNetARX;
using Linq2Acad;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using Autodesk.AutoCAD.DatabaseServices;

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
            regionBorders.ForEach(o =>
            {
                var collectIds = Arrange(o);
                //ThEliminateService.Eliminate(RacewayParameter, o.RegionBorder, collectIds,ArrangeParameter.Width);
            });
        }
        private ObjectIdList Arrange(ThRegionBorder regionBorder)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 预处理
                Preprocess(regionBorder);

                // 根据桥架中心线建立线槽
                var ports = new List<Point3d>();
                var collectIds = new ObjectIdList();
                var cableCenterLines = new List<Line>();
                cableCenterLines.AddRange(DxLines);
                cableCenterLines.AddRange(FdxLines);
                using (var buildRacywayEngine = new ThBuildRacewayEngineEx(cableCenterLines, ArrangeParameter.Width))
                {
                    buildRacywayEngine.Build();
                    ports = buildRacywayEngine.GetPorts();
                    collectIds.AddRange(buildRacywayEngine.CreateGroup(RacewayParameter));
                }

                // 创建灯和编号
                var lightEdges = new List<ThLightEdge>();
                DxLines.ForEach(o => lightEdges.Add(new ThLightEdge(o)));        
                using (var buildNumberEngine = new ThSingleRowNumberEngine(ports, lightEdges, ArrangeParameter))
                {
                    buildNumberEngine.Build();                    
                    collectIds.AddRange(CreateLightAndNumber(buildNumberEngine.DxLightEdges));
                }

                // 返回灯和编号
                return collectIds;
            }
        }    
    }
}
