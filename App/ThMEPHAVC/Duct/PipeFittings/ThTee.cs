using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHAVC.Duct.PipeFitting
{
    public class ThTeeParameters
    {
        /// <summary>
        /// 中心点
        /// </summary>
        public Point3d CenterPoint { get; set; }

        /// <summary>
        /// 支路管道直径
        /// </summary>
        public double BranchDiameter { get; set; }

        /// <summary>
        /// 主路大端管道直径
        /// </summary>
        public double MainBigDiameter { get; set; }

        /// <summary>
        /// 主路小端管道直径
        /// </summary>
        public double MainSmallDiameter { get; set; }

        /// <summary>
        /// 旋转角度
        /// </summary>
        public double RotateAngle { get; set; }
    }
    public class ThTee : IPipeFitting
    {
        public DBObjectCollection Geometries { get; set; }
        public ThTeeParameters Parameters { get; set; }

        public ThTee(ThTeeParameters parameters)
        {
            Parameters = parameters;
            Parameters.CenterPoint = new Point3d(0, 0, 0);
        }

    }
}
