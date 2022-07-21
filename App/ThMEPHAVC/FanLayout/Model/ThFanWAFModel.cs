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
    /// <summary>
    /// 壁式轴流风机
    /// </summary>
    public class ThFanWAFModel
    {
        public Point3d FanPosition { set; get; }//风机位置
        public double FontHeight { set; get; }//字体高度
        public double FanAngle { set; get; }//风机角度
        public double RotateAngle { set; get; }//旋转角度
        public string FanNumber { set; get; }//设备编号
        public string FanVolume { set; get; }//风量
        public string FanPower { set; get; }//电量=功率
        public string FanWeight { set; get; }//重量
        public string FanNoise { set; get; } //噪音
        public double FanDepth { set; get; }//深度
        public double FanWidth { set; get; }//宽度
        public double FanLength { set; get; }//长度
        public string FanMark { set; get; }//标高
    }
}
