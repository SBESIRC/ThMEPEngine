using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FanLayout.Model
{
    public class ThFanAirPortModel
    {
        public string AirPortType { set; get; }//风口类型
        public Point3d AirPortPosition { set; get; }//风口位置
        public double AirPortAngle { set; get; }//风口角度
        public double RotateAngle { set; get; }//旋转角度
        public double AirPortLength { set; get; }//风口长度
        public double AirPortDepth { set; get; }//侧回风口深度
        public short AirPortDirection { set; get; }//气流方向
    }
}
