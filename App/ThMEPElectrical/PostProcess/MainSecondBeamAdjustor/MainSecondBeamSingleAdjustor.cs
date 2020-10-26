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

        protected override void DefinePosPointsInfo()
        {
            m_mediumNodes.Add(new MediumNode(m_mainSecondBeamRegion.PlacePoints.First(), PointPosType.LeftTopPoint));
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
            var pointNode = CalculateClosetPoints(validPolys, pt);

            pts.AddRange(pointNode.NearestPts);

            if (pts.Count == 1)
                return pts.First();

            // 定义点集所在的象限
            var quadrantNode = DefineQuadrantInfo(pts, mediumNode);

            return SelectPoint(quadrantNode);
        }
    }
}
