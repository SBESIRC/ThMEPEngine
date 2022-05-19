using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore;
using ThMEPEngineCore.CAD;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Model
{
    public class PipeLine
    {
        public static bool hasSitong(FireHydrantSystemIn fireHydrantSysIn)
        {
            var sitong = false;
            foreach (var pt in fireHydrantSysIn.PtDic.Keys)
            {
                var cnt = fireHydrantSysIn.PtDic[pt].Count;
                if (cnt > 3)
                {
                    sitong = true;
                    Active.Editor.WriteMessage($"\n在点{pt._pt.X},");
                    Active.Editor.WriteMessage($"{pt._pt.Y}处存在四通!");
                }
            }
            return sitong;
        }

        public static void AddPipeLine(DBObjectCollection dbObjs, ref FireHydrantSystemIn fireHydrantSysIn, 
            ref List<Point3dEx> pointList, ref List<Line> lineList)
        {
            double pipeLenTor = 25.0;
            foreach (var f in dbObjs)
            {
                if (f is Line fline)
                {
                    if(fline.Length < pipeLenTor)
                    {
                        continue;//小于1的直线跳过
                    }
                    var pt1 = new Point3dEx(fline.StartPoint.X, fline.StartPoint.Y, 0);
                    var pt2 = new Point3dEx(fline.EndPoint.X, fline.EndPoint.Y, 0);
                    
                    pointList.Add(pt1);
                    pointList.Add(pt2);
                    ThPointCountService.AddPoint(ref fireHydrantSysIn, ref pt1, ref pt2, "MainLoop");
                    lineList.Add(new Line(pt1._pt, pt2._pt));
                }
                if(f is Polyline fl)
                {
                    var ptPre = fl.GetPoint3dAt(0);
                    for (int i = 1; i < fl.NumberOfVertices; i++)
                    {
                        var pti = fl.GetPoint3dAt(i);
                        var pt1 = new Point3dEx(ptPre.X, ptPre.Y, 0);
                        var pt2 = new Point3dEx(pti.X, pti.Y, 0);
                        if(pt1._pt.DistanceTo(pt2._pt) < pipeLenTor)
                        {
                            continue;
                        }
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
            ref FireHydrantSystemIn fireHydrantSysIn, ref List<Point3dEx> pointList, ref List<Line> lineList, 
            ref List<Line> valveList, ThCADCoreNTSSpatialIndex casingSpatialIndex)
        {
            foreach (var v in valveDB)
            {
                Point3dEx pt1;
                Point3dEx pt2;
                Line line1;
                Point3dCollection rect;
                if (v is BlockReference)
                {
                    var br = v as BlockReference;
                    var valve = new ThFireHydrantValve(br);
                    line1 = valve.GetLine(fireHydrantSysIn.ValveIsBkReference);
                    
                    pt1 = new Point3dEx(line1.StartPoint);
                    pt2 = new Point3dEx(line1.EndPoint);
                    rect = valve.GetRect();
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
                    rect = GetRect(boundPt1, boundPt2);
                    var bdpt1 = baseLine.GetClosestPointTo(boundPt1, true);
                    var bdpt2 = baseLine.GetClosestPointTo(boundPt2, true);

                    pt1 = new Point3dEx(bdpt1);
                    pt2 = new Point3dEx(bdpt2);
                    line1 = new Line(bdpt1, bdpt2);
                }
                pointList.Add(pt1);
                pointList.Add(pt2);
                valveList.Add(line1);
                ThPointCountService.AddPoint(ref fireHydrantSysIn, ref pt1, ref pt2, "Valve");

                
                var casingObj = casingSpatialIndex.SelectCrossingPolygon(rect);
                casingSpatialIndex.Update(new DBObjectCollection(), casingObj);
                if (casingObj.Count == 1)
                {
                    var cPt1 = (casingObj[0] as Entity).GeometricExtents.MaxPoint;
                    var cPt2 = (casingObj[0] as Entity).GeometricExtents.MinPoint;
                    var casingPt = General.GetMidPt(cPt1, cPt2);
                    if (casingPt.DistanceTo(pt1._pt) < casingPt.DistanceTo(pt2._pt))
                    {
                        fireHydrantSysIn.PtTypeDic[pt1] = "Valve-casing";
                    }
                    else
                    {
                        fireHydrantSysIn.PtTypeDic[pt2] = "Valve-casing";
                    }
                }
                if(casingObj.Count > 1)
                {
                    var pt = General.GetMidPt(pt1._pt, pt2._pt);
                    double minDist = 9999;
                    Point3d targetPt = new Point3d();
                    foreach (var obj in casingObj)
                    {
                        var cPt1 = (obj as Entity).GeometricExtents.MaxPoint;
                        var cPt2 = (obj as Entity).GeometricExtents.MinPoint;
                        var casingPt = General.GetMidPt(cPt1, cPt2);
                        var dist = pt.DistanceTo(casingPt);
                        if(dist < minDist)
                        {
                            minDist = dist;
                            targetPt = casingPt;
                        }
                    }
                    if (targetPt.DistanceTo(pt1._pt) < targetPt.DistanceTo(pt2._pt))
                    {
                        fireHydrantSysIn.PtTypeDic[pt1] = "Valve-casing";
                    }
                    else
                    {
                        fireHydrantSysIn.PtTypeDic[pt2] = "Valve-casing";
                    }
                }
            }
            PipeLineSplit(ref lineList, valveList);
        }

        public static Point3dCollection GetRect(Point3d pt1, Point3d pt2)
        {
            double gap = 300;
            var pts = new Point3d[5];
            pts[0] = new Point3d(pt2.X - gap, pt1.Y + gap, 0);
            pts[1] = new Point3d(pt1.X + gap, pt1.Y + gap, 0); 
            pts[2] = new Point3d(pt1.X + gap, pt2.Y - gap, 0);
            pts[3] = new Point3d(pt2.X - gap, pt2.Y - gap, 0); 
            pts[4] = pts[0];
            return new Point3dCollection(pts);
        }

        public static void PipeLineSplit(ref List<Line> pipeLineList, List<Point3dEx> pointList, double toleranceForPointIsLineTerm = 10, double toleranceForPointOnLine = 1.0)
        {
            foreach (var pt in pointList)//管线打断
            {
                var line = PointCompute.PointInLine(pt._pt, pipeLineList, toleranceForPointIsLineTerm, toleranceForPointOnLine);
                if (!PointCompute.IsNullLine(line))
                {
                    if (!PointCompute.PointIsLineTerm(pt._pt, line, toleranceForPointIsLineTerm))
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
                if(pipeLine.StartPoint.DistanceTo(new Point3d(0,0,0)) < 10)
                {
                    continue;
                }
                if (pipeLine.EndPoint.DistanceTo(new Point3d(0, 0, 0)) < 10)
                {
                    continue;
                }
                LineOnLine.LineSplit(valve, pipeLine, ref pipeLineList);
            }
        }
    }
}
