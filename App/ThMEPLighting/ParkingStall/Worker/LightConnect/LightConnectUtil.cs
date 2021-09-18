using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ThMEPLighting.ParkingStall.Worker.LightConnect
{
    class LightConnectUtil
    {
        public static List<Line> TransPolylineToLine(Polyline polyline)
        {
            ///闭合多段线获取线段
            List<Line> lines = new List<Line>();
            int count = polyline.NumberOfVertices;
            count = polyline.IsReallyClosing ? count : count - 1;
            for (int i = 0; i < count; i++)
            {
                var line = new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % polyline.NumberOfVertices));
                if (line.Length > 0)
                {
                    lines.Add(line);
                }
            }
            return lines;
        }
        public static Point3d GetGroupNearPoint(List<Point3d> firstPoints, List<Point3d> secondPoints, out Point3d secondPoint)
        {
            double nearDis = double.MaxValue;
            Point3d firstPoint = new Point3d();
            secondPoint = new Point3d();
            foreach (var point1 in firstPoints)
            {
                foreach (var point2 in secondPoints)
                {
                    var dis = point1.DistanceTo(point2);
                    if (dis >= nearDis)
                        continue;
                    firstPoint = point1;
                    secondPoint = point2;
                    nearDis = dis;
                }
            }
            return firstPoint;
        }
        public static bool GroupDirIsParallel(Vector3d firstDir,Vector3d secondDir,double precision=5.0)
        {
            var angle = firstDir.GetAngleTo(secondDir);
            angle %= Math.PI;
            angle = Math.Abs(angle);
            if (angle < (Math.PI * precision / 180.0) || angle > ((180- precision) * Math.PI / 180.0))
                return true;
            return false;
        }
        public static Point3d NearPoint(List<Point3d> firstPoints, List<Point3d> secondPoints, out Point3d secondPoint)
        {
            Point3d firstPoint = firstPoints.First();
            secondPoint = secondPoints.First();
            double minDis = double.MaxValue;
            foreach (var first in firstPoints)
            {
                foreach (var second in secondPoints)
                {
                    var dis = first.DistanceTo(second);
                    if (dis < minDis)
                    {
                        minDis = dis;
                        firstPoint = first;
                        secondPoint = second;
                    }
                }
            }
            return firstPoint;
        }
    }
}
