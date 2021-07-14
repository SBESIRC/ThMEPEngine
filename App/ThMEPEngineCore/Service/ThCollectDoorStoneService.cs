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
        public double DoorStoneLength { get; set; }
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
            DoorStoneLength = 50;
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
                        var textDir = center.LineDirection();
                        var stoneCenterLength = length - DoorStoneLength;
                        if(stones.Count >=2)
                        {
                            //Results.Add(Tuple.Create(o, stones, length));
                            int max = stones.Count;
                            double Minlength = double.MaxValue;
                            double MinAngle = double.MaxValue;
                            Tuple<Entity, List<Polyline>, double> temp=null;
                            for (int i=0;i<max-1;i++)
                                for(int j=i+1;j<max;j++)
                                {
                                    var doorCenterDir = stones[i].GetCenter().GetVectorTo(stones[j].GetCenter());
                                    var angle = Math.Min(textDir.GetAngleTo(doorCenterDir), Math.PI - textDir.GetAngleTo(doorCenterDir));
                                    var realLength= PolylineDistance(stones[i], stones[j]) * Math.Cos(angle);
                                    if (Math.Abs(realLength- stoneCenterLength) < ThMEPEngineCoreCommon.DoorStoneWidthTolerance)//距离调整
                                    {
                                        if(ThAuxiliaryUtils.DoubleEquals(angle, MinAngle, 1.0 / 180 * Math.PI) && realLength < Minlength)
                                        {
                                            temp = Tuple.Create(o, new List<Polyline>() { stones[i], stones[j] }, length);
                                            //Results.Add(Tuple.Create(o, new List<Polyline>() { stones[i], stones[j] }, length));
                                            Minlength = realLength;
                                            MinAngle = angle;
                                        }
                                        else if(angle < MinAngle)
                                        {
                                            temp = Tuple.Create(o, new List<Polyline>() { stones[i], stones[j] }, length);
                                            //Results.Add(Tuple.Create(o, new List<Polyline>() { stones[i], stones[j] }, length));
                                            Minlength = realLength;
                                            MinAngle = angle;
                                        }
                                    }
                                }

                            if (temp != null)
                            {
                                Results.Add(temp);
                            }
                        }
                    }
                });
        }
        /// <summary>
        /// 返回两条闭合Polyline中心距离
        /// </summary>
        /// <param name="firstpol">第一条Polyline</param>
        /// <param name="secondpol">第二条Polyline</param>
        /// <returns>中心距离</returns>
        private double PolylineDistance(Polyline firstpol,Polyline secondpol)
        {
            return firstpol.GetCenter().DistanceTo(secondpol.GetCenter());
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
                if(stones.Count>=2)
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
