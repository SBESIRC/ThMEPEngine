using System;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThCollectDoorStoneService
    {
        public double EnvelopHeightIncrement { get; set; }
        public double EnvelopWidthIncrement { get; set; }
        public double EnvelopWidthMaxExtent { get; set; }
        public double EnvelopHeightMaxExtent { get; set; }
        public List<Tuple<Entity, List<Polyline>, double>> Results { get; private set; }
        private DBObjectCollection DoorMarks { get; set; }
        private ThCADCoreNTSSpatialIndex DoorStoneSpatialIndex { get; set; }
        public ThCollectDoorStoneService(
            DBObjectCollection doorStones,
            DBObjectCollection doorMarks)
        {
            DoorMarks = doorMarks;
            EnvelopHeightIncrement = 50;
            EnvelopWidthIncrement = 50;
            EnvelopWidthMaxExtent = 200;
            EnvelopHeightMaxExtent = 400;
            DoorStoneSpatialIndex = new ThCADCoreNTSSpatialIndex(doorStones);
            Results = new List<Tuple<Entity, List<Polyline>, double>>();
        }
        public void Build()
        {
            DoorMarks
                .Cast<Entity>()
                .Where(o => ThTextInfoService.IsDoorMark(ThTextInfoService.GetText(o)))
                .ForEach(o =>
                {
                    var strList = ThTextInfoService.Parse(ThTextInfoService.GetText(o));
                    double length = ThTextInfoService.GetLength(strList);
                    if (length > 0)
                    {                        
                        var center = ThTextInfoService.GetCenterLine(o);
                        var height = ThTextInfoService.GetHeight(o);
                        var stones = FindStones(center, length, height);
                        if(stones.Count ==2)
                        {
                            Results.Add(Tuple.Create(o, stones, length));
                        }
                    }
                });
        }
        private List<Polyline> FindStones(Line center,double length ,double height)
        {
            // 后续根据需要是否为高度增加步长搜索
            // 后续根据需要是否对找到大于2以上的门洞进行过滤
            for (int i = 1; i<= EnvelopWidthMaxExtent/ EnvelopWidthIncrement;i++)
            {
                var envelope = CreateEnvelope(center, 
                    length + i * 2 * EnvelopWidthIncrement, 
                    height + EnvelopHeightMaxExtent);                
                var stones = DoorStoneSpatialIndex.SelectCrossingPolygon(envelope).Cast<Polyline>().ToList();
                if(stones.Count==2)
                {
                    return stones;
                }
            }
            return new List<Polyline>();
        }
        private Polyline CreateEnvelope(Line center,double length,double height)
        {
            var vec = center.LineDirection();
            var midPt = center.StartPoint.GetMidPt(center.EndPoint);
            var sp = midPt - vec.MultiplyBy(length / 2.0);
            var ep = midPt + vec.MultiplyBy(length / 2.0);
            return ThDrawTool.ToOutline(sp, ep, height);
        }       
    }
}
