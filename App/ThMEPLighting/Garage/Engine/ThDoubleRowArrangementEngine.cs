using Linq2Acad;
using DotNetARX;
using System.Linq;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;

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
            regionBorders.ForEach(o =>
            {
                var collectIds = Arrange(o);

                //清除原有构件
                //暂时取消此功能(20210201),以免误删灯
                //ThEliminateService.Eliminate(RacewayParameter, o.RegionBorder, collectIds, ArrangeParameter.Width);
            });
        }
        private ObjectIdList Arrange(ThRegionBorder regionBorder)
        {
            var collectIds = new ObjectIdList();
            //对传入的线进行清洗    
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //对灯线中心线进行修剪、合并、分割和偏移操作
                Preprocess(regionBorder);

                //识别内外圈
                var innerOuterCircles = new List<ThWireOffsetData>();
                using (var innerOuterEngine = new ThInnerOuterCirclesEngine(regionBorder.RegionBorder))
                {
                    //需求变化2020.12.23,非灯线不参与编号传递
                    //创建1、2号线，车道线merge，配对1、2号线
                    innerOuterEngine.Width = ArrangeParameter.Width;
                    innerOuterEngine.Reconize(DxLines,new List<Line>(), ArrangeParameter.RacywaySpace / 2.0);
                    innerOuterCircles = innerOuterEngine.WireOffsetDatas;
                }

                //延伸非灯线
                FdxLines = ThExtendFdxLinesService.Extend(FdxLines, innerOuterCircles);
 
                //创建线槽
                var ports = BuildCableTray(innerOuterCircles,ref collectIds); //线槽端口

                //电灯编号        
                //需求变化2020.12.23,非灯线不参与编号传递

                //为了选起点，建图成功
                var centerLines = new List<Line>();
                var firstLines = new List<Line>();
                innerOuterCircles.ForEach(o => centerLines.Add(o.Center));
                innerOuterCircles.ForEach(o => firstLines.Add(o.First));
                using (var precessEngine = new ThLightLinePreprocessEngine())
                {
                    centerLines=precessEngine.Preprocess(centerLines);
                    firstLines= precessEngine.Preprocess(firstLines);                    
                }
                var centerLightEdges = new List<ThLightEdge>();                
                centerLines.ForEach(o => centerLightEdges.Add(new ThLightEdge(o.Normalize())));

                var firstLightEdges = new List<ThLightEdge>();
                firstLines.ForEach(o => firstLightEdges.Add(new ThLightEdge(o.Normalize())));
                
                //获取端点在DxLines的Port
                var centerPorts = GetDxCenterLinePorts(ports,  //灯线端口
                    centerLightEdges.Where(o=>o.IsDX).Select(o => o.Edge).ToList());

                //创建偏移1、2线索引服务，便于后期查询
                var wireOffsetDataService=ThWireOffsetDataService.Create(innerOuterCircles);

                //布灯
                using (var buildNumberEngine = new ThDoubleRowNumberEngine(
                    centerPorts, centerLightEdges, firstLightEdges,
                    ArrangeParameter, wireOffsetDataService))
                {
                    buildNumberEngine.Build();
                    collectIds.AddRange(Print(buildNumberEngine.FirstLightEdges));
                    collectIds.AddRange(Print(buildNumberEngine.SecondLightEdges));
                }
            }
            return collectIds;
        }
        private List<Point3d> BuildCableTray(List<ThWireOffsetData> innerOuterCircles,ref ObjectIdList collectIds)
        {
            //桥架中心线
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var firstCurves = new List<Curve>();
                var secondCurves = new List<Curve>();
                innerOuterCircles.ForEach(o =>
                {
                    firstCurves.Add(o.First.Clone() as Curve);
                    secondCurves.Add(o.Second.Clone() as Curve);
                });
                using (var buildRacywayEngine = new ThBuildDoubleRacewayEngine(
                    firstCurves, secondCurves, 
                    FdxLines.Cast<Curve>().ToList(), 
                    ArrangeParameter.Width,RacewayParameter))
                {
                    //创建线槽
                    buildRacywayEngine.Build();

                    collectIds.AddRange(buildRacywayEngine.DrawObjIs);

                    //获取参数
                    return buildRacywayEngine.GetPorts();
                }
            }
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
