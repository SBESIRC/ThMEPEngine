using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPElectrical.PostProcess.Adjustor
{
    /// <summary>
    /// 规则的布置调整计算
    /// </summary>
    public class RegularPlacePointAdjustor : PointAdjustor
    {
        private Line m_posLine; // 规则矩形点布置所在的线

        public RegularPlacePointAdjustor(Line posLine, List<Point3d> srcPts)
            :base(srcPts)
        {
            m_posLine = posLine;
        }

        /// <summary>
        /// 规则形状点集的调整计算
        /// </summary>
        /// <param name="posLine"></param>
        /// <param name="srcPts"></param>
        /// <returns></returns>
        public static List<Point3d> MakeRegularPlacePointAdjustor(Line posLine, List<Point3d> srcPts)
        {
            var regularAdjustor = new RegularPlacePointAdjustor(posLine, srcPts);
            regularAdjustor.DoAdjust();
            return regularAdjustor.PostPoints;
        }


        private void DoAdjust()
        {
            m_postPts = CalculateBeautifyPoints();
        }

        /// <summary>
        /// 美观约束计算
        /// </summary>
        /// <returns></returns>
        public override List<Point3d> CalculateBeautifyPoints()
        {
            var pts = new List<Point3d>();
            var length = m_posLine.Length;

            var gapDistance = length / 4.0;

            var startPoint = m_posLine.StartPoint;

            var offsetDirection = m_posLine.GetFirstDerivative(startPoint).GetNormal();

            var firstPt = startPoint + offsetDirection * gapDistance;

            var secondPt = firstPt + offsetDirection * gapDistance * 2;

            pts.Add(firstPt);
            pts.Add(secondPt);
            return pts;
        }

        /// <summary>
        /// 距离约束计算
        /// </summary>
        /// <returns></returns>
        public override List<Point3d> CalculateDistancePoints()
        {
            throw new NotImplementedException();
        }
    }
}
