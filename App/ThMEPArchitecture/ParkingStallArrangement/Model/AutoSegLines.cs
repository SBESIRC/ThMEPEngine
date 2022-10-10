using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThParkingStall.Core.Tools;

namespace ThMEPArchitecture.ParkingStallArrangement.Model
{
    public class AutoSegLines
    {
        public Line Seglines { get; set; }//分割线
        public double MaxValues { get; set; }//分割线的最大值
        public double MinValues { get; set; }//分割线的最小值
        public bool Direction { get; set; }//分割线的方向

        public AutoSegLines(Line segLine, double maxVal, double minVal)
        {
            Seglines = segLine;
            MaxValues = maxVal;
            MinValues = minVal;
            Direction = segLine.GetDirection() == 1;
        }

        public SegLineEx GetRandomLine()
        {
            var direction = Seglines.GetDirection() == 1;
            if (direction)
            {
                var lastCur = Seglines.StartPoint.X;
                var randomVal = lastCur + MinValues + ThParkingStallCoreTools.RandDouble() * (MaxValues - MinValues);
                var spt = Seglines.StartPoint;
                var ept = Seglines.EndPoint;
                var line = new Line(new Point3d(randomVal, spt.Y, 0), new Point3d(randomVal, ept.Y, 0));
                var maxVal = MaxValues + lastCur - randomVal;
                var minVal = MinValues + lastCur - randomVal;
                return new SegLineEx(line, maxVal, minVal);
            }
            else
            {
                var lastCur = Seglines.StartPoint.Y;
                var randomVal = lastCur + MinValues + ThParkingStallCoreTools.RandDouble() * (MaxValues - MinValues);
                var spt = Seglines.StartPoint;
                var ept = Seglines.EndPoint;
                var line = new Line(new Point3d(spt.X, randomVal, 0), new Point3d(ept.X, randomVal, 0));
                var maxVal = MaxValues + lastCur - randomVal;
                var minVal = MinValues + lastCur - randomVal;
                return new SegLineEx(line, maxVal, minVal);
            }
        }
    }
}
