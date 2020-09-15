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
    /// 两点 点集定义
    /// </summary>
    public class MediumNode
    {
        public Point3d Point
        {
            get;
            set;
        }

        public PointPosType PointType
        {
            get;
            set;
        }

        public MediumNode(Point3d pt, PointPosType ptType)
        {
            Point = pt;
            PointType = ptType;
        }
    }

    /// <summary>
    /// 主次梁两个调整
    /// </summary>
    class MainSecondBeamMediumAdjustor : PointMoveAdjustor
    {
        // 原始点集
        private List<Point3d> m_srcPts;

        // 原始插入点的编号信息
        private List<MediumNode> m_mediumNodes = new List<MediumNode>();

        // 象限信息
        private List<QuadrantNode> m_quadrantNodes = new List<QuadrantNode>();

        /// <summary>
        /// 两个插入点的位置调整
        /// </summary>
        /// <param name="beamSpanInfo"></param>
        /// <returns></returns>
        public static List<Point3d> MakeMainSecondBeamMediumAdjustor(MainSecondBeamRegion beamSpanInfo)
        {
            var mediumAdjustor = new MainSecondBeamMediumAdjustor(beamSpanInfo);
            mediumAdjustor.Do();
            return mediumAdjustor.PostPoints;
        }

        public MainSecondBeamMediumAdjustor(MainSecondBeamRegion beamSpanInfo)
            : base(beamSpanInfo)
        {
            m_srcPts = m_mainSecondBeamRegion.PlacePoints;
        }



        /// <summary>
        /// 点集定义
        /// </summary>
        private void DefinePosPointsInfo()
        {
            var firstPt = m_srcPts[0];
            var secPt = m_srcPts[1];

            if (firstPt.X < secPt.X)
            {
                m_mediumNodes.Add(new MediumNode(firstPt, PointPosType.LeftTopPoint));
                m_mediumNodes.Add(new MediumNode(secPt, PointPosType.RightTopPoint));
            }
            else
            {
                m_mediumNodes.Add(new MediumNode(secPt, PointPosType.LeftTopPoint));
                m_mediumNodes.Add(new MediumNode(firstPt, PointPosType.RightTopPoint));
            }
        }

        public override void Do()
        {

            // 有效的布置区域
            var polylines = m_mainSecondBeamRegion.ValidRegions;

            // 定义布置点
            DefinePosPointsInfo();

            // 点位置调整
            PointMove(polylines);
        }

        private void PointMove(List<Polyline> validPolys)
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
                    if (movePt.HasValue)
                        PostPoints.Add(movePt.Value);
                }
            }
        }

        /// <summary>
        /// 移动后的点
        /// </summary>
        /// <param name="validPolys"></param>
        /// <param name="pt"></param>
        /// <returns></returns>
        private Point3d? PostMovePoint(List<Polyline> validPolys, MediumNode mediumNode)
        {
            var pt = mediumNode.Point;
            var pts = new List<Point3d>(); // 不同方向上的点集
            var closestPt = CalculateClosetPoint(validPolys, pt);
            pts.Add(closestPt);
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
        /// 确定有效的点
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="mediumNode"></param>
        /// <returns></returns>
        private Point3d? SelectPoint(QuadrantNode quadrantNode)
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


            return null;
        }

        private QuadrantNode DefineQuadrantInfo(List<Point3d> pts, MediumNode mediumNode)
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
