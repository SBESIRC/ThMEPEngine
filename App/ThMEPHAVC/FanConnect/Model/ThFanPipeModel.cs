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
        public PIPETYPE PipeType { set; get; }
        public PIPELEVEL PipeLevel { set; get; }
        public LineSegment2d LineSegment { set; get; }
        public ThFanPipeModel(LineSegment2d line, PIPELEVEL level = PIPELEVEL.LEVEL1)
        {
            PipeLevel = level;
            LineSegment = line;
        }
        
    }
}
