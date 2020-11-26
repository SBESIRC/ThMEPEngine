using System;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Algorithm
{
    public class ThPolygonSplitPointAnalysis
    {
        public Tuple<LineSegment3d,Point3d, Point3d> ShellSegmentSplitPts { get; private set; }
        public Tuple<LineSegment3d, Point3d, Point3d> HoleSegmentSplitPts { get; private set; }
        public bool IsFind { get; private set; }
        private ThPolygonSplitParameter SplitParameter { get; set; } 
        private List<LineSegment3d> HoleLineSegments { get; set; }
        private List<LineSegment3d> ShellLineSegments { get; set; }
        private ThPolygonSplitPointAnalysis(ThPolygonSplitParameter splitParameter)
        {
            SplitParameter = splitParameter;
            ShellLineSegments = GetLineSegments(SplitParameter.Shell);
            HoleLineSegments = GetLineSegments(SplitParameter.Hole);
        }
        public static ThPolygonSplitPointAnalysis Split(ThPolygonSplitParameter splitParameter)
        {
            var instance = new ThPolygonSplitPointAnalysis(splitParameter);
            instance.Split();
            return instance;
        }
        private void Split()
        {
            if(!SplitParameter.Valid)
            {
                return;
            }
            HoleLineSegments = HoleLineSegments.OrderBy(o =>
              SplitParameter.Shell.Distance(ThGeometryTool.GetMidPt(o.StartPoint, o.EndPoint))).ToList();
            foreach (var lineSegment in HoleLineSegments)
            {
                IsFind = FindValidSplitPoints(lineSegment);
                if (IsFind)
                {
                    break;
                }
            }
        }
        private bool FindValidSplitPoints(LineSegment3d lineSegment)
        {
            bool result = false;
            Vector3d vec = lineSegment.Direction.GetNormal();
            Point3d startSlitPt = lineSegment.StartPoint+vec.MultiplyBy(SplitParameter.Step);
            double disToStartPoint = startSlitPt.DistanceTo(lineSegment.StartPoint);
            while (disToStartPoint < lineSegment.Length)
            {
                var splitPts = BuildOffsetPoints(lineSegment, disToStartPoint);
                var possiblePts = IsPossibleSplitPoint(splitPts.Item1, splitPts.Item2);
                if(!possiblePts.Item1)
                {
                    startSlitPt = startSlitPt + vec.MultiplyBy(SplitParameter.Step);
                    disToStartPoint= startSlitPt.DistanceTo(lineSegment.StartPoint);
                    continue;
                }
                var closestEdge = GetClosestEdge(splitPts.Item1, splitPts.Item2, possiblePts.Item2);
                if(closestEdge==null)
                {
                    startSlitPt = startSlitPt + vec.MultiplyBy(SplitParameter.Step);
                    disToStartPoint = startSlitPt.DistanceTo(lineSegment.StartPoint);
                    continue;
                }
                bool isExact = IsExactSplitEdge(closestEdge.Item1, splitPts.Item1, closestEdge.Item2,
                    splitPts.Item2, closestEdge.Item3);
                if(isExact)
                {
                    ShellSegmentSplitPts = closestEdge;
                    HoleSegmentSplitPts = Tuple.Create(lineSegment, splitPts.Item1, splitPts.Item2);
                    result = true;
                    break;
                }
                startSlitPt = startSlitPt + vec.MultiplyBy(SplitParameter.Step);
                disToStartPoint = startSlitPt.DistanceTo(lineSegment.StartPoint);
            }
            return result;
        }
        private bool IsExactSplitEdge(LineSegment3d closeEdge,Point3d splitSp, 
            Point3d splitSpExtendPt, Point3d splitEp, Point3d splitEpExtendPt)
        {
            var spSegments = IntersectWithShell(splitSp, splitSp.GetVectorTo(splitSpExtendPt));
            var epSegments = IntersectWithShell(splitEp, splitEp.GetVectorTo(splitEpExtendPt));
            var spIntersectSegments = spSegments.Select(o => o.Item1).ToList();
            var epIntersectSegments = epSegments.Select(o => o.Item1).ToList();
            spIntersectSegments.Remove(closeEdge);
            epIntersectSegments.Remove(closeEdge);
            return spIntersectSegments.Count == 0 && epIntersectSegments.Count==0;
        }
        /// <summary>
        /// 获取Shell上可以分割的最近边
        /// </summary>
        /// <param name="splitSp">洞边上的分割起始点</param>
        /// <param name="splitEp">洞边上的分割终止点</param>
        /// <param name="rayVec">射线方向</param>
        /// <returns>最近边，splitSp对应的延长分割点,splitEp对应的延长分割点,</returns>
        private Tuple<LineSegment3d,Point3d,Point3d> GetClosestEdge(Point3d splitSp, Point3d splitEp, Vector3d rayVec)
        {
            var spInters = IntersectWithShell(splitSp, rayVec);
            if (spInters.Count==0)
            {
                return null;
            }
            var epInters = IntersectWithShell(splitEp, rayVec);
            if (epInters.Count == 0)
            {
                return null;
            }
            var spCloestEdge = spInters.OrderBy(o => splitSp.DistanceTo(o.Item2)).First();
            var epCloestEdge = epInters.OrderBy(o => splitEp.DistanceTo(o.Item2)).First();
            return spCloestEdge.Item1.IsEqualTo(epCloestEdge.Item1) ?
                Tuple.Create(spCloestEdge.Item1, spCloestEdge.Item2, epCloestEdge.Item2) : null;   
        }
        private List<Tuple<LineSegment3d, Point3d>> IntersectWithShell(Point3d sp, Vector3d rayVec)
        {
            var results = new List<Tuple<LineSegment3d, Point3d>>();
            Line rayLine = new Line(sp, sp + rayVec);
            ShellLineSegments.ForEach(o =>
            {
                Line line = new Line(o.StartPoint, o.EndPoint);
                Point3dCollection pts = new Point3dCollection();
                rayLine.IntersectWith(line, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
                if(pts.Count>0 && IsIn(o,pts[0]))
                {
                    results.Add(Tuple.Create(o, pts[0]));
                }
            });
            return results;
        }

        private Tuple<bool,Vector3d> IsPossibleSplitPoint(Point3d sp,Point3d ep)
        {
            Vector3d vec = sp.GetVectorTo(ep);
            Vector3d? spExtendVec = LineExtendVector(sp, vec, SplitParameter.RayLength);
            if(!spExtendVec.HasValue)
            {
                return Tuple.Create(false,Vector3d.XAxis);
            }
            Vector3d? epExtendVec = LineExtendVector(ep, vec, SplitParameter.RayLength);
            if (!epExtendVec.HasValue)
            {
                return Tuple.Create(false, Vector3d.XAxis);
            }
            if(spExtendVec.Value.IsCodirectionalTo(epExtendVec.Value))
            {
                return Tuple.Create(true, spExtendVec.Value);
            }
            return Tuple.Create(false, Vector3d.XAxis);
        }

        /// <summary>
        /// 获取线段上两个分割点
        /// </summary>
        /// <param name="lineSegment">线段</param>
        /// <param name="lengthFromSp">距离线段起点的长度</param>
        /// <returns></returns>
        private Tuple<Point3d,Point3d> BuildOffsetPoints(LineSegment3d lineSegment,double lengthFromSp)
        {
            List<Point3d> offsetPts = new List<Point3d>();
            Vector3d vec = lineSegment.Direction.GetNormal();
            Point3d splitSp= lineSegment.StartPoint+vec.MultiplyBy(lengthFromSp);
            Point3d splitEp = splitSp + vec.MultiplyBy(SplitParameter.OffsetDistance);
            if (IsIn(lineSegment, splitSp) && IsIn(lineSegment, splitEp))
            {
                return Tuple.Create(splitSp, splitEp);
            }
            else
            {
                return Tuple.Create(Point3d.Origin, Point3d.Origin);
            }
        }
        private bool IsIn(LineSegment3d lineSegment, Point3d pt, double tolerance = 1.0)
        {
            return ThGeometryTool.IsPointInLine(lineSegment.StartPoint, lineSegment.EndPoint, pt, tolerance);
        }
        private List<LineSegment3d> GetLineSegments(Polyline polyline)
        {
            var results = new List<LineSegment3d>();
            if (polyline == null || polyline.IsDisposed || polyline.IsErased)
            {
                return results;
            }
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                var segmentType = polyline.GetSegmentType(i);
                if(segmentType== SegmentType.Line)
                {
                    var lineSegment = polyline.GetLineSegmentAt(i);
                    results.Add(lineSegment);
                }
            }
            return results;
        }
        private Vector3d? LineExtendVector(Point3d pt, Vector3d ptOnLineVec,double extendLength= 1e6)
        {
            var perpendVec = ptOnLineVec.GetPerpendicularVector().GetNormal();
            Point3d extendPt1 = pt - perpendVec.MultiplyBy(extendLength);
            Point3d extendPt2 = pt + perpendVec.MultiplyBy(extendLength);
            if (IntersectCurrentHole(pt, extendPt1).Count == 1 &&
                IsIntersectOtherHoles(pt, extendPt1)==false)
            {
                return pt.GetVectorTo(extendPt1);
            }
            if (IntersectCurrentHole(pt, extendPt2).Count == 1 &&
                IsIntersectOtherHoles(pt, extendPt2) == false)
            {
                return pt.GetVectorTo(extendPt2);
            }
            return null;
        }
        private bool IsIntersectOtherHoles(Point3d sp, Point3d extendPt)
        {
            Point3dCollection totalIntersectPoints = new Point3dCollection();
            Line rayLine = new Line(sp, extendPt);
            SplitParameter.OtherHoles.ForEach(o =>
            {
                Point3dCollection intersectPts = new Point3dCollection();
                rayLine.IntersectWith(o, Intersect.OnBothOperands, intersectPts, IntPtr.Zero, IntPtr.Zero);
                intersectPts.Cast<Point3d>().ForEach(k => totalIntersectPoints.Add(k));
            });
            rayLine.Dispose();
            return totalIntersectPoints.Count > 0;
        }
        private Point3dCollection IntersectCurrentHole(Point3d basePt, Point3d extendPt)
        {
            Point3dCollection intersectPts = new Point3dCollection();
            Line extendLine = new Line(basePt, extendPt);
            extendLine.IntersectWith(SplitParameter.Hole, Intersect.OnBothOperands, intersectPts, IntPtr.Zero, IntPtr.Zero);
            return intersectPts;
        }
    }
    public class ThPolygonSplitParameter
    {
        public Polyline Shell { get; set; }
        public Polyline Hole { get; set; }
        /// <summary>
        /// 撕口的长度
        /// </summary>
        public double OffsetDistance { get; set; }
        /// <summary>
        /// Shell内其它的洞
        /// </summary>
        public List<Polyline> OtherHoles { get; set; }
        /// <summary>
        /// 在LineSegment上查找的的步进长度
        /// </summary>
        public double Step { get; set; } = 50.0;
        /// <summary>
        /// 撕口的点往外做射线的长度
        /// 用于验证撕口的点是否合适
        /// </summary>
        public double RayLength { get; set; } = 1e6;
        public bool Valid
        {
            get
            {
                return Check();
            }
        }
        private bool Check()
        {
            return
                Step > 0 &&
                RayLength > 0.0 &&
                OffsetDistance > 0.0 &&
                Shell.Contains(Hole);
        }
    }
}
