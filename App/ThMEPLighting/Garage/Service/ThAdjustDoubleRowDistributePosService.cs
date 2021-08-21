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
    public class ThAdjustDoubleRowDistributePosService: ThAdjustLightDistributePosService
    {
        private ThWireOffsetDataService WireOffsetDataService { get; set; }

        public ThAdjustDoubleRowDistributePosService(
            Tuple<Point3d,Point3d> linePorts, 
            ThLightArrangeParameter arrangeParameter,
            List<ThLightEdge>  graphEdges,
            List<ThLightEdge> distributedEdges,
            ThWireOffsetDataService wireOffsetDataService):base(linePorts, arrangeParameter, graphEdges, distributedEdges)
        {
            WireOffsetDataService = wireOffsetDataService;
        }
        public override List<Tuple<Point3d, Point3d>> Distribute()
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
                var seconds = WireOffsetDataService.FindSecondByFirst(o);
                if (seconds.Count > 0)
                {
                    var result = seconds.GetCollinearMaxPts();
                    firstSecondParis.Add(Tuple.Create(o, new Line(result.Item1,result.Item2)));
                }
            });

            //获取分支点在此直段上的交点
            var splitPoints = GetOccupiedSection(firstSecondParis);

            //获取可以布点的区域
            return ObtainArrangedSegments(splitPoints);
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
        private bool CheckBranchIsValid(Line main,Line branchFirst,Line branchSecond)
        {
            var firstInters = ThGeometryTool.IntersectPts(main, branchFirst, Intersect.ExtendBoth);
            var secondInters = ThGeometryTool.IntersectPts(main, branchSecond, Intersect.ExtendBoth);
            if(firstInters.Count > 0 && secondInters.Count > 0)
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
    } 
}
