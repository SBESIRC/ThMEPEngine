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
    public class ThFanFireValveModel
    {
        public Point3d FireValvePosition { set; get; }//防火阀位置
        public double FontHeight { set; get; }//文字高度
        public double FireValveAngle { set; get; }//防火阀角度
        public double FireValveWidth { set; get; }//防火阀宽度
        public string FireValveMark { set; get; }//可见性
    }
}
