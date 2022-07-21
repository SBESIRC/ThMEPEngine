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
    public class ThFanHoleModel
    {
        public Point3d FanHolePosition { set; get; }//洞口位置
        public double FontHeight { set; get; }//文字高度
        public double FanHoleWidth { set; get; }//洞口宽度
        public double FanHoleAngle { set; get; }//洞口角度
        public double RotateAngle { set; get; }//旋转角度
        public string FanHoleSize { set; get; }//洞口尺寸
        public string FanHoleMark { set; get; }//标高
    }
}
