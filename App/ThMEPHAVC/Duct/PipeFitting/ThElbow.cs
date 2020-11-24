using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using GeometryExtensions;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHAVC.Duct.PipeFitting
{
    public class ThElbowParameters
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

    public class ThElbow : IPipeFitting
    {
        public DBObjectCollection Geometries { get; set; }
        public ThElbowParameters Parameters { get; set; }

        public ThElbow(ThElbowParameters parameters)
        {
            Parameters = parameters;
            Parameters.CornerPoint = new Point3d(0,0,0);
        }
    }
}
