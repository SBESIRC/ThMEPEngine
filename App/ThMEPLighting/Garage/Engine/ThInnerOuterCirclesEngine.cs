using System;
using ThCADCore.NTS;
using ThMEPLighting.Common;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.LaneLine;
using System.Linq;

namespace ThMEPLighting.Garage.Engine
{
    public class ThInnerOuterCirclesEngine : IDisposable
    {
        /// <summary>
        /// 创建内外圈
        /// </summary>
        public List<ThWireOffsetData> WireOffsetDatas { get; private set; }
        private Polyline Border { get; set; }
        /// <summary>
        /// 线槽宽度
        /// </summary>
        public double Width { get; set; } 
        public ThInnerOuterCirclesEngine(Polyline border)
        {            
            Border = border;
            WireOffsetDatas = new List<ThWireOffsetData>();
        } 
        public void Dispose()
        {           
        }
        public void Reconize(List<Line> dxLines,List<Line> fdxLines, double offsetDistance)
        {
            //var splitLineTuple = Split(dxLines, fdxLines);           
            //单位化
            var dxNomalLines = new List<Line>();
            var fdxNomalLines = new List<Line>();

            //修正方向
            dxLines.ForEach(o => dxNomalLines.Add(NormalizeLaneLine(o)));
            fdxLines.ForEach(o => fdxNomalLines.Add(NormalizeLaneLine(o)));

            //创建非灯线的偏移
            fdxNomalLines.ForEach(o =>
            {
                var offsetLines = ThOffsetLineService.Offset(o, false ? offsetDistance : 0.0);
                var offsetData = new ThWireOffsetData
                {
                    Center = o,
                    First = offsetLines.First as Line,
                    Second = offsetLines.Second as Line,
                    IsDX = false
                };
                WireOffsetDatas.Add(offsetData);
            });
            //从小汤车道线合并服务中获取合并的主道线，辅道线            
             var mergeCurves=ThMergeLightCenterLines.Merge(Border, dxNomalLines,301);
            //通过中心线往两侧偏移
            var offsetCurves = Offset(mergeCurves.Cast<Curve>().ToList(), offsetDistance);            
            //让1号线、2号线连接
            offsetCurves =ThExtendService.Extend(offsetCurves, Width);
            
            //为中心线找到对应的1号线和2号线
            var dxWireOffsetDatas=ThFindFirstLinesService.Find(offsetCurves, offsetDistance);
            WireOffsetDatas.AddRange(dxWireOffsetDatas);
        }  

        private Line NormalizeLaneLine(Line line,double tolerance=0.5)
        {
            var newLine = new Line(line.StartPoint,line.EndPoint);
            if(Math.Abs(line.StartPoint.Y- line.EndPoint.Y)<= tolerance)
            {
                if(line.StartPoint.Y< line.EndPoint.Y)
                {
                    newLine = new Line(line.StartPoint, line.EndPoint);
                }
                else
                {
                    newLine = new Line(line.EndPoint, line.StartPoint);
                }
            }
            return newLine.Normalize();
        }

        private Tuple<List<Line>,List<Line>> Split(List<Line> dxLines, List<Line> fdxLines)
        {
            //在T型、十字型处分割线
            var totalLines = new List<Line>();
            dxLines.ForEach(o => totalLines.Add(new Line(o.StartPoint, o.EndPoint)));
            fdxLines.ForEach(o => totalLines.Add(new Line(o.StartPoint, o.EndPoint)));

            var splitDxLines = new List<Line>();
            var splitFdxLines = new List<Line>();
            using (var splitEngine = new ThSplitLineEngine(totalLines))
            {
                splitEngine.Split();
                foreach (var item in splitEngine.Results)
                {
                    if (dxLines.IsContains(item.Key))
                    {
                        if (item.Value.Count > 0)
                        {
                            splitDxLines.AddRange(item.Value);
                        }
                        else
                        {
                            splitDxLines.Add(new Line(item.Key.StartPoint, item.Key.EndPoint));
                        }
                    }
                    else if (fdxLines.IsContains(item.Key))
                    {
                        if (item.Value.Count > 0)
                        {
                            splitFdxLines.AddRange(item.Value);
                        }
                        else
                        {
                            splitFdxLines.Add(new Line(item.Key.StartPoint, item.Key.EndPoint));
                        }
                    }
                }
            }
            return Tuple.Create(splitDxLines, splitFdxLines);
        }
        private List<Tuple<Curve, Curve, Curve>> Offset(List<Curve> curves, double offsetDis)
        {
            var results = new List<Tuple<Curve, Curve, Curve>>();
            curves.ForEach(o =>
            {
                if (o.GetLength() >= 10)
                {
                    var instance = ThOffsetLineService.Offset(o, offsetDis);
                    results.Add(Tuple.Create(o, instance.First, instance.Second));
                }
            });
            return results;
        }
    }
}
