using System;
using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    /// <summary>
    /// 分析1号线直链段可以进行布点的线段
    /// 在直线范围内已经布灯的分支范围扣除掉
    /// </summary>
    public class ThDoubleRowDistributeExService
    {
        private Point3d StartPt { get; set; }
        private Point3d EndPt { get; set; }
        private ThLightArrangeParameter ArrangeParameter { get; set; }
        private List<ThLightEdge> DistributedEdges { get; set; }
        private List<ThLightEdge> GraphEdges { get; set; }

        private List<Tuple<Point3d, Point3d>> SplitPoints { get; set; }
        private ThWireOffsetDataService WireOffsetDataService { get; set; }

        /// <summary>
        /// 1号线被分割后产生的线，这些与LightEdges中的边是一致的
        /// 放在这儿是为了查询2号线，用于计算拐弯或T型，不需要布灯的长度
        /// </summary>
        public Dictionary<Line, List<Line>> OuterSplitDic { get; set; }

        public ThDoubleRowDistributeExService(
            Tuple<Point3d,Point3d> linePorts, 
            ThLightArrangeParameter arrangeParameter,
            List<ThLightEdge>  graphEdges,
            List<ThLightEdge> distributedEdges,
            ThWireOffsetDataService wireOffsetDataService)
        {
            StartPt = linePorts.Item1;
            EndPt = linePorts.Item2;
            ArrangeParameter = arrangeParameter;
            DistributedEdges = distributedEdges;
            GraphEdges = graphEdges;
            WireOffsetDataService = wireOffsetDataService;
        }
        public List<Tuple<Point3d, Point3d>> Distribute()
        {
            //找出当前直链上的分支
            var branchLines = FindBranchLines();
            //找出哪些分支已经布灯
            var branchEdges = DistributedEdges
                .Where(o => branchLines.IsContains(o.Edge))
                .Where(o => o.LightNodes.Count > 0 || IsNeibourEdgeDistributed(o))
                .Where(o=>o.IsDX)
                .ToList();

            //找出已布灯边对应的原始（未分割前）的1号线的边
            var branchOriginFirstLines = new List<Line>();
            branchEdges.ForEach(o =>
            {
                var midPt = o.Edge.StartPoint.GetMidPt(o.Edge.EndPoint);
                var orignFirst=WireOffsetDataService.FindFirstByPt(midPt);
                branchOriginFirstLines.Add(orignFirst);
            });
            //找出(未分割前）的1号边对应说的2号边
            var firstSecondParis = new List<Tuple<Line, Line>>();
            branchOriginFirstLines.ForEach(o =>
            {
                var second = WireOffsetDataService.FindSecondByFirst(o);
                firstSecondParis.Add(Tuple.Create(o, second));
            });

            //获取分支点在此直段上的交点
            var splitPoints = GetOccupiedSection(firstSecondParis);

            //获取可以布点的区域
            SplitPoints = ObtainArrangedSegments(splitPoints);
            return SplitPoints;
        }
        /// <summary>
        /// 如果分支上有短线未布灯，但是其相邻边布灯，也视为布灯
        /// </summary>
        /// <param name="lightEdge"></param>
        /// <returns></returns>
        private bool IsNeibourEdgeDistributed(ThLightEdge lightEdge)
        {
            return DistributedEdges
                    .Where(o => o.Edge.IsLink(lightEdge.Edge.StartPoint) ||
                    o.Edge.IsLink(lightEdge.Edge.EndPoint))
                    .Where(o => ThGeometryTool.IsCollinearEx(
                        lightEdge.Edge.StartPoint, lightEdge.Edge.EndPoint,
                        o.Edge.StartPoint, o.Edge.EndPoint))
                    .Where(o => o.LightNodes.Count > 0).Any();
        }
        private List<Tuple<Point3d, Point3d>> ObtainArrangedSegments(List<Tuple<Point3d, Point3d>> occupiedSections)
        {
            //occupiedSections 已经在”GetOccupiedSection“排序
            var results = new List<Tuple<Point3d, Point3d>>();
            if(occupiedSections.Count==0)
            {
                results.Add(Tuple.Create(StartPt, EndPt));
            }
            else
            {
                occupiedSections = Merge(occupiedSections);
                Point3d basePt = StartPt;
                foreach(var segment in occupiedSections)
                {
                    if (ThGeometryTool.IsPointInLine(segment.Item1, segment.Item2, basePt,-1.0))
                    {
                        basePt = GetSplitPt(segment,true);
                        continue;
                    }
                    else
                    {
                        var prePt = GetSplitPt(segment, false);
                        results.Add(Tuple.Create(basePt, prePt));
                        basePt = GetSplitPt(segment, true);
                    }
                }
                if(ThGeometryTool.IsPointInLine(StartPt, EndPt, basePt,-1.0) &&
                    basePt.DistanceTo(EndPt)>ThGarageLightCommon.RepeatedPointDistance)
                {
                    results.Add(Tuple.Create(basePt, EndPt));
                }
            }
            return results.Where(o=>o.Item1.DistanceTo(o.Item2)>0.0).ToList();
        }
        private Point3d GetSplitPt(Tuple<Point3d, Point3d> segment,bool isNext)
        {
            var segSp = segment.Item1;
            var segEp = segment.Item2;
            var segVec = segment.Item1.GetVectorTo(segment.Item2);
            var lineVec = StartPt.GetVectorTo(EndPt);
            if (!segVec.IsCodirectionalTo(lineVec))
            {
                segSp = segment.Item2;
                segEp = segment.Item1;
            }
            return isNext?segEp: segSp;
        }
        
        private List<Tuple<Point3d,Point3d>> Merge(List<Tuple<Point3d, Point3d>> originSplitPts)
        {
            var mergeSplitPts = new List<Tuple<Point3d, Point3d>>();
            for(int i=0;i< originSplitPts.Count;i++)
            {
                var firstSegment = originSplitPts[i];
                int j = i + 1;
                for (; j < originSplitPts.Count; j++)
                {
                    var secondSegment = originSplitPts[j];
                    if (ThGeometryTool.IsOverlap(firstSegment.Item1,
                        firstSegment.Item2, secondSegment.Item1, secondSegment.Item2))
                    {
                        var pts = new List<Point3d> {
                            firstSegment.Item1,
                            firstSegment.Item2, 
                            secondSegment.Item1,
                            secondSegment.Item2};
                        firstSegment = pts.GetCollinearMaxPts();
                    }
                    else
                    {
                        break;
                    }
                }
                mergeSplitPts.Add(firstSegment);
                i = j - 1;
            }
            return mergeSplitPts;
        }
        
        private List<Tuple<Point3d,Point3d>> GetOccupiedSection(List<Tuple<Line, Line>> branchPairs)
        {
            var splitPoints = new List<Tuple<Point3d, Point3d>>();
            var main = new Line(StartPt, EndPt);
            branchPairs.ForEach(o =>
            {
                if (CheckBranchIsValid(main, o.Item1, o.Item2))
                {
                    var firstInters = ThGeometryTool.IntersectPts(main, o.Item1, Intersect.ExtendBoth,5.0);
                    var secondInters = ThGeometryTool.IntersectPts(main, o.Item2, Intersect.ExtendBoth,5.0);
                    splitPoints.Add(Tuple.Create(firstInters[0], secondInters[0]));
                }
            });
            return splitPoints
                .OrderBy(o=> ProjectionDis(ThGeometryTool.GetMidPt(o.Item1,o.Item2)))
                .ToList();
        }
        private double ProjectionDis(Point3d pt)
        {
            var vec = StartPt.GetVectorTo(EndPt);
            var plane = new Plane(StartPt, vec.GetNormal());
            var mt = Matrix3d.WorldToPlane(plane);
            var newPt=pt.TransformBy(mt);
            return newPt.Z;
        }
        private bool CheckBranchIsValid(Line main,Line branchFirst,Line branchSecond)
        {
            var firstInters = ThGeometryTool.IntersectPts(main, branchFirst, Intersect.ExtendBoth);
            var secondInters = ThGeometryTool.IntersectPts(main, branchSecond, Intersect.ExtendBoth);
            if(firstInters.Count > 0 && secondInters.Count > 0)
            {
                var firstClosePt = firstInters[0].DistanceTo(branchFirst.StartPoint) <
                    firstInters[0].DistanceTo(branchFirst.EndPoint) ? branchFirst.StartPoint : branchFirst.EndPoint;
                var secondClosePt = secondInters[0].DistanceTo(branchSecond.StartPoint) <
                    secondInters[0].DistanceTo(branchSecond.EndPoint) ? branchSecond.StartPoint : branchSecond.EndPoint;

                if(firstClosePt.DistanceTo(firstInters[0])<=(ArrangeParameter.Interval+1) &&
                    secondClosePt.DistanceTo(secondInters[0]) <= (ArrangeParameter.Interval + 1))
                {
                    bool isPtOn = ThGeometryTool.IsPointOnLine(StartPt, EndPt, firstInters[0], 1.0) ||
                 ThGeometryTool.IsPointOnLine(StartPt, EndPt, secondInters[0], 1.0);
                    if (isPtOn && CheckDisValid(main, branchFirst, firstInters[0].DistanceTo(secondInters[0])))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
        }      
        private bool CheckDisValid(Line first ,Line second,double dis)
        {
            var firstVec = first.StartPoint.GetVectorTo(first.EndPoint).GetNormal();
            var secondVec = second.StartPoint.GetVectorTo(second.EndPoint).GetNormal();
            double rad = firstVec.GetAngleTo(secondVec);
            if(rad>Math.PI)
            {
                rad -= Math.PI;
            }
            double verDis = Math.Sin(rad) * dis;
            return Math.Abs(ArrangeParameter.RacywaySpace- verDis)<=5.0;
        }
        private List<Line> FindBranchLines()
        {
            var queryInstance = ThQueryLineService.Create(GraphEdges.Select(o=>o.Edge).ToList());
            return queryInstance.QueryUnparallellines(
                StartPt, EndPt, ThGarageLightCommon.RepeatedPointDistance);
        }
    } 
}
