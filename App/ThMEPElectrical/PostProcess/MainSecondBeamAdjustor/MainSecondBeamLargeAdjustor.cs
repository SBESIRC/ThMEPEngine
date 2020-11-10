using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;

namespace ThMEPElectrical.PostProcess.MainSecondBeamAdjustor
{
    /// <summary>
    /// 矩形的四个点移动状况
    /// </summary>
    public class MainSecondBeamLargeAdjustor : PointMoveAdjustor
    {

        public MainSecondBeamLargeAdjustor(MainSecondBeamRegion beamSpanInfo)
            : base(beamSpanInfo)
        {
        }

        public static List<Point3d> MakeMainSecondBeamMediumAdjustor(MainSecondBeamRegion beamSpanInfo)
        {
            var largeAdjustor = new MainSecondBeamLargeAdjustor(beamSpanInfo);
            largeAdjustor.Do();
            return largeAdjustor.PostPoints;
        }

        /// <summary>
        /// 插入点定义
        /// </summary>
        protected override void DefinePosPointsInfo()
        {
            var srcPts = m_mainSecondBeamRegion.PlacePoints;
            var firstPt = srcPts[0];
            var secPt = srcPts[1];
            var thirdPt = srcPts[2];
            var fourthPt = srcPts[3];

            var xLst = srcPts.Select(e => e.X).ToList();
            var yLst = srcPts.Select(e => e.Y).ToList();

            var xMin = xLst.Min();
            var yMin = yLst.Min();

            var xMax = xLst.Max();
            var yMax = yLst.Max();
            var rectLeftBottomPt = new Point3d(xMin, yMin, 0);
            var rectRightTopPt = new Point3d(xMax, yMax, 0);
            var rectRightBottomPt = new Point3d(xMax, yMin, 0);
            var rectLeftTopPt = new Point3d(xMin, yMax, 0);

            // 1号点
            var leftTopPt = FindNearestPt(srcPts, rectLeftTopPt);
            m_mediumNodes.Add(new MediumNode(leftTopPt, PointPosType.LeftTopPoint));

            // 2号点
            var rightTopPt = FindNearestPt(srcPts, rectRightTopPt);
            m_mediumNodes.Add(new MediumNode(rightTopPt, PointPosType.RightTopPoint));

            // 3号点
            var leftBottomPt = FindNearestPt(srcPts, rectLeftBottomPt);
            m_mediumNodes.Add(new MediumNode(leftBottomPt, PointPosType.LeftBottomPoint));

            // 4号点
            var rightBottomPt = FindNearestPt(srcPts, rectRightBottomPt);
            m_mediumNodes.Add(new MediumNode(rightBottomPt, PointPosType.RightBottomPoint));
        }

        /// <summary>
        /// 寻找最近点
        /// </summary>
        /// <param name="srcPts"></param>
        /// <param name="pt"></param>
        /// <returns></returns>
        private Point3d FindNearestPt(List<Point3d> srcPts, Point3d pt)
        {
            var distance = pt.DistanceTo(srcPts.First());
            var aimPt = srcPts.First();
            for (int i = 1; i < srcPts.Count; i++)
            {
                var curDis = srcPts[i].DistanceTo(pt);
                if (curDis < distance)
                {
                    distance = curDis;
                    aimPt = srcPts[i];
                }
            }

            return aimPt;
        }

        /// <summary>
        /// 四个点的移动方向优先级关系
        /// </summary>
        /// <param name="quadrantNode"></param>
        /// <returns></returns>
        protected override Point3d SelectPoint(QuadrantNode quadrantNode)
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
                // 第一象限
                if (quadrantNode.FirstQuadrant.Count > 0)
                {
                    return quadrantNode.FirstQuadrant.First();
                }

                // 第四象限
                if (quadrantNode.FourthQuadrant.Count > 0)
                {
                    return quadrantNode.FourthQuadrant.Last();
                }

                //第三象限
                if (quadrantNode.ThirdQuadrant.Count > 0)
                {
                    return quadrantNode.ThirdQuadrant.Last();
                }

                //第二象限
                if (quadrantNode.SecondQuadrant.Count > 0)
                {
                    return quadrantNode.SecondQuadrant.First();
                }
            }
            else if (mediumNode.PointType == PointPosType.LeftBottomPoint)
            {
                // 3号点
                //第三象限
                if (quadrantNode.ThirdQuadrant.Count > 0)
                {
                    return quadrantNode.ThirdQuadrant.First();
                }

                //第二象限
                if (quadrantNode.SecondQuadrant.Count > 0)
                {
                    return quadrantNode.SecondQuadrant.Last();
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
            else if (mediumNode.PointType == PointPosType.RightBottomPoint)
            {
                // 4号点
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
    }
}
