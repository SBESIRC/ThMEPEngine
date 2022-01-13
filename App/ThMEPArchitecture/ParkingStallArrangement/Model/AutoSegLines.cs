using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPArchitecture.ParkingStallArrangement.Model
{
    public class AutoSegLines
    {
        public int SeglineClass { get; set; }//分割线层级
        public Line Seglines { get; set; }//分割线
        public List<Polyline> SegAreas { get; set; }//由分割线生成的区域
        public double MaxValues { get; set; }//分割线的最大值
        public double MinValues { get; set; }//分割线的最小值

        public AutoSegLines()
        {
            Seglines = new Line();
            SegAreas = new List<Polyline>();
            MaxValues = 0;
            MinValues = 0;
        }
        public AutoSegLines(Line segLine, List<Polyline> segAreas, double maxVal, double minVal)
        {
            Seglines = segLine;
            SegAreas = segAreas;
            MaxValues = maxVal;
            MinValues = minVal;
        }
    }
}
