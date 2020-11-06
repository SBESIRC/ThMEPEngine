using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Assistant;
using ThMEPElectrical.Geometry;
using ThMEPElectrical.Model;

namespace ThMEPElectrical.PostProcess.MainSecondBeamAdjustor
{
    /// <summary>
    /// 点与移动方向的映射关系
    /// </summary>
    public class QuadrantNode
    {
        /// <summary>
        /// 原始的插入点
        /// </summary>
        public MediumNode MediumNode
        {
            get;
            set;
        }

        public List<Point3d> FirstQuadrant
        {
            get;
            set;
        } = new List<Point3d>();

        public List<Point3d> SecondQuadrant
        {
            get;
            set;
        } = new List<Point3d>();


        public List<Point3d> ThirdQuadrant
        {
            get;
            set;
        } = new List<Point3d>();

        public List<Point3d> FourthQuadrant
        {
            get;
            set;
        } = new List<Point3d>();

        public QuadrantNode(MediumNode mediumNode)
        {
            MediumNode = mediumNode;
        }
    }

    /// <summary>
    /// 最近点和点集的映射关系
    /// </summary>
    public class PointNode
    {
        // 最近点
        public Point3d NearestPt
        {
            get;
            set;
        }

        // 比较近的点
        public List<Point3d> NearestPts
        {
            get;
            set;
        }

        public PointNode(Point3d nearestPt, List<Point3d> nearestPts)
        {
            NearestPt = nearestPt;
            NearestPts = nearestPts;
        }
    }

    public abstract class PointMoveAdjustor
    {
        protected MainSecondBeamRegion m_mainSecondBeamRegion; // 梁跨区域信息
                                                               
        protected List<MediumNode> m_mediumNodes = new List<MediumNode>(); // 原始插入点的编号信息

        /// <summary>
        /// 处理后的插入点信息
        /// </summary>
        public List<Point3d> PostPoints
        {
            get;
            set;
        } = new List<Point3d>();

        public PointMoveAdjustor(MainSecondBeamRegion beamSpanInfo)
        {
            m_mainSecondBeamRegion = beamSpanInfo;
        }

        public void Do()
        {
            // 有效的布置区域
            var polylines = m_mainSecondBeamRegion.ValidRegions;

            // 定义布置点
            DefinePosPointsInfo();

            // 点位置调整
            PointMove(polylines);
        }

