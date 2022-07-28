﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPWSS.SprinklerConnect.Model;
using ThMEPWSS.SprinklerConnect.Service;

namespace ThMEPWSS.SprinklerDim.Model
{
    public class ThSprinklerDimGroup
    {
        public bool IsDim = new();               //标签
        public List<Point3d> Pts = new();        //存储点的列表
        public bool IsxAxis = false;             //记录标注的方向（如：值为true则代表是一列y值相同的点）
        public double STol = 4800;

        private static long GetValue(Point3d pt, bool isXAxis)
        {
            if (isXAxis)
                return (long)pt.X / 45;
            else
                return (long)pt.Y / 45;
        }

        //获取该组到已经标注的标注线的最小距离
        public double GetMinDis(List<Point3d> DimLine)
        {
            if (DimLine.Count == 0) return 0;
            else if (DimLine.Count == 1)
            {
                List<double> dis = new();
                foreach (Point3d pt in Pts) dis.Add(pt.DistanceTo(DimLine[0]));
                return dis.Min();
            }
            else
            {
                DimLine = DimLine.OrderBy(p => GetValue(p, true)).ThenBy(q => GetValue(q, false)).ToList();
                Line dim = new Line(DimLine[0], DimLine[-1]);
                List<double> dis = new();
                foreach (Point3d pt in Pts) 
                {
                    var pt1 = dim.GetClosestPointTo(pt, true);
                    dis.Add(pt.DistanceTo(pt1));
                }
                return dis.Min();
            }
        }

        //从一个共线的点的列表中分出一个组（按照坐标从小到大的顺序，考虑STol），并将未存入的点返回
        public List<Point3d> GetPoints(List<Point3d> pts)
        {
            pts = pts.OrderBy(p => GetValue(p, true)).ThenBy(q => GetValue(q, false)).ToList();
            List<Point3d> pts1 = new();
            if (pts.Count == 0) return null;
            else
            {
                Vector2d vc = new Vector2d(GetValue(pts[pts.Count - 1], true) - GetValue(pts[0], true), GetValue(pts[pts.Count - 1], false) - GetValue(pts[0], false));
                Vector2d vx = new Vector2d(1, 0);
                IsxAxis = !(vc.DotProduct(vx) == 0);

                Pts.Add(pts[0]);
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    if (pts[i].DistanceTo(pts[i + 1]) < STol)
                    {
                        Pts.Add(pts[i + 1]);
                        continue;
                    }
                    pts1.Add(pts[i + 1]);
                }
                return pts1;
            }
        }





    }
}