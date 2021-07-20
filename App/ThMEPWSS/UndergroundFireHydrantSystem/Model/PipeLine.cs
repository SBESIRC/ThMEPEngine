﻿using Autodesk.AutoCAD.DatabaseServices;
using GeometryExtensions;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Model
{
    class PipeLine
    {
        public static void DealCircular(DBObjectCollection dbObjs, ref FireHydrantSystemIn fireHydrantSysIn, ref List<Point3dEx> pointList, ref List<Line> lineList)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lineList.ToCollection());

            foreach (var f in dbObjs)
            {
                if (f is Circle)
                {
                    var circle = f as Circle;
                    var center = circle.Center;
                    var obb = circle.ToRectangle().Buffer(10)[0] as Polyline;
                    var crossObjs = spatialIndex.SelectCrossingPolygon(obb);
                    foreach (var obj in crossObjs)
                    {
                        if (obj is Line)
                        {
                            var line = obj as Line;
                            if (obb.Contains(line.StartPoint))
                            {
                                var pt1 = new Point3dEx(line.StartPoint.X, line.StartPoint.Y, 0);
                                var pt2 = new Point3dEx(center.X, center.Y, 0);
                                pointList.Add(pt1);
                                pointList.Add(pt2);
                                ThPointCountService.AddPoint(ref fireHydrantSysIn, ref pt1, ref pt2, "MainLoop");
                                lineList.Add(new Line(pt1._pt, pt2._pt));
                            }
                            else if (obb.Contains(line.EndPoint))
                            {
                                var pt1 = new Point3dEx(line.EndPoint.X, line.EndPoint.Y, 0);
                                var pt2 = new Point3dEx(center.X, center.Y, 0);
                                pointList.Add(pt1);
                                pointList.Add(pt2);
                                ThPointCountService.AddPoint(ref fireHydrantSysIn, ref pt1, ref pt2, "MainLoop");
                                lineList.Add(new Line(pt1._pt, pt2._pt));
                            }
                        }
                    }
                }
            }
        }
        public static void AddPipeLine(DBObjectCollection dbObjs, ref FireHydrantSystemIn fireHydrantSysIn, ref List<Point3dEx> pointList, ref List<Line> lineList)
        {
            foreach (var f in dbObjs)
            {
                var fl = f as Polyline;
                if (fl is null)
                {
                    var fline = f as Line;
                    var pt1 = new Point3dEx(fline.StartPoint.X, fline.StartPoint.Y, 0);
                    var pt2 = new Point3dEx(fline.EndPoint.X, fline.EndPoint.Y, 0);
                    pointList.Add(pt1);
                    pointList.Add(pt2);
                    ThPointCountService.AddPoint(ref fireHydrantSysIn, ref pt1, ref pt2, "MainLoop");
                    lineList.Add(new Line(pt1._pt, pt2._pt));
                }
                else
                {
                    var ptPre = fl.GetPoint3dAt(0);
                    for (int i = 1; i < fl.NumberOfVertices; i++)
                    {
                        var pti = fl.GetPoint3dAt(i);
                        var pt1 = new Point3dEx(ptPre.X, ptPre.Y, 0);
                        var pt2 = new Point3dEx(pti.X, pti.Y, 0);
                        pointList.Add(pt1);
                        pointList.Add(pt2);
                        ThPointCountService.AddPoint(ref fireHydrantSysIn, ref pt1, ref pt2, "MainLoop");
                        lineList.Add(new Line(pt1._pt, pt2._pt));
                        ptPre = fl.GetPoint3dAt(i);
                    }
                }
            }
        }

        public static void AddValveLine(DBObjectCollection valveDB,
            ref FireHydrantSystemIn fireHydrantSysIn, ref List<Point3dEx> pointList, ref List<Line> lineList, ref List<Line> valveList)
        {
            foreach (var v in valveDB)
            {
                if (v is BlockReference)
                {
                    var br = v as BlockReference;
                    var valve = new ThFireHydrantValve(br);
                    var line1 = valve.GetLine(fireHydrantSysIn.ValveIsBkReference);
                    var pt1 = new Point3dEx(line1.StartPoint);
                    var pt2 = new Point3dEx(line1.EndPoint);
                    pointList.Add(pt1);
                    pointList.Add(pt2);
                    valveList.Add(line1);
                    //lineList.Add(new Line(pt1._pt, pt2._pt));
                    ThPointCountService.AddPoint(ref fireHydrantSysIn, ref pt1, ref pt2, "Valve");
                }
                else
                {
                    var br = new DBObjectCollection();

                    (v as Entity).Explode(br);
                    var bkr = br[0] as BlockReference;
                    var centerPt = bkr.Position;
                    var angle = bkr.Rotation;
                    var distance = 1000;
                    var tempt = centerPt.Polar(angle, distance);
                    var baseLine = new Line(centerPt, tempt);

                    var boundPt1 = (br[0] as BlockReference).GeometricExtents.MaxPoint;

                    var boundPt2 = (br[0] as BlockReference).GeometricExtents.MinPoint;
                    var bdpt1 = baseLine.GetClosestPointTo(boundPt1, true);
                    var bdpt2 = baseLine.GetClosestPointTo(boundPt2, true);

                    var pt1 = new Point3dEx(bdpt1);
                    var pt2 = new Point3dEx(bdpt2);
                    pointList.Add(pt1);
                    pointList.Add(pt2);
                    valveList.Add(new Line(bdpt1, bdpt2));
                    //lineList.Add(new Line(pt1._pt, pt2._pt));
                    ThPointCountService.AddPoint(ref fireHydrantSysIn, ref pt1, ref pt2, "Valve");
                }

            }
            PipeLineSplit(ref lineList, valveList);

        }

        public static void PipeLineSplit(ref List<Line> pipeLineList, List<Point3dEx> pointList)
        {
            foreach (var pt in pointList)//管线打断
            {
                var line = PointCompute.PointInLine(pt._pt, pipeLineList);
                if (!PointCompute.IsNullLine(line))
                {
                    if (!PointCompute.PointIsLineTerm(pt._pt, line))
                    {
                        pipeLineList.Remove(line);
                        var lList = PointCompute.CreateNewLine(pt._pt, line);

                        foreach (var ls in lList)
                        {
                            pipeLineList.Add(ls);
                        }
                    }
                }
            }
        }


        public static void PipeLineSplit(ref List<Line> pipeLineList, List<Line> valveLineList)
        {
            foreach (var valve in valveLineList)//管线打断
            {
                var pipeLine = LineOnLine.LineIsOnLine(valve, pipeLineList);
                LineOnLine.LineSplit(valve, pipeLine, ref pipeLineList);

            }
        }
    }
}
