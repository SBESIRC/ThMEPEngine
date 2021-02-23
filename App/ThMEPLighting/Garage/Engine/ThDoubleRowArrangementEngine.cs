using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service;

namespace ThMEPLighting.Garage.Engine
{
    public class ThDoubleRowArrangementEngine : ThArrangementEngine
    {        
        public ThDoubleRowArrangementEngine(
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

            //识别内外圈
            var innerOuterCircles = new List<ThWireOffsetData>();
            using (var innerOuterEngine = new ThInnerOuterCirclesEngine(regionBorder.RegionBorder))
            {
                //需求变化2020.12.23,非灯线不参与编号传递
                //创建1、2号线，车道线merge，配对1、2号线
                innerOuterEngine.Width = ArrangeParameter.Width;
                innerOuterEngine.Reconize(DxLines, new List<Line>(), ArrangeParameter.RacywaySpace / 2.0);
                innerOuterCircles = innerOuterEngine.WireOffsetDatas;
            }

            //延伸非灯线
            FdxLines = ThExtendFdxLinesService.Extend(FdxLines, innerOuterCircles);
            
            //创建电缆桥架
            var ports = BuildCableTray(regionBorder, innerOuterCircles);
            
            //创建灯编号
            BuildLightNumer(ports, regionBorder, innerOuterCircles);
        }
        private void BuildLightNumer(
            List<Point3d> ports,
            ThRegionBorder regionBorder,
            List<ThWireOffsetData> innerOuterCircles)
        {
            //为了选起点，建图成功
            var centerLines = new List<Line>();
            var firstLines = new List<Line>();
            innerOuterCircles.ForEach(o => centerLines.Add(o.Center));
            innerOuterCircles.ForEach(o => firstLines.Add(o.First));
            centerLines = ThPreprocessLineService.Preprocess(centerLines);
            firstLines = ThPreprocessLineService.Preprocess(firstLines);
            var centerLightEdges = new List<ThLightEdge>();
            centerLines.ForEach(o => centerLightEdges.Add(new ThLightEdge(o.Normalize())));

            var firstLightEdges = new List<ThLightEdge>();
            firstLines.ForEach(o => firstLightEdges.Add(new ThLightEdge(o.Normalize())));

            //获取端点在DxLines的Port
            var centerPorts = GetDxCenterLinePorts(ports,  //灯线端口
                centerLightEdges.Where(o => o.IsDX).Select(o => o.Edge).ToList());

            //创建偏移1、2线索引服务，便于后期查询
            var wireOffsetDataService = ThWireOffsetDataService.Create(innerOuterCircles);
            //布灯
            using (var buildNumberEngine = new ThDoubleRowNumberEngine(
                centerPorts, centerLightEdges, firstLightEdges, ArrangeParameter))
            {
                var service = ThQueryLightBlockService.Create(regionBorder.RegionBorder, RacewayParameter.LaneLineBlockParameter.Layer);
                buildNumberEngine.QueryLightBlockService = service;
                buildNumberEngine.WireOffsetDataService = wireOffsetDataService;
                buildNumberEngine.Build();
                regionBorder.LightEdges.AddRange(buildNumberEngine.FirstLightEdges);
                regionBorder.LightEdges.AddRange(buildNumberEngine.SecondLightEdges);
            }
        }
        private List<Point3d> BuildCableTray(
            ThRegionBorder regionBorder, 
            List<ThWireOffsetData> innerOuterCircles)
        {
            //创建线槽
            var cableCenterLines = new List<Line>(); //线槽中心线
            var secondCurves = new List<Curve>();
            innerOuterCircles.ForEach(o =>
            {
                cableCenterLines.Add(o.First.Clone() as Line);
                cableCenterLines.Add(o.Second.Clone() as Line);
            });
            FdxLines.ForEach(o => cableCenterLines.Add(o.Clone() as Line));
            var ports = new List<Point3d>();
            using (var buildRacywayEngine = new ThBuildRacewayEngineEx(
                cableCenterLines, ArrangeParameter.Width))
            {
                //创建线槽
                buildRacywayEngine.Build();
                ports = buildRacywayEngine.GetPorts();
                //电缆桥架的边线和中线及配对的结果返回给->regionBorder
                //便于后期打印
                regionBorder.CableTrayCenters = buildRacywayEngine.SplitCenters;
                regionBorder.CableTraySides = buildRacywayEngine.SplitSides;
                regionBorder.CableTrayGroups = buildRacywayEngine.CenterWithSides;
                regionBorder.CableTrayPorts = buildRacywayEngine.CenterWithPorts;
            }
            return ports;
        }
        private List<Point3d> GetDxCenterLinePorts(
            List<Point3d> innerOuterPorts,
            List<Line> centerLines)
        {
            var centerPorts = new List<Point3d>();
            centerLines.ForEach(o =>
                {
                    var horVec = o.StartPoint.GetVectorTo(o.EndPoint);
                    var verVec = horVec.GetPerpendicularVector().GetNormal();
                    Point3d upSp = o.StartPoint + verVec.MultiplyBy(
                        ArrangeParameter.RacywaySpace / 2.0);
                    Point3d downSp = o.StartPoint - verVec.MultiplyBy(
                       ArrangeParameter.RacywaySpace / 2.0);
                    if(innerOuterPorts.Where(k=>k.DistanceTo(upSp)<=5.0 || k.DistanceTo(downSp) <= 5.0).Any())
                    {
                        centerPorts.Add(o.StartPoint);
                    }
                    Point3d upEp = o.EndPoint + verVec.MultiplyBy(
                        ArrangeParameter.RacywaySpace / 2.0);
                    Point3d downEp = o.EndPoint - verVec.MultiplyBy(
                       ArrangeParameter.RacywaySpace / 2.0);
                    if (innerOuterPorts.Where(k => k.DistanceTo(upEp) <= 5.0 || k.DistanceTo(downEp) <= 5.0).Any())
                    {
                        centerPorts.Add(o.EndPoint);
                    }
                });
            if(centerPorts.Count>1)
            {
                centerPorts = centerPorts.Distinct().ToList();
            }
            return centerPorts;
        }
    }
}
