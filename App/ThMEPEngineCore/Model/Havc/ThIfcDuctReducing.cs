using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model.Havc
{
    public class ThIfcDuctReducingParameters
    {
        /// <summary>
        /// 异径大端截面宽度
        /// </summary>
        public double BigEndWidth { get; set; }

        /// <summary>
        /// 异径小端截面宽度
        /// </summary>
        public double SmallEndWidth { get; set; }

        /// <summary>
        /// 小端中点
        /// </summary>
        public Point3d StartCenterPoint { get; set; }

        /// <summary>
        /// 旋转角度
        /// </summary>
        public double RotateAngle { get; set; }
    }

    public class ThIfcDuctReducing : ThIfcDuctFitting
    {
        public ThIfcDuctReducingParameters Parameters { get; set; }

        public ThIfcDuctReducing(ThIfcDuctReducingParameters parameters)
        {
            Parameters = parameters;
            Parameters.StartCenterPoint = Point3d.Origin;
        }

        public static ThIfcDuctReducing Create(ThIfcDuctReducingParameters parameters)
        {
            throw new NotImplementedException();
        }
    }
}
