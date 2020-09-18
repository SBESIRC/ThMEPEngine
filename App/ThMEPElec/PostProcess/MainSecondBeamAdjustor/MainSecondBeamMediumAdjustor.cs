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
    /// 主次梁两个水平调整
    /// </summary>
    class MainSecondBeamMediumAdjustor : PointMoveAdjustor
    {
        // 原始点集
        private List<Point3d> m_srcPts;

        /// <summary>
        /// 两个插入点的位置调整
        /// </summary>
        /// <param name="beamSpanInfo"></param>
        /// <returns></returns>
        public static List<Point3d> MakeMainSecondBeamMediumAdjustor(MainSecondBeamRegion beamSpanInfo)
        {
            var firPt = beamSpanInfo.PlacePoints[0];
            var secPt = beamSpanInfo.PlacePoints[1];
            var deltaX = Math.Abs(firPt.X - secPt.X);
            var deltaY = Math.Abs(firPt.Y - secPt.Y);

            PointMoveAdjustor mediumAdjustor = new MainSecondBeamMediumAdjustor(beamSpanInfo);
            if (deltaX < deltaY)
                mediumAdjustor = new MainSecondBeamMediumVerticalAdjustor(beamSpanInfo);

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
        protected override void DefinePosPointsInfo()
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
    }

    /// <summary>
    /// 垂直两个点移动调整
    /// </summary>
    internal class MainSecondBeamMediumVerticalAdjustor : PointMoveAdjustor
    {
        // 原始点集
        private List<Point3d> m_srcPts;

        public MainSecondBeamMediumVerticalAdjustor(MainSecondBeamRegion beamSpanInfo)
            : base(beamSpanInfo)
        {
            m_srcPts = beamSpanInfo.PlacePoints;
        }

        protected override void DefinePosPointsInfo()
        {
            var firstPt = m_srcPts[0];
            var secPt = m_srcPts[1];

            if (firstPt.Y > secPt.Y)
            {
                m_mediumNodes.Add(new MediumNode(firstPt, PointPosType.LeftTopPoint));
                m_mediumNodes.Add(new MediumNode(secPt, PointPosType.RightBottomPoint));
            }
            else
            {
                m_mediumNodes.Add(new MediumNode(secPt, PointPosType.LeftTopPoint));
                m_mediumNodes.Add(new MediumNode(firstPt, PointPosType.RightBottomPoint));
            }
        }

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
