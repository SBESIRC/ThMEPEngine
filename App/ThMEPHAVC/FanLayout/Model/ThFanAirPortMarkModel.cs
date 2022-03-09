using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FanLayout.Model
{
    public class ThFanAirPortMarkModel
    {
        public Point3d FanPosition { set; get; }//风机位置
        public Point3d AirPortMarkPosition { set; get; }//风口位置
        public double FontHeight { set; get; }//字体高度或者比例
        public string AirPortMarkName { set; get; }//风口名称
        public string AirPortMarkSize { set; get; }//尺寸
        public string AirPortMarkCount { set; get; }//风口数量
        public string AirPortMarkVolume { set; get; }//风量
        public string AirPortHeightMark { set; get; }//标高
    }
}
