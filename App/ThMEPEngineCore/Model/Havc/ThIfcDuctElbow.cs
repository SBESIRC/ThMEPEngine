using System;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPEngineCore.Model.Havc
{
    public class ThIfcDuctElbowParameters
    {
        /// <summary>
        /// 弯头角度(弧度)
        /// </summary>
        public double ElbowDegree { get; set; }

        /// <summary>
        /// 管道直径
        /// </summary>
        public double PipeOpenWidth { get; set; }

        /// <summary>
        /// 末端直管段长度
        /// </summary>
        public double ReserveLength { get; set; }

        /// <summary>
        /// 角点
        /// </summary>
        public Point3d CornerPoint { get; set; }

        /// <summary>
        /// 中心点
        /// </summary>
        public Point3d CenterPoint
        {
            get
            {
                return CornerPoint + new Vector3d(-PipeOpenWidth, -Math.Abs(PipeOpenWidth * Math.Tan(0.5 * (ElbowDegree*Math.PI/180))), 0);
            }
        }

        /// <summary>
        /// 旋转角度
        /// </summary>
        public double RotateAngle { get; set; }
    }

    public class ThIfcDuctElbow : ThIfcDuctFitting
    {
        public ThIfcDuctElbowParameters Parameters { get; private set; }

        public ThIfcDuctElbow(ThIfcDuctElbowParameters parameters)
        {
            Parameters = parameters;
            Parameters.CornerPoint = Point3d.Origin;
        }

        public static ThIfcDuctElbow Create(ThIfcDuctElbowParameters parameters)
        {
            throw new NotImplementedException();
        }
    }
}
