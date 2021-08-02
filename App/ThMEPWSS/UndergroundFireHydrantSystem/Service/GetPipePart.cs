using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    class GetPipePart
    {
        public static Point3d GetMainLoopPoint(ref FireHydrantSystemOut fireHydrantSysOut, int i, Point3d stPt, 
            List<Point3dEx> rstPath, FireHydrantSystemIn fireHydrantSysIn, double valveWidth, double pipeLength)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var pt1 = stPt;
                var pt2 = new Point3d();
                if(i != 0)//对于多主环点的情况，我们只需要绘制第一对即可
                {
                    if(fireHydrantSysIn.ptTypeDic[rstPath[i-1]].Equals("MainLoop") || fireHydrantSysIn.ptTypeDic[rstPath[i - 1]].Equals("Branch"))
                    {
                        return stPt;
                    }
                    if(fireHydrantSysIn.ptTypeDic[rstPath[i - 1]].Equals("Valve"))
                    {
                        return stPt;
                    }
                }
                pt2 = new Point3d(stPt.X + pipeLength, stPt.Y, 0);
                if (i != rstPath.Count -1)
                {
                    if(fireHydrantSysIn.ptTypeDic[rstPath[i + 1]].Equals("Valve"))
                    {
                        pt2 = new Point3d(stPt.X + pipeLength - 240 - 400, stPt.Y, 0);
                    }
                }
                
                fireHydrantSysOut.LoopLine.Add(new Line(pt1, pt2));
                stPt = pt2;

    
                AddDN(ref fireHydrantSysOut, i, pt1, fireHydrantSysIn, rstPath);
                return stPt;
            }
        }

        public static Point3d GetSubLoopPoint(ref FireHydrantSystemOut fireHydrantSysOut, bool IsSubLoop, int i,
            Point3dEx pt, Point3d stPt, List<Point3dEx> rstPath, FireHydrantSystemIn fireHydrantSysIn, double pipeGap, double pipeLength)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var pt1 = stPt;
                var pt2 = new Point3d(stPt.X, stPt.Y - pipeGap, 0);
                var pt3 = new Point3d(pt2.X + pipeLength / 2, pt2.Y, 0);
                var pt4 = new Point3d(stPt.X + pipeLength, stPt.Y, 0);

                if (i != 0 || !IsSubLoop)
                {
                    var textMark = ThTextSet.ThText(new Point3d(pt3.X + 120, pt3.Y - 180, 0), fireHydrantSysIn.markList[pt]);

                    fireHydrantSysOut.LoopLine.Add(new Line(pt1, pt2));
                    fireHydrantSysOut.LoopLine.Add(new Line(pt2, pt3));
                    fireHydrantSysOut.PipeInterrupted.Add(pt3, Math.PI);
                    fireHydrantSysOut.TextList.Add(textMark);
                }
                fireHydrantSysOut.LoopLine.Add(new Line(pt1, pt4));
                stPt = pt4;
                AddDN(ref fireHydrantSysOut, i, pt1, fireHydrantSysIn, rstPath);
                return stPt;
            }   
        }

        public static Point3d GetBranchPoint(ref FireHydrantSystemOut fireHydrantSysOut, int i, Point3dEx pt, Point3d stPt,
            List<Point3dEx> rstPath, double pipeGap, double pipeLength, FireHydrantSystemIn fireHydrantSysIn)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var pt1 = stPt;
                var pt2 = new Point3d(stPt.X, stPt.Y - pipeGap, 0);
                var pt4 = new Point3d(stPt.X + pipeLength, stPt.Y, 0);
                if(fireHydrantSysOut.BranchDrawDic.ContainsKey(pt))
                {
                    return stPt;
                }
                fireHydrantSysOut.BranchDrawDic.Add(pt, new Point3dEx(pt2));
                fireHydrantSysOut.LoopLine.Add(new Line(pt1, pt2));
                fireHydrantSysOut.LoopLine.Add(new Line(pt1, pt4));
                stPt = pt4;

                AddDN(ref fireHydrantSysOut, i, pt1, fireHydrantSysIn, rstPath);


                return stPt;
            }  
        }

        public static Point3d GetValvePoint(Point3dEx valve, ref FireHydrantSystemOut fireHydrantSysOut, Point3d stPt, ref bool valveCheck,
            FireHydrantSystemIn fireHydrantSysIn)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                if(fireHydrantSysIn.ptTypeDic[valve].Contains("casing"))
                {
                    fireHydrantSysOut.IsCasing.Add(new Point3d(stPt.X + 100, stPt.Y, 0));
                }
                if (valveCheck)
                {
                    var flag = false;
                    fireHydrantSysOut.Valve.Add(stPt);
                    foreach(var pt in fireHydrantSysIn.GateValves)
                    {
                        if(valve._pt.DistanceTo(pt) < 250)
                        {
                            fireHydrantSysOut.IsGateValve.Add(stPt);
                            flag = true;
                            break;
                        }
                    }
                    if(flag)
                    {
                        stPt = new Point3d(stPt.X + 300, stPt.Y, 0);
                    }
                    else
                    {
                        stPt = new Point3d(stPt.X + 240, stPt.Y, 0);

                    }
                    valveCheck = false;
                }
                else
                {
                    var pt1 = stPt;
                    var pt2 = new Point3d(stPt.X + 400, stPt.Y, 0);
                    fireHydrantSysOut.LoopLine.Add(new Line(pt1, pt2));
                    stPt = pt2;
                    valveCheck = true;
                }
                return stPt;
            } 
        }

        public static void AddDN(ref FireHydrantSystemOut fireHydrantSysOut, int i, Point3d pt1, FireHydrantSystemIn fireHydrantSysIn, List<Point3dEx> rstPath)
        {
            var pipeLine = new LineSegEx(rstPath[i]._pt, rstPath[i + 1]._pt);
            var position = new Point3d(pt1.X + 350, pt1.Y + 100, 0);
            if (fireHydrantSysIn.ptDNDic.ContainsKey(pipeLine))
            {

                var dn = ThTextSet.ThText(position, fireHydrantSysIn.ptDNDic[pipeLine]);
                fireHydrantSysOut.DNList.Add(dn);
            }
            else
            {
                var tolerance = 20;
                foreach (var slash in fireHydrantSysIn.SlashDic.Keys)
                {
                    if (slash._pt.DistanceTo(new Line(rstPath[i]._pt, rstPath[i + 1]._pt).GetClosestPointTo(slash._pt, false)) < tolerance)
                    {
                        var dn = ThTextSet.ThText(position, fireHydrantSysIn.SlashDic[slash]);
                        fireHydrantSysOut.DNList.Add(dn);
                        break;
                    }
                }
            }
        }
        public static void GetMainLoopDetial(ref FireHydrantSystemOut fireHydrantSysOut, Point3d stPt, Point3d ptStart)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var ptRightUp = new Point3d(stPt.X, stPt.Y + 500, 0);
                var ptEnd = new Point3d(ptStart.X, ptStart.Y + 500, 0);
                var line1 = new Line(stPt, ptRightUp);
                var line2 = new Line(ptRightUp, ptEnd);
                line1.LayerId = DbHelper.GetLayerId("0");

                fireHydrantSysOut.LoopLine.Add(line1);
                fireHydrantSysOut.LoopLine.Add(line2);

                var textPipeMark1 = ThTextSet.ThText(new Point3d(ptStart.X - 630, ptStart.Y - 200, 0), "环管");
                var textPipeMark2 = ThTextSet.ThText(new Point3d(ptEnd.X - 630, ptEnd.Y - 200, 0), "环管");
                var textPipeMark3 = ThTextSet.ThText(new Point3d(stPt.X + 120, stPt.Y - 200, 0), "环管");

                fireHydrantSysOut.TextList.Add(textPipeMark1);
                fireHydrantSysOut.TextList.Add(textPipeMark2);
                fireHydrantSysOut.TextList.Add(textPipeMark3);

                fireHydrantSysOut.PipeInterrupted.Add(ptStart, Math.PI);
                fireHydrantSysOut.PipeInterrupted.Add(ptEnd, Math.PI);
            }
        }

        public static void GetSubLoopDetial(ref FireHydrantSystemOut fireHydrantSysOut, Point3d stPt, Point3d ptStart, List<Point3dEx> rstPath, Dictionary<Point3dEx, string> markList)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var textMark1 = ThTextSet.ThText(new Point3d(ptStart.X - 340, ptStart.Y - 180, 0), markList[rstPath.First()]);
                var textMark2 = ThTextSet.ThText(new Point3d(stPt.X + 120, stPt.Y - 180, 0), markList[rstPath.Last()]);

                fireHydrantSysOut.TextList.Add(textMark1);
                fireHydrantSysOut.TextList.Add(textMark2);
                fireHydrantSysOut.PipeInterrupted.Add(ptStart, Math.PI);
                fireHydrantSysOut.PipeInterrupted.Add(stPt, Math.PI);
            }      
        }
    }
}