        protected bool IsValidPoint(List<Polyline> srcPolys, Point3d srcPt)
        {
            foreach (var singlePoly in srcPolys)
            {
                if (GeomUtils.PtInLoop(singlePoly, srcPt))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 计算最近的有效点
        /// </summary>
        /// <param name="srcPolys"></param>
        /// <param name="srcPt"></param>
        /// <returns></returns>
        protected Point3d CalculateClosetPoint(List<Polyline> srcPolys, Point3d srcPt)
        {
            var closestPts = new List<Point3d>();

            foreach (var singlePoly in srcPolys)
            {
                closestPts.Add(singlePoly.GetClosestPointTo(srcPt, false));
            }

            closestPts.Sort((pt1, pt2) =>
            {
                return pt1.DistanceTo(srcPt).CompareTo(pt2.DistanceTo(srcPt));
            }
            );

            return closestPts.First();
        }

        /// <summary>
        /// 计算距离比较近的移动的几个点
        /// </summary>
        /// <param name="srcPolys"></param>
        /// <param name="srcPt"></param>
        /// <returns></returns>
        protected PointNode CalculateClosetPoints(List<Polyline> srcPolys, Point3d srcPt)
        {
            var closestPts = new List<Point3d>();

            foreach (var singlePoly in srcPolys)
            {
                closestPts.Add(singlePoly.GetClosestPointTo(srcPt, false));
            }

            closestPts.Sort((pt1, pt2) =>
            {
                return pt1.DistanceTo(srcPt).CompareTo(pt2.DistanceTo(srcPt));
            }
            );

            var nearestPt = closestPts.First();
            var nearestPts = closestPts.Where(pt => pt.DistanceTo(srcPt) < nearestPt.DistanceTo(srcPt) * 2).ToList();
            if (nearestPts.Count == 0)
                nearestPts.Add(nearestPt);
            return new PointNode(nearestPt, nearestPts);
        }

        /// <summary>
        /// 定义点的类型
        /// </summary>
        protected abstract void DefinePosPointsInfo();

        /// <summary>
        /// 逐个点进行调整
        /// </summary>
        /// <param name="validPolys"></param>
        protected void PointMove(List<Polyline> validPolys)
        {
            foreach (var singleNode in m_mediumNodes)
            {
                if (IsValidPoint(validPolys, singleNode.Point))
                {
                    PostPoints.Add(singleNode.Point);
                }
                else
                {
                    var movePt = PostMovePoint(validPolys, singleNode);
                    if (movePt != ThMEPCommon.NullPoint3d)
                        PostPoints.Add(movePt);
                }
            }
        }

        protected virtual Point3d PostMovePoint(List<Polyline> validPolys, MediumNode mediumNode)
        {
            var pt = mediumNode.Point;
            var pts = new List<Point3d>(); // 不同方向上的点集s
            var pointNode = CalculateClosetPoints(validPolys, pt);
            var closestPt = pointNode.NearestPt;
            pts.AddRange(pointNode.NearestPts);
            var constraintDis = pt.DistanceTo(closestPt) * 2;

            double disGap = 10e6;
            var lineEndPts = new List<Point3d>();
            // 水平向左
            var extendEndPt = pt - Vector3d.XAxis * disGap;
            lineEndPts.Add(extendEndPt);
            // 竖直向上
            extendEndPt = pt + Vector3d.YAxis * disGap;
            lineEndPts.Add(extendEndPt);

            // 水平向右
            extendEndPt = pt + Vector3d.XAxis * disGap;
            lineEndPts.Add(extendEndPt);
            // 竖直向下
            extendEndPt = pt - Vector3d.YAxis * disGap;
            lineEndPts.Add(extendEndPt);

            // 计算不同方向上的距离和点集关系
            foreach (var endPt in lineEndPts)
            {
                var endLine = new Line(pt, endPt);
                var dirClosestPt = CalculateDirectionClosestPoint(endLine, validPolys, pt);
                if (dirClosestPt.HasValue && !pts.Contains(dirClosestPt.Value) && pt.DistanceTo(dirClosestPt.Value) < constraintDis)
                {
                    pts.Add(dirClosestPt.Value);
                }
            }

            if (pts.Count == 1)
                return pts.First();

            // 定义点集所在的象限
            var quadrantNode = DefineQuadrantInfo(pts, mediumNode);

            return SelectPoint(quadrantNode);
        }

        /// <summary>
        /// 计算沿着某一个方向的最近点，没有则返回空
        /// </summary>
        /// <param name="firCurve"></param>
        /// <param name="secCurve"></param>
        /// <returns></returns>
        protected Point3d? CalculateDirectionClosestPoint(Curve firCurve, List<Polyline> validPolys, Point3d srcPt)
        {
            var resPts = new List<Point3d>();

            foreach (var poly in validPolys)
            {
                var pts = GeomUtils.CurveIntersectCurve(firCurve, poly);
                if (pts.Count != 0)
                {
                    resPts.AddRange(pts);
                }
            }

            if (resPts.Count == 0)
            {
                return null;
            }
            else if (resPts.Count == 1)
            {
                return resPts.First();
            }
            else
            {
                resPts.Sort((p1, p2)
                    =>
                {
                    return p1.DistanceTo(srcPt).CompareTo(p2.DistanceTo(srcPt));
                });

                return resPts.First();
            }
        }

        /// <summary>
        /// 确定有效的点，点的优先级选择关系
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="mediumNode"></param>
        /// <returns></returns>
        protected virtual Point3d SelectPoint(QuadrantNode quadrantNode)
        {
            // 选择
            var mediumNode = quadrantNode.MediumNode;
            if (mediumNode.PointType == PointPosType.LeftTopPoint)
            {
                // 1号点
                // 第二象限
                if (quadrantNode.SecondQuadrant.Count > 0)
                    return quadrantNode.SecondQuadrant.Last();

                // 第三象限
                if (quadrantNode.ThirdQuadrant.Count > 0)
                {
                    return quadrantNode.ThirdQuadrant.First();
                }

                // 第一象限
                if (quadrantNode.FirstQuadrant.Count > 0)
                {
                    return quadrantNode.FirstQuadrant.Last();
                }

                // 第四象限
                if (quadrantNode.FourthQuadrant.Count > 0)
                {
                    return quadrantNode.FourthQuadrant.First();
                }
            }
            else if (mediumNode.PointType == PointPosType.RightTopPoint)
            {
                // 2号点
                // 第四象限
                if (quadrantNode.FourthQuadrant.Count > 0)
                {
                    return quadrantNode.FourthQuadrant.Last();
                }

                // 第一象限
                if (quadrantNode.FirstQuadrant.Count > 0)
                {
                    return quadrantNode.FirstQuadrant.First();
                }

                //第三象限
                if (quadrantNode.ThirdQuadrant.Count > 0)
                {
                    return quadrantNode.ThirdQuadrant.Last();
                }

                //第二象限
                if (quadrantNode.SecondQuadrant.Count > 0)
                {
                    return quadrantNode.SecondQuadrant.Last();
                }
            }

            return ThMEPCommon.NullPoint3d;
        }
        
        /// <summary>
        /// 象限定义
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="mediumNode"></param>
        /// <returns></returns>
        protected QuadrantNode DefineQuadrantInfo(List<Point3d> pts, MediumNode mediumNode)
        {
            var insertPt = mediumNode.Point;
            var plane = new Plane(insertPt, Vector3d.ZAxis);
            var quadrantNode = new QuadrantNode(mediumNode);
            foreach (var pt in pts)
            {
                var vec = (pt - insertPt).GetNormal();
                var rad = vec.AngleOnPlane(plane);

                var angle = GeomUtils.Rad2Angle(rad);
                if (angle > 0 && angle < 90)
                {
                    quadrantNode.FirstQuadrant.Add(pt);
                    quadrantNode.FirstQuadrant.Sort((p1, p2) =>
                    {
                        var vec1 = (p1 - insertPt).GetNormal();
                        var rad1 = vec1.AngleOnPlane(plane);
                        var vec2 = (p2 - insertPt).GetNormal();
                        var rad2 = vec2.AngleOnPlane(plane);
                        return rad1.CompareTo(rad2);
                    });
                }
                else if (angle >= 90 && angle <= 180)
                {
                    quadrantNode.SecondQuadrant.Add(pt);
                    quadrantNode.SecondQuadrant.Sort((p1, p2) =>
                    {
                        var vec1 = (p1 - insertPt).GetNormal();
                        var rad1 = vec1.AngleOnPlane(plane);
                        var vec2 = (p2 - insertPt).GetNormal();
                        var rad2 = vec2.AngleOnPlane(plane);
                        return rad1.CompareTo(rad2);
                    });
                }
                else if (angle > 180 && angle < 270)
                {
                    quadrantNode.ThirdQuadrant.Add(pt);
                    quadrantNode.ThirdQuadrant.Sort((p1, p2) =>
                    {
                        var vec1 = (p1 - insertPt).GetNormal();
                        var rad1 = vec1.AngleOnPlane(plane);
                        var vec2 = (p2 - insertPt).GetNormal();
                        var rad2 = vec2.AngleOnPlane(plane);
                        return rad1.CompareTo(rad2);
                    });
                }
                else
                {
                    quadrantNode.FourthQuadrant.Add(pt);
                    quadrantNode.FourthQuadrant.Sort((p1, p2) =>
                    {
                        var vec1 = (p1 - insertPt).GetNormal();
                        var rad1 = vec1.AngleOnPlane(plane);
                        var vec2 = (p2 - insertPt).GetNormal();
                        var rad2 = vec2.AngleOnPlane(plane);
                        return rad1.CompareTo(rad2);
                    });
                }
            }

            return quadrantNode;
        }
    }
}
