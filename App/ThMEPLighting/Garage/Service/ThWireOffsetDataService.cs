using System.Linq;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Engine;

namespace ThMEPLighting.Garage.Service
{
    /// <summary>
    /// 中心线产生偏移后
    /// 对1号线分割
    /// 根据中心线能找到对应的1号线、2号线
    /// </summary>
    public class ThWireOffsetDataService
    {
        private double coincideTolerance { get; set; }
        /// <summary>
        /// 1号线分割后对应的线
        /// </summary>
        public Dictionary<Line, List<Line>> FirstSplitResult { get; private set; }
        /// <summary>
        /// 灯线中心线按灯槽间距Offset对应的数据
        /// </summary>
        public List<ThWireOffsetData> WireOffsetDatas { get; private set; }
        /// <summary>
        /// 1号线所有分割线的索引
        /// </summary>
        public ThQueryLineService FirstQueryInstance { get; private set; }
        private ThWireOffsetDataService(List<ThWireOffsetData> wireOffsetDatas)
        {
            WireOffsetDatas = wireOffsetDatas;
            FirstSplitResult = new Dictionary<Line, List<Line>>();
            coincideTolerance = ThGarageLightCommon.LineCoincideTolerance;
        }
        public static ThWireOffsetDataService Create(List<ThWireOffsetData> wireOffsetDatas)
        {
            var instance = new ThWireOffsetDataService(wireOffsetDatas);
            instance.Create();
            return instance;
        }
        private void Create()
        {
            var firstLines = WireOffsetDatas.Select(o => o.First).ToList();
            using (var splitLineEngine =new ThSplitLineEngine(firstLines))
            {
                splitLineEngine.Split();
                FirstSplitResult = splitLineEngine.Results;
            }
            var firstSplitLines = new List<Line>();
            FirstSplitResult.ForEach(o => firstSplitLines.AddRange(o.Value));
            FirstQueryInstance = ThQueryLineService.Create(firstSplitLines);
        }
        /// <summary>
        /// 通过1号线的分割线，找到其所属的1号线
        /// </summary>
        /// <param name="firstSplitLine"></param>
        /// <returns></returns>
        public Line FindFirstBySplitLine(Line firstSplitLine)
        {
            var results = FirstSplitResult.Where(o => o.Value.IsContains(firstSplitLine, coincideTolerance));
            return results.Count() > 0 ? results.First().Key : new Line();
        }
        private bool IsContains(List<Line> lines,Line line)
        {
            return lines.Where(o => line.IsCoincide(o, coincideTolerance)).Any();
        }
        /// <summary>
        /// 获取1号线对应的分割线
        /// </summary>
        /// <param name="first"></param>
        /// <returns></returns>
        public List<Line> FindFirstSplitLines(Line first)
        {
            foreach(var split in FirstSplitResult)
            {
                if(first.IsCoincide(split.Key, coincideTolerance))
                {
                    return split.Value;
                }
            }
            return new List<Line>();
        }
        /// <summary>
        /// 通过中心线找到对应的1号线
        /// </summary>
        /// <param name="center"></param>
        /// <returns></returns>
        public Line FindFirstByCenter(Line center)
        {
            var results=WireOffsetDatas.Where(o => o.Center.IsCoincide(center, coincideTolerance));
            return results.Count() > 0 ? results.First().First : new Line();
        }
        /// <summary>
        /// 通过1号线找到对应的中心线
        /// </summary>
        /// <param name="center"></param>
        /// <returns></returns>
        public Line FindCenterByFirst(Line first)
        {
            var results = WireOffsetDatas.Where(o => o.First.IsCoincide(first, coincideTolerance));
            return results.Count() > 0 ? results.First().First : new Line();
        }
        /// <summary>
        /// 通过中心线找到对应的2号线
        /// </summary>
        /// <param name="center"></param>
        /// <returns></returns>
        public Line FindSecondByCenter(Line center)
        {
            var results = WireOffsetDatas.Where(o => o.Center.IsCoincide(center, coincideTolerance));
            return results.Count() > 0 ? results.First().Second : new Line();
        }
        /// <summary>
        /// 通过1号线找到对应的2号线
        /// </summary>
        /// <param name="center"></param>
        /// <returns></returns>
        public Line FindSecondByFirst(Line first)
        {
            var results = WireOffsetDatas.Where(o => o.First.IsCoincide(first, coincideTolerance));
            return results.Count() > 0 ? results.First().Second : new Line();
        }
        /// <summary>
        /// 获取中心线起点，对应1号线边的起点
        /// </summary>
        /// <param name="center"></param>
        /// <param name="isStart"></param>
        /// <returns></returns>
        public Point3d? FindOuterStartPt(Line center,bool isStart)
        {
            var first = FindFirstByCenter(center);
            if(first.Length==0)
            {
                return null;
            }
            var centerVec = center.StartPoint.GetVectorTo(center.EndPoint);
            var firstVec = first.StartPoint.GetVectorTo(first.EndPoint);
            if (isStart)
            {
                Point3d pt = center.StartPoint;
                if (centerVec.IsCodirectionalTo(firstVec, new Tolerance(1.0, 1.0)))
                {
                    return first.StartPoint;
                }
                else if (centerVec.IsCodirectionalTo(firstVec.Negate(), new Tolerance(1.0, 1.0)))
                {
                    return first.EndPoint;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                Point3d pt = center.EndPoint;
                if (centerVec.IsCodirectionalTo(firstVec, new Tolerance(1.0, 1.0)))
                {
                    return first.EndPoint;
                }
                else if (centerVec.IsCodirectionalTo(firstVec.Negate(), new Tolerance(1.0, 1.0)))
                {
                    return first.StartPoint;
                }
                else
                {
                    return null;
                }
            }
        }
        public Point3d? FindOuterStartPt(Point3d centerStartPt)
        {
            var centerlines = WireOffsetDatas.Where(o =>o.Center.IsLink(centerStartPt));
            if(centerlines.Count()==0)
            {
                return null;
            }
            var center = centerlines.First().Center;
            var first = centerlines.First().First;
            var centerVec = center.StartPoint.GetVectorTo(center.EndPoint);
            var firstVec = first.StartPoint.GetVectorTo(first.EndPoint);

            bool isStart = centerStartPt.DistanceTo(center.StartPoint)
                <  centerStartPt.DistanceTo(center.EndPoint);
            if (isStart)
            {
                Point3d pt = center.StartPoint;
                if (centerVec.IsCodirectionalTo(firstVec, new Tolerance(1.0, 1.0)))
                {
                    return first.StartPoint;
                }
                else if (centerVec.IsCodirectionalTo(firstVec.Negate(), new Tolerance(1.0, 1.0)))
                {
                    return first.EndPoint;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                Point3d pt = center.EndPoint;
                if (centerVec.IsCodirectionalTo(firstVec, new Tolerance(1.0, 1.0)))
                {
                    return first.EndPoint;
                }
                else if (centerVec.IsCodirectionalTo(firstVec.Negate(), new Tolerance(1.0, 1.0)))
                {
                    return first.StartPoint;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
