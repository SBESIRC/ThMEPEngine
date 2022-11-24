using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThCADCore.NTS;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.General;


namespace ThMEPWSS.UndergroundFireHydrantSystem.Model
{
    public class PipeLine
    {
        public static void AddPipeLine(DBObjectCollection dbObjs, FireHydrantSystemIn fireHydrantSysIn, 
            List<Point3dEx> pointList, List<Line> lineList)
        {
            double tolerance = 5.0;
            foreach(var obj in dbObjs)
            {
                if(obj is Line line)
                {
                    var pt1 = line.StartPoint.Point3dZ0();
                    var pt2 = line.EndPoint.Point3dZ0();
                    if(pt1.DistanceTo(pt2)>= tolerance)
                    {
                        lineList.Add(new Line(pt1, pt2));
                    }
                }
                if(obj is Polyline pline)
                {
                    lineList.AddItems(pline.Pline2Lines());
                }
            }
            lineList = PipeLineList.CleanLaneLines3(lineList);
            foreach (var line in lineList)
            {
                var pt1 = new Point3dEx(line.StartPoint);
                var pt2 = new Point3dEx(line.EndPoint);
                pointList.Add(pt1);
                pointList.Add(pt2);
                ThPointCountService.AddPoint(fireHydrantSysIn, ref pt1, ref pt2, "MainLoop");
            }
        }

        public static void AddValveLine(DBObjectCollection valveDB, FireHydrantSystemIn fireHydrantSysIn,  
            List<Line> lineList, DBObjectCollection casingPts)
        {
            var valvePts = new List<Point3dEx>();
            foreach (var v in valveDB)
            {
                Point3dEx valvePt;
                if (v is BlockReference br)
                {
                    valvePt = new Point3dEx(br.Position);
                }
                else
                {
                    var objs = new DBObjectCollection();

                    (v as Entity).Explode(objs);
                    var bkr = objs[0] as BlockReference;
                    valvePt = new Point3dEx(bkr.Position);
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
