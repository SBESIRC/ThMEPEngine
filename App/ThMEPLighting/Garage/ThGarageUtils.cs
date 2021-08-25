using System;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace ThMEPLighting.Garage
{
    public static class ThGarageUtils
    {
        public static bool IsLessThan45Degree(Point3d firstStart, Point3d firstEnd, Point3d secondStart, Point3d secondEnd)
        {
            var distance = new List<double>
            {
                firstStart.DistanceTo(secondStart),
                firstStart.DistanceTo(secondEnd),
                firstEnd.DistanceTo(secondStart),
                firstEnd.DistanceTo(secondEnd)
            };

            var mindistance = double.MaxValue;
            var index = 0;
            for (int i = 0; i < distance.Count; i++)
            {
                if (distance[i] < mindistance)
                {
                    mindistance = distance[i];
                    index = i;
                }
            }

            var a = index / 2;
            var b = index % 2;
            var firstList = new List<Point3d>
            {
                firstStart,
                firstEnd
            };
            var secondList = new List<Point3d>
            {
                secondStart,
                secondEnd
            };

            var first = firstList[1 - a].GetVectorTo(firstList[a]);
            var second = secondList[b].GetVectorTo(secondList[1 - b]);

            if (first.GetAngleTo(second) < Math.PI / 4 - 1e-5)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
