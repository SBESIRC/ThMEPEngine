﻿using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.SprinklerConnect.Service
{
    internal class ThSprinklerLineService
    {
        public static List<Line> GetConnLine(Point3d pt, List<Line> lineList)
        {
            var tol = new Tolerance(10, 10);
            var connLines = lineList.Where(x => x.StartPoint.IsEqualTo(pt, tol) ||
                                                x.EndPoint.IsEqualTo(pt, tol)).ToList();
            return connLines;
        }

        public static List<Point3d> LineListToPtList(List<Line> lines)
        {
            var tol = new Tolerance(10, 10);
            var ptList = new List<Point3d>();
            for (int i = 0; i < lines.Count; i++)
            {
                var startInList = ptList.Where(x => x.IsEqualTo(lines[i].StartPoint, tol));
                if (startInList.Count() == 0)
                {
                    ptList.Add(lines[i].StartPoint);
                }
                var endInList = ptList.Where(x => x.IsEqualTo(lines[i].EndPoint, tol));
                if (endInList.Count() == 0)
                {
                    ptList.Add(lines[i].EndPoint);
                }
            }

            return ptList;
        }

        public static List<Line> PolylineToLine(List<Polyline> dtPls)
        {
            var tol = new Tolerance(10, 10);
            var dtLines = new List<Line>();
            var dtPts = new List<(Point3d, Point3d)>();

            foreach (var dtPoly in dtPls)
            {
                int ptNum = dtPoly.NumberOfVertices;
                if (dtPoly.Closed == false)
                {
                    ptNum = ptNum - 1;
                }
                for (int i = 0; i < ptNum; i++)
                {
                    var pt1 = dtPoly.GetPoint3dAt(i % dtPoly.NumberOfVertices);
                    var pt2 = dtPoly.GetPoint3dAt((i + 1) % dtPoly.NumberOfVertices);

                    var inList = dtPts.Where(x => (x.Item1.IsEqualTo(pt1, tol) && x.Item2.IsEqualTo(pt2, tol)) ||
                                                    (x.Item1.IsEqualTo(pt2, tol) && x.Item2.IsEqualTo(pt1, tol)));

                    if (inList.Count() == 0 && pt1.IsEqualTo(pt2, tol) == false)
                    {
                        dtPts.Add((pt1, pt2));
                    }
                }
            }

            dtPts.ForEach(x => dtLines.Add(new Line(x.Item1, x.Item2)));

            return dtLines;
        }

        public static bool IsParallelAngle(double angleA, double angleB, double tol)
        {
            var bReturn = false;
            var angleDelta = angleA - angleB;
            var cosAngle = Math.Abs(Math.Cos(angleDelta));

            if (cosAngle > Math.Cos(tol * Math.PI / 180))
            {
                bReturn = true;
            }

            return bReturn;
        }

        /// <summary>
        /// 判断角A角B是否正交。角A角B弧度制
        /// tol:角度容差（角度制），数值大于0 小于90
        /// </summary>
        /// <param name="angleA"></param>
        /// <param name="angleB"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static bool IsOrthogonalAngle(double angleA, double angleB, double tol)
        {
            var bReturn = false;
            var angleDelta = angleA - angleB;
            var cosAngle = Math.Abs(Math.Cos(angleDelta));

            if (cosAngle > Math.Cos(tol * Math.PI / 180) || cosAngle < Math.Cos((90 - tol) * Math.PI / 180))
            {
                bReturn = true;
            }

            return bReturn;
        }

        public static List<Line> GetLineFromList(List<Line> lines, Point3d SP, Point3d EP)
        {
            var tol = new Tolerance();

            var lineSelect = lines.Where(x => (x.StartPoint.IsEqualTo(SP, tol) && x.EndPoint.IsEqualTo(EP, tol)) ||
                                        (x.EndPoint.IsEqualTo(SP, tol) && x.StartPoint.IsEqualTo(EP, tol))).ToList();


            return lineSelect;
        }
    }
}
