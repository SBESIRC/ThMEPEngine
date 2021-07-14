using Autodesk.AutoCAD.DatabaseServices;
using GeometryExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Model
{
    class PipeLine
    {
        public static void AddPipeLine(DBObjectCollection dbObjs, ref FireHydrantSystemIn fireHydrantSysIn, ref List<Point3dEx> pointList, ref List<Line> lineList)
        {
            foreach (var f in dbObjs)
            {
                if (f is Line)
                {
                    var fl = f as Line;
                    var pt1 = new Point3dEx(fl.StartPoint.X, fl.StartPoint.Y, 0);
                    var pt2 = new Point3dEx(fl.EndPoint.X, fl.EndPoint.Y, 0);
                    pointList.Add(pt1);
                    pointList.Add(pt2);
                    ThPointCountService.AddPoint(ref fireHydrantSysIn, ref pt1, ref pt2, "MainLoop");
                    lineList.Add(new Line(pt1._pt, pt2._pt));
                }
                if (f is Polyline)
                {
                    var fl = f as Polyline;
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

        public static void AddValveLine(DBObjectCollection valveDB, ref FireHydrantSystemIn fireHydrantSysIn, 
            ref List<Point3dEx> pointList, ref List<Line> lineList, ref List<Line> valveList)
        {
            foreach (var v in valveDB)
            {
                if(v is BlockReference)//块参照
                {
                    var br = v as BlockReference;
                    var valve = new ThFireHydrantValve(br);
                    var line1 = valve.GetLine(fireHydrantSysIn.ValveIsBkReference);
                    if(LineOnLine.LineIsOnLineList(line1, lineList))//阀门在管段线上
                    {
                        var pt1 = new Point3dEx(line1.StartPoint);
                        var pt2 = new Point3dEx(line1.EndPoint);
                        pointList.Add(pt1);
                        pointList.Add(pt2);
                        valveList.Add(line1);
                        ThPointCountService.AddPoint(ref fireHydrantSysIn, ref pt1, ref pt2, "Valve");
                    }
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
                    if (LineOnLine.LineIsOnLineList(new Line(pt1._pt, pt2._pt), lineList))//阀门在管段线上
                    {
                        pointList.Add(pt1);
                        pointList.Add(pt2);
                        valveList.Add(new Line(bdpt1, bdpt2));
                        ThPointCountService.AddPoint(ref fireHydrantSysIn, ref pt1, ref pt2, "Valve");
                    }    
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
