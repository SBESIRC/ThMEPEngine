using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.PostProcess.Adjustor;

namespace ThMEPElectrical.PostProcess
{
    /// <summary>
    /// 规则约束类型
    /// </summary>
    public enum AdjustorType
    {
        ROWCOLADJUSTOR, // 2行或者2列调整
        DISTANCEADJUSTOR, // 距离约束调整
    }

    /// <summary>
    /// 形状约束类型
    /// </summary>
    public enum ShapeConstraintType
    {
        REGULARSHAPE, // 规则图形
        NONREGULARSHAPE, // 非规则图形
    }

    /// <summary>
    /// 布置的位置调整
    /// 行数或者列数 为2的调整
    /// 距离边界的约束调整
    /// </summary>
    public class PlacePointAdjustor
    {
        private List<Point3d> m_srcPts; // 原始的需要调整的插入点
        private Line m_posLine; // 单行或者单列点所在位置的直线
        private double m_distanceConstraint; // 距离约束
        private ShapeConstraintType m_shapeConstraintType; // 形状约束类型

        public List<Point3d> PostPoints { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="srcPts"></param>
        /// <param name="posLine"></param>
        /// <param name="distanceConstraint"></param>
        /// <param name="shapeConstraintType"></param>
        public PlacePointAdjustor(List<Point3d> srcPts, Line posLine, double distanceConstraint, ShapeConstraintType shapeConstraintType)
        {
            m_srcPts = srcPts;
            m_posLine = posLine;
            m_distanceConstraint = distanceConstraint;
            m_shapeConstraintType = shapeConstraintType;
        }

        /// <summary>
        /// 位置调整
        /// </summary>
        public void Do()
        {
            switch(m_shapeConstraintType)
            {
                case ShapeConstraintType.REGULARSHAPE:
                    PostPoints = RegularPlacePointAdjustor.MakeRegularPlacePointAdjustor(m_posLine, m_srcPts);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 根据约束规则进行位置调整
        /// </summary>
        /// <param name="srcPts"></param>
        /// <param name="posLine"></param>
        /// <param name="distanceConstraint"></param>
        /// <param name="shapeConstraintType"> 根据约束的类型使用相应的计算方法</param>
        /// <returns></returns>
        public static List<Point3d> MakePlacePointAdjustor(List<Point3d> srcPts, Line posLine, double distanceConstraint, ShapeConstraintType shapeConstraintType)
        {
            var placePointAdjustor = new PlacePointAdjustor(srcPts, posLine, distanceConstraint, shapeConstraintType);
            placePointAdjustor.Do();
            return placePointAdjustor.PostPoints;
        }
    }
}
