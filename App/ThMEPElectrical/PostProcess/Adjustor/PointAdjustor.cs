using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using ThMEPElectrical.PostProcess.ConstraintInterface;

namespace ThMEPElectrical.PostProcess.Adjustor
{
    /// <summary>
    /// 位置调整器
    /// </summary>
    public abstract class PointAdjustor : IBeautyConstraint, IDistanceConstraint
    {
        protected List<Point3d> m_postPts; // 调整后点集
        protected List<Point3d> m_srcPts; // 没有调整的原始点

        public List<Point3d> PostPoints
        {
            get { return m_postPts; }
        }

        public PointAdjustor(List<Point3d> srcPts)
        {
            m_srcPts = srcPts;
        }

        public abstract List<Point3d> CalculateBeautifyPoints();
        public abstract List<Point3d> CalculateDistancePoints();
    }
}
