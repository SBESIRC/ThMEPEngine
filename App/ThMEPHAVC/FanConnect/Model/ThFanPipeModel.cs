using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ThMEPHVAC.FanConnect.Model
{
    public enum PIPELEVEL
    {
        LEVEL1,//第一级管（干管）
        LEVEL2,//第二级管（支干管）
        LEVEL3 //第三级管（支管）
    }
    public enum PIPETYPE
    {
        R,
        C,
        CS,
        CR,
        HS,
        HR,
        CHS,
        CHR,
        CSCR,
        HSHR,
    }

    public class ThFanPipeModel
    {
        
        public bool IsFlag { set; get; }
        public bool IsConnect { set; get; }//与父结点是否连接
        public double PipeWidth { set; get; }
        public PIPELEVEL PipeLevel { set; get; }
        public Vector3d CroVector { set; get; }//与父结点叉乘方向
        public Line PLine { set; get; }
        public List<Line> ExPline { set; get; }
        public List<Point3d> ExPoint { set; get; }
        public ThFanPipeModel(Line line, PIPELEVEL level = PIPELEVEL.LEVEL1,double width = 200)
        {
            IsFlag = false;
            IsConnect = false;
            PipeWidth = width;
            PipeLevel = level;
            PLine = line;
            ExPoint = new List<Point3d>();
            CroVector = new Vector3d(0.0,0.0,1.0);
        }
        
    }
}
