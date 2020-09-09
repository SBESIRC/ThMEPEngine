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
    /// 先拆分，后期可能合并
    /// </summary>
    public class NonRegularPlacePointAdjustor : PointAdjustor
    {
        private Line m_posLine;
        public NonRegularPlacePointAdjustor(Line midLine, List<Point3d> srcPts)
            :base(srcPts)
        {
            m_posLine = midLine;
        }

        /// <summary>
        /// 异形点集的调整计算
        /// </summary>
        /// <param name="posLine"></param>
        /// <param name="srcPts"></param>
        /// <returns></returns>
        public static List<Point3d> MakeNonRegularPlacePointAdjustor(Line posLine, List<Point3d> srcPts)
        {
            var regularAdjustor = new NonRegularPlacePointAdjustor(posLine, srcPts);
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

        public override List<Point3d> CalculateDistancePoints()
        {
            throw new NotImplementedException();
        }
    }
}
