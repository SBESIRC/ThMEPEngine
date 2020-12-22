﻿using Linq2Acad;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
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
            //对传入的线进行清洗    
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //对灯线中心线进行修剪、合并、分割和偏移操作
                Preprocess(regionBorder);

                //识别内外圈
                var innerOuterCircles = new List<ThWireOffsetData>();
                using (var innerOuterEngine = new ThInnerOuterCirclesEngine())
                {
                    //识别内外圈
                    innerOuterEngine.Reconize(DxLines, FdxLines, ArrangeParameter.RacywaySpace / 2.0);
                    innerOuterCircles = innerOuterEngine.WireOffsetDatas;
                }               

                //创建线槽
                var ports = BuildCableTray(innerOuterCircles); //线槽端口

                //电灯编号                
                var centerLightEdges = new List<ThLightEdge>();
                innerOuterCircles.ForEach(o =>
                {
                    centerLightEdges.Add(new ThLightEdge(o.Center) { IsDX=o.IsDX});
                });

                //获取端点在DxLines的Port
                var centerPorts = GetDxCenterLinePorts(ports,  //灯线端口
                    centerLightEdges.Where(o=>o.IsDX).Select(o => o.Edge).ToList());                         

                //创建偏移1、2线索引服务，便于后期查询
                var wireOffsetDataService=ThWireOffsetDataService.Create(innerOuterCircles);

                using (var buildNumberEngine = new ThDoubleRowNumberEngine(
                    centerPorts, centerLightEdges, ArrangeParameter, wireOffsetDataService))
                {
                    buildNumberEngine.Build();
                    Print(buildNumberEngine.FirstLightEdges);
                    Print(buildNumberEngine.SecondLightEdges);
                }
            }
        }
        private List<Point3d> BuildCableTray(List<ThWireOffsetData> innerOuterCircles)
        {
            //桥架中心线
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var cableCenterLines = new List<Line>();
                innerOuterCircles.ForEach(o =>
                {
                    cableCenterLines.Add(o.First);
                    cableCenterLines.Add(o.Second);
                });
                using (var buildRacywayEngine = new ThBuildRacewayEngine(
                    cableCenterLines, ArrangeParameter.Width))
                {
                    //创建线槽
                    buildRacywayEngine.Build();

                    //成组
                    buildRacywayEngine.CreateGroup(RacewayParameter);

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
                    if(innerOuterPorts.Where(k=>k.DistanceTo(upSp)<=1.0 || k.DistanceTo(downSp) <= 1.0).Any())
                    {
                        centerPorts.Add(o.StartPoint);
                    }
                    Point3d upEp = o.EndPoint + verVec.MultiplyBy(
                        ArrangeParameter.RacywaySpace / 2.0);
                    Point3d downEp = o.EndPoint - verVec.MultiplyBy(
                       ArrangeParameter.RacywaySpace / 2.0);
                    if (innerOuterPorts.Where(k => k.DistanceTo(upEp) <= 1.0 || k.DistanceTo(downEp) <= 1.0).Any())
                    {
                        centerPorts.Add(o.EndPoint);
                    }
                });
            return centerPorts;
        }
    }
}
