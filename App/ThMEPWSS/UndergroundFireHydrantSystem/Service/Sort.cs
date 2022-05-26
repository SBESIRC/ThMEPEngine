using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    public static class Sort
    {
        public static void PointsSort(this List<Point3d> ptls)
        {
            if(ptls.Count >= 2)
            {
                if(Math.Abs(ptls[0].X - ptls[1].X) > Math.Abs(ptls[0].Y - ptls[1].Y))//横向排列
                {
                    ptls = ptls.OrderBy(pt=>pt.X).ToList(); //从左到右
                }
                else//纵向排列
                {
                    ptls = ptls.OrderBy(pt => -pt.Y).ToList(); //从上到下
                }
            }
        }

        public static void LinesSort(this List<Line> lineList)
        {
            if(lineList.Count >= 2)
            {
                if(Math.Abs(lineList[0].StartPoint.X - lineList[0].EndPoint.X) < Math.Abs(lineList[0].StartPoint.Y - lineList[0].EndPoint.Y))
                //横向排列
                {
                    lineList = lineList.OrderBy(line => line.StartPoint.X).ToList();
                }
                else//纵向排列
                {
                    lineList = lineList.OrderBy(line => -line.StartPoint.Y).ToList();//从上到下
                }
            }
        }
    }
}
