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
    public class ThReducingParameters
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

    public class ThReducing : IPipeFitting
    {
        public DBObjectCollection Geometries { get; set; }
        public ThReducingParameters Parameters { get; set; }

        public ThReducing(ThReducingParameters parameters)
        {
            Parameters = parameters;
            Parameters.StartCenterPoint = new Point3d(0,0,0);
        }
    }
}
