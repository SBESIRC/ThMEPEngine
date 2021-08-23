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
    public class ThAdjustSingleRowDistributePosService: ThAdjustLightDistributePosService
    {
        public ThAdjustSingleRowDistributePosService(
            Tuple<Point3d,Point3d> linePorts, 
            ThLightArrangeParameter arrangeParameter,
            List<ThLightEdge>  graphEdges,
            List<ThLightEdge> distributedEdges)
            :base(linePorts,arrangeParameter,graphEdges,distributedEdges)
        {
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

            //获取分支点在此直段上的交点
            var splitPoints = GetBranchIntersPt(branchEdges.Select(o=>o.Edge).ToList());
            var occupyPoints = GetOccupiedSection(splitPoints);

            //获取可以布点的区域
            return ObtainArrangedSegments(occupyPoints);
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
        /// <summary>
        /// 获取已布灯分支的交点
        /// </summary>
        /// <param name="branchLines"></param>
        /// <returns></returns>
        private List<Point3d> GetBranchIntersPt(List<Line> branchLines)
        {
            var splitPoints = new List<Point3d>();
            var main = new Line(StartPt, EndPt);
            branchLines.ForEach(o =>
            {
                var inters = ThGeometryTool.IntersectPts(main, o, Intersect.ExtendBoth, 5.0);
                if (inters.Count > 0)
                {
                    splitPoints.Add(inters[0]);
                }
            });
            return splitPoints.OrderBy(o=> ProjectionDis(o)).ToList();
        }
        private List<Tuple<Point3d, Point3d>> GetOccupiedSection(List<Point3d> distributedBranchPts)
        {
            var occupiedSections = new List<Tuple<Point3d, Point3d>>();
            var vec = StartPt.GetVectorTo(EndPt).GetNormal();            
            for (int i = 0; i < distributedBranchPts.Count; i++)
            {
                var rangePt1 = distributedBranchPts[i] - vec.MultiplyBy(ArrangeParameter.Interval / 2.0);
                var rangePt2 = distributedBranchPts[i] + vec.MultiplyBy(ArrangeParameter.Interval / 2.0);
                occupiedSections.Add(Tuple.Create(rangePt1, rangePt2));
            }
            return occupiedSections;
        }
    } 
}
