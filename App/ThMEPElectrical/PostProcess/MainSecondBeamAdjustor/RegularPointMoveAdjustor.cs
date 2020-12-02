using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Assistant;
using ThMEPElectrical.Model;

namespace ThMEPElectrical.PostProcess.MainSecondBeamAdjustor
{
    /// <summary>
    /// 通用移动逻辑， 一行或者一列， 或者多个的移动逻辑
    /// </summary>
    public class RegularPointMoveAdjustor : PointMoveAdjustor
    {
        public RegularPointMoveAdjustor(MainSecondBeamRegion beamSpanInfo)
            : base(beamSpanInfo)
        {
        }

        /// <summary>
        /// 通用调整入口
        /// </summary>
        /// <param name="beamSpanInfo"></param>
        /// <param name="placeAdjustorType"></param>
        /// <returns></returns>
        public static List<Point3d> MakeRegularPointMoveAdjustor(MainSecondBeamRegion beamSpanInfo)
        {
            var regularAdjustor = new RegularPointMoveAdjustor(beamSpanInfo);
            regularAdjustor.Do();
            return regularAdjustor.PostPoints;
        }

        protected override void DefinePosPointsInfo()
        {
            foreach (var singlePt in m_mainSecondBeamRegion.PlacePoints)
            {
                m_mediumNodes.Add(new MediumNode(singlePt, PointPosType.LeftTopPoint));
            }
        }

        /// <summary>
        /// 每个node点的位置调整
        /// </summary>
        /// <param name="validPolys"></param>
        /// <param name="mediumNode"></param>
        /// <returns></returns>
        protected override Point3d PostMovePoint(List<Polyline> validPolys, MediumNode mediumNode)
        {
            var pt = mediumNode.Point;
            var pts = new List<Point3d>(); // 不同方向上的点集
            var pointNode = CalculateToleranceClosetPoints(validPolys, pt);
            pts.AddRange(pointNode.NearestPts);

            if (pts.Count == 0)
            {
                return ThMEPCommon.NullPoint3d;
            }

            if (pts.Count == 1)
                return pts.First();

            return CalculateRandomPoint(pts);
        }

        /// <summary>
        /// 计算带有容差的最近点距离
        /// </summary>
        /// <param name="srcPolys"></param>
        /// <param name="srcPt"></param>
        /// <returns></returns>
        protected PointNode CalculateToleranceClosetPoints(List<Polyline> srcPolys, Point3d srcPt)
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

            var nearestDis = nearestPt.DistanceTo(srcPt);

            var nearestPts = closestPts.Where(pt => pt.DistanceTo(srcPt) < (nearestDis + ThMEPCommon.NearestDisTolerance)).ToList();
            if (nearestPts.Count == 0)
                nearestPts.Add(nearestPt);
            return new PointNode(nearestPt, nearestPts);
        }


        /// <summary>
        /// 随机选择移动点
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        private Point3d CalculateRandomPoint(List<Point3d> pts)
        {
            var random = new Random();
            var randomValue = random.Next(0, pts.Count - 1);
            return pts.ToArray()[randomValue];
        }
    }
}
