using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHAVC.Duct.PipeFitting
{
    public class ThFourWayParameters
    {
        /// <summary>
        /// 四通大端管道截面宽度
        /// </summary>
        public double BigEndWidth { get; set; }

        /// <summary>
        /// 四通与大端对接管道截面宽度
        /// </summary>
        public double mainSmallEndWidth { get; set; }

        /// <summary>
        /// 四通侧路大端管道截面宽度
        /// </summary>
        public double SideBigEndWidth { get; set; }

        /// <summary>
        /// 四通侧路小端管道截面宽度
        /// </summary>
        public double SideSmallEndWidth { get; set; }

        /// <summary>
        /// 四通中心点
        /// </summary>
        public Point3d FourWayCenter { get; set; }

        /// <summary>
        /// 旋转角度
        /// </summary>
        public double RotateAngle { get; set; }

    }

    public class ThFourWay : IPipeFitting
    {
        public DBObjectCollection Geometries { get; set; }
        public ThFourWayParameters Parameters { get; set; }

        public ThFourWay(ThFourWayParameters parameters)
        {
            Parameters = parameters;
            Parameters.FourWayCenter = new Point3d(0, 0, 0);
        }

    }
}
