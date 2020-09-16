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
    /// 主次梁两个调整
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
            var mediumAdjustor = new MainSecondBeamMediumAdjustor(beamSpanInfo);
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
}
