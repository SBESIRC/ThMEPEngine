using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FanConnect.Model
{
    public class ThFanCUModel
    {
        public string FanType { set; get; }//类型
        public Point3d FanPoint { set; get; }//连接点
        public Polyline FanObb { set; get; }//设备外包框
    }
}
