﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    class PtOnLine
    {
        public static bool PtIsOnLine(Point3d pt, Line line)//判断点是否在线上
        {
            if(line is null)
            {
                return false;
            }
            var tolerance = 10;
            if(line.GetClosestPointTo(pt, false).DistanceTo(pt) < tolerance)//点在线的内部
            {
                return true;
            }
            else//点在线的外部
            {
                if(line.StartPoint.DistanceTo(pt) < tolerance || line.StartPoint.DistanceTo(pt) < tolerance)
                {
                    return true;
                }
            }
            return false;
        }


        public static bool PtIsOnLine(Point3d pt, Line line, double tolerance)//判断点是否在线上
        {
            if (line is null)
            {
                return false;
            }
            if (line.GetClosestPointTo(pt, false).DistanceTo(pt) < tolerance)//点在线的内部
            {
                return true;
            }
            else//点在线的外部
            {
                if (line.StartPoint.DistanceTo(pt) < tolerance || line.StartPoint.DistanceTo(pt) < tolerance)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
