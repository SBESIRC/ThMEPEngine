using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
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
            //对传入的线进行清洗    
            using (AcadDatabase acadDatabase=AcadDatabase.Active())  
            {
                //预处理
                Preprocess(regionBorder);

                //桥架中心线，建立线槽
                var cableCenterLines = new List<Line>();
                cableCenterLines.AddRange(DxLines);
                cableCenterLines.AddRange(FdxLines);
                var ports = new List<Point3d>();
                using (var buildRacywayEngine = new ThBuildRacewayEngine(cableCenterLines, ArrangeParameter.Width))
                {
                    //创建线槽
                    buildRacywayEngine.Build();
                    //成组
                    buildRacywayEngine.CreateGroup(RacewayParameter);
                    //获取参数
                    ports = buildRacywayEngine.GetPorts();
                }
                //电灯编号
                var lightEdges = new List<ThLightEdge>();
                DxLines.ForEach(o => lightEdges.Add(new ThLightEdge(o)));
                FdxLines.ForEach(o => lightEdges.Add(new ThLightEdge(o) { IsDX = false }));                
                using (var buildNumberEngine = new ThSingleRowNumberEngine(
                     ports, lightEdges, ArrangeParameter))
                {
                    buildNumberEngine.Build();
                    Print(buildNumberEngine.DxLightEdges);
                }
            }
        }    
    }
}
