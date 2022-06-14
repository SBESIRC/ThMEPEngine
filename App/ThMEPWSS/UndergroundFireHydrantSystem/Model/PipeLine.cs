using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeometryExtensions;
using NFox.Cad;
using System.Collections.Generic;
using ThCADCore.NTS;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Model
{
    public class PipeLine
    {
        public static bool HasSitong(FireHydrantSystemIn fireHydrantSysIn)
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

        public static void AddPipeLine(DBObjectCollection dbObjs, FireHydrantSystemIn fireHydrantSysIn, 
            List<Point3dEx> pointList, List<Line> lineList)
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

        public static void AddValveLine(DBObjectCollection valveDB, FireHydrantSystemIn fireHydrantSysIn,  
            List<Line> lineList, List<Line> valveList, DBObjectCollection casingPts)
        {
            var valvePts = new List<Point3dEx>();
            foreach (var v in valveDB)
            {
                Point3dEx valvePt;
                if (v is BlockReference br)
                {
                    valvePt = new Point3dEx(br.GetRect().GetCentroidPoint());
                }
                else
                {
                    var objs = new DBObjectCollection();

                    (v as Entity).Explode(objs);
                    var bkr = objs[0] as BlockReference;
                    valvePt = new Point3dEx(bkr.GetRect().GetCentroidPoint());
                }
                valvePts.Add(valvePt);
            }
            var newPts = PipeLineSplit(lineList, valvePts);
            foreach(var pt in newPts)
            {
                fireHydrantSysIn.PtTypeDic.Add(pt, "DieValve");
            }

            newPts.Clear();
            newPts = PipeLineSplit(lineList, fireHydrantSysIn.GateValves);
            foreach (var pt in newPts)
            {
                if(fireHydrantSysIn.PtTypeDic.ContainsKey(pt))
                {
                    fireHydrantSysIn.PtTypeDic[pt] = "GateValve";
                }
                else
                {
                    fireHydrantSysIn.PtTypeDic.Add(pt, "GateValve");
                }
            }

            valvePts.Clear();
            newPts.Clear();
            foreach (var casing in casingPts)
            {
                var pt = new Point3dEx((casing as BlockReference).GetRect().GetCentroidPoint());
                valvePts.Add(pt);
            }
            newPts = PipeLineSplit(lineList, valvePts);
            foreach (var pt in newPts)
            {
                fireHydrantSysIn.PtTypeDic.Add(pt, "Casing");
            }
            ;
        }

        public static void PipeLineSplit(List<Line> pipeLineList, List<Point3dEx> pointList, 
            double toleranceForPointIsLineTerm = 10, double toleranceForPointOnLine = 1.0)
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

        public static List<Point3dEx> PipeLineSplit(List<Line> lines, List<Point3dEx> pts)//阀门点分割线段
        {
            var newPts = new List<Point3dEx>();
            foreach(var pt in pts)
            {
                var line = PointCompute.PointInLine2(pt._pt, lines);
                if(line is not null)
                {
                    lines.Remove(line);//删除该直线
                    var newPt = line.GetClosestPointTo(pt._pt, false);//找到线上最近点
                    var lList = PointCompute.CreateNewLine(newPt, line);//利用最近点对线进行打断

                    newPts.Add(new Point3dEx(newPt));//新生成的点加入点集
                    foreach (var l in lList)
                    {
                        lines.Add(l);//新生成的直线添加至列表
                    }
                }
            }
            return newPts;
        }

        public static List<Point3dEx> PipeLineSplit(List<Line> lines, List<Point3d> pts)//阀门点分割线段
        {
            var newPts = new List<Point3dEx>();
            foreach (var pt in pts)
            {
                var line = PointCompute.PointInLine2(pt, lines);
                if (line is not null)
                {
                    lines.Remove(line);//删除该直线
                    var newPt = line.GetClosestPointTo(pt, false);//找到线上最近点
                    var lList = PointCompute.CreateNewLine(newPt, line);//利用最近点对线进行打断

                    newPts.Add(new Point3dEx(newPt));//新生成的点加入点集
                    foreach (var l in lList)
                    {
                        lines.Add(l);//新生成的直线添加至列表
                    }
                }
                else
                {
                    newPts.Add(new Point3dEx(pt));
                }
            }
            return newPts;
        }
    }
}
