using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPArchitecture.ParkingStallArrangement.Model;

namespace ThMEPArchitecture.ParkingStallArrangement.Method
{
    public static class LineTools
    {
        public static bool GetValue(this Line line, out double value, out double startVal, out double endVal)
        {
            value = 0;
            startVal = 0;
            endVal = 0;
            if (line == null)
            {
                return false;
            }
            var dir = line.GetDirection() > 0;
            if (dir)
            {
                value = line.StartPoint.X;
                startVal = line.StartPoint.Y;
                endVal = line.EndPoint.Y;
            }
            else
            {
                value = line.StartPoint.Y;
                startVal = line.StartPoint.X;
                endVal = line.EndPoint.X;
            }
            return dir;
        }

        public static int GetDirection(this Line line, double tor = 0.035)
        {
            var angle = line.Angle;
            while(true)
            {
                if(angle >= Math.PI)
                {
                    angle -= Math.PI;
                }
                if(angle < 0)
                {
                    angle += Math.PI;
                }
                if(angle >= 0 && angle < Math.PI)
                {
                    break;
                }
            }
            if(Math.Abs(angle - Math.PI/2.0) < tor)
            {
                return 1;//竖直输出 1
            }
            if(angle < tor || Math.Abs(angle - Math.PI) < tor)
            {
                return -1;//水平输出 -1
            }
            return 0;//斜线输出 0
        }

        public static bool EqualsTo(this Line line1, Line line2, double tor = 1.0)
        {
            var spt1 = line1.StartPoint;
            var ept1 = line1.EndPoint;
            var spt2 = line2.StartPoint;
            var ept2 = line2.EndPoint;
            if (spt1.DistanceTo(spt2) < tor && ept2.DistanceTo(ept2) < tor)
            {
                return true;
            }
            if (spt1.DistanceTo(ept2) < tor && spt2.DistanceTo(ept1) < tor)
            {
                return true;
            }
            return false;
        }

        public static Point3d GetCenterPt(this Line line)
        {
            var spt = line.StartPoint;
            var ept = line.EndPoint;
            return PtTools.GetMiddlePt(spt, ept);
        }

        public static bool EqualsTo(this List<Line> lines1, List<Line> lines2)
        {
            var sortLines1 = lines1.OrderBy(l => l.GetCenterPt().X).ThenByDescending(l => l.GetCenterPt().Y).ToList();//从左向右，从上到下
            var sortLines2 = lines2.OrderBy(l => l.GetCenterPt().X).ThenByDescending(l => l.GetCenterPt().Y).ToList();//从左向右，从上到下
            for (int i =0; i < sortLines1.Count; i++)
            {
                if(!sortLines1[i].EqualsTo(sortLines2[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
