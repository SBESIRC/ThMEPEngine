using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;
using ThMEPElectrical.Geometry;
using ThMEPElectrical.Assistant;

namespace ThMEPElectrical.PostProcess.MainSecondBeamAdjustor
{
    /// <summary>
    /// 主次梁单个调整器
    /// </summary>
    public class MainSecondBeamSingleAdjustor
    {
        private MainSecondBeamRegion m_mainSecondBeamRegion;

        public List<Point3d> PlacePoints
        {
            get;
            private set;
        } = new List<Point3d>();

        public MainSecondBeamSingleAdjustor(MainSecondBeamRegion beamSpanInfo)
        {
            m_mainSecondBeamRegion = beamSpanInfo;
        }

        /// <summary>
        /// 单个调整入口
        /// </summary>
        /// <param name="beamSpanInfo"></param>
        /// <param name="placeAdjustorType"></param>
        /// <returns></returns>
        public static List<Point3d> MakeSecondBeamSingleAdjustor(MainSecondBeamRegion beamSpanInfo)
        {
            var singleAdjustor = new MainSecondBeamSingleAdjustor(beamSpanInfo);
            singleAdjustor.Do();
            return singleAdjustor.PlacePoints;
        }

        public void Do()
        {
            // 有效区域
            var polylines = m_mainSecondBeamRegion.ValidRegions;

            // 原始的布置点
            var srcPt = m_mainSecondBeamRegion.PlacePoints.First();

            if (IsValidPoint(polylines, srcPt))
            {
                PlacePoints.Add(srcPt);
            }
            else
            {
                // 选择最近的点
                PlacePoints.Add(CalculateClosetPoint(polylines, srcPt));
            }
        }

        /// <summary>
        /// 计算最近的有效点
        /// </summary>
        /// <param name="srcPolys"></param>
        /// <param name="srcPt"></param>
        /// <returns></returns>
        private Point3d CalculateClosetPoint(List<Polyline> srcPolys, Point3d srcPt)
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

        private bool IsValidPoint(List<Polyline> srcPolys, Point3d srcPt)
        {
            foreach (var singlePoly in srcPolys)
            {
                if (GeomUtils.PtInLoop(singlePoly, srcPt.Point2D()))
                    return true;
            }

            return false;
        }
    }



}
