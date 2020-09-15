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

    public abstract class PointMoveAdjustor
    {
        protected MainSecondBeamRegion m_mainSecondBeamRegion; // 梁跨区域信息

        /// <summary>
        /// 处理后的插入点信息
        /// </summary>
        protected List<Point3d> PostPoints
        {
            get;
            set;
        } = new List<Point3d>();

        public PointMoveAdjustor(MainSecondBeamRegion beamSpanInfo)
        {
            m_mainSecondBeamRegion = beamSpanInfo;
        }

        public abstract void Do();

        protected bool IsValidPoint(List<Polyline> srcPolys, Point3d srcPt)
        {
            foreach (var singlePoly in srcPolys)
            {
                if (GeomUtils.PtInLoop(singlePoly, srcPt.Point2D()))
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
    }
}
