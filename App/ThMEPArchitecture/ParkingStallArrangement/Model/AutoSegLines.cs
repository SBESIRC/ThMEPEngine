using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPArchitecture.ParkingStallArrangement.Method;

namespace ThMEPArchitecture.ParkingStallArrangement.Model
{
    public class AutoSegLines
    {
        public int SeglineClass { get; set; }//分割线层级
        public Line Seglines { get; set; }//分割线
        public List<Polyline> SegAreas { get; set; }//由分割线生成的区域
        public double MaxValues { get; set; }//分割线的最大值
        public double MinValues { get; set; }//分割线的最小值
        public bool Direction { get; set; }//分割线的方向

        public AutoSegLines()
        {
            Seglines = new Line();
            SegAreas = new List<Polyline>();
            MaxValues = 0;
            MinValues = 0;
            Direction = true;
        }
        public AutoSegLines(Line segLine, List<Polyline> segAreas, double maxVal, double minVal)
        {
            Seglines = segLine;
            SegAreas = segAreas;
            MaxValues = maxVal;
            MinValues = minVal;
            Direction = segLine.GetDirection() == 1;
        }

        public Line GetRandomLine()
        {
            var direction = Seglines.GetDirection() == 1;
            if (direction)
            {
                var randomVal = Seglines.StartPoint.X + MinValues + General.Utils.RandDouble() * (MaxValues - MinValues);
                var spt = Seglines.StartPoint;
                var ept = Seglines.EndPoint;
                return new Line(new Point3d(randomVal,spt.Y,0), new Point3d(randomVal, ept.Y, 0));
            }
            else
            {
                var randomVal = Seglines.StartPoint.Y + MinValues + General.Utils.RandDouble() * (MaxValues - MinValues);
                var spt = Seglines.StartPoint;
                var ept = Seglines.EndPoint;
                return new Line(new Point3d(spt.X, randomVal, 0), new Point3d(ept.X, randomVal, 0));
            }
        }
    }
}
