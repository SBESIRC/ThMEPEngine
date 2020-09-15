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
    public class MainSecondBeamSingleAdjustor : PointMoveAdjustor
    {

        public MainSecondBeamSingleAdjustor(MainSecondBeamRegion beamSpanInfo)
            : base(beamSpanInfo)
        {
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
            return singleAdjustor.PostPoints;
        }

        public override void Do()
        {
            // 有效区域
            var polylines = m_mainSecondBeamRegion.ValidRegions;

            // 原始的布置点
            var srcPt = m_mainSecondBeamRegion.PlacePoints.First();

            if (IsValidPoint(polylines, srcPt))
            {
                PostPoints.Add(srcPt);
            }
            else
            {
                // 选择最近的点
                PostPoints.Add(CalculateClosetPoint(polylines, srcPt));
            }
        }
    }
}
