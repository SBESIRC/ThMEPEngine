using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;

namespace ThMEPTCH.TCHArchDataConvert.THArchEntity
{
    class WallEntity:THArchEntityBase
    {
        public Point3d StartPoint { get; set; }
        public Point3d EndPoint { get; set; }
        /// <summary>
        /// 墙中心线，非异形墙有数据（Line,Arc），后续用来计算交点处的延长问题
        /// 非线或弧线的异形墙不计算延长计算轮廓
        /// </summary>
        public Curve WallCenterCurve { get; set; }
        /// <summary>
        /// 圆弧墙时，此轮廓为内弧线
        /// </summary>
        public Curve WallLeftCurve { get; set; }
        public Curve WallRightCurve { get; set; }
        public double Elevtion { get; set; }
        public double LeftWidth { get; set; }
        public double RightWidth { get; set; }
        public double WallHeight { get; set; }
        public double WallMinZ
        {
            get
            {
                if (null == WallCenterCurve && null == Outline)
                    return 0.0;
                if (null != WallCenterCurve)
                {
                    var pt = WallCenterCurve.StartPoint;
                    return pt.Z;
                }
                return 0.0;
            }
        }
        public double WallMaxZ
        {
            get
            {
                if (null == WallCenterCurve && null == Outline)
                    return 0.0;
                if (null != WallCenterCurve)
                {
                    var pt = WallCenterCurve.StartPoint;
                    return pt.Z + WallHeight;
                }
                return 0.0;
            }
        }
        public WallEntity(TArchEntity dbWall):base(dbWall)
        {
        }
    }
}
