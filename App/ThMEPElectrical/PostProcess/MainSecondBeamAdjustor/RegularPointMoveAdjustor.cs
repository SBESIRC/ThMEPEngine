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

            if (pts.Count == 0)
            {
                return ThMEPCommon.NullPoint3d;
            }

            if (pts.Count == 1)
                return pts.First();

            return CalculateRandomPoint(pts);
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
