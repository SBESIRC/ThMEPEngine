using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    class GetPipePart
    {
        public static Point3d GetMainLoopPoint(FireHydrantSystemOut fireHydrantSysOut, int i, Point3d stPt, 
            List<Point3dEx> rstPath, FireHydrantSystemIn fireHydrantSysIn, double pipeLength)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var pt1 = stPt;
                var pt2 = new Point3d();
                
                if (i != 0)//对于多主环点的情况，我们只需要绘制第一对即可
                {
                    var type = fireHydrantSysIn.PtTypeDic[rstPath[i - 1]];
                    if (type.Equals("MainLoop") || type.Equals("Branch"))
                    {
                        return stPt;
                    }
                    if(type.Contains("Valve") || type.Contains("Casing"))
                    {
                        return stPt;
                    }
                }
                pt2 = new Point3d(stPt.X + pipeLength, stPt.Y, 0);

                var nextType = fireHydrantSysIn.PtTypeDic[rstPath[i + 1]];
                if (nextType.Contains("Valve") || nextType.Contains("Casing"))
                {
                    pt2 = new Point3d(stPt.X + pipeLength - 640, stPt.Y, 0);
                }
                fireHydrantSysOut.LoopLine.Add(new Line(pt1, pt2));
                stPt = pt2;

                AddDN(ref fireHydrantSysOut, i, pt1, fireHydrantSysIn, rstPath);
                return stPt;
            }
        }

        public static Point3d GetSubLoopPoint(FireHydrantSystemOut fireHydrantSysOut, bool IsSubLoop, int i,
            Point3dEx pt, Point3d stPt, List<Point3dEx> rstPath, FireHydrantSystemIn fireHydrantSysIn, double pipeGap, double pipeLength)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var pt1 = stPt;
                var pt2 = new Point3d(stPt.X, stPt.Y - pipeGap, 0);
                var pt3 = new Point3d(pt2.X + pipeLength / 2, pt2.Y, 0);

                var pt4 = new Point3d(stPt.X + pipeLength, stPt.Y, 0);
                var nextType = fireHydrantSysIn.PtTypeDic[rstPath[i + 1]];

                if (nextType.Contains("Valve") || nextType.Contains("Casing"))
                {
                    pt4 = new Point3d(stPt.X + pipeLength - 640, stPt.Y, 0);
                }
                if (i != 0 || !IsSubLoop)
                {
                    var textMark = ThTextSet.ThText(new Point3d(pt3.X + 120, pt3.Y - 180, 0), fireHydrantSysIn.MarkList[pt]);

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

        public static Point3d GetBranchPoint(FireHydrantSystemOut fireHydrantSysOut, int i, Point3dEx pt, Point3d stPt,
            List<Point3dEx> rstPath, double pipeGap, double pipeLength, FireHydrantSystemIn fireHydrantSysIn)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var pt1 = stPt;
                var pt2 = new Point3d(stPt.X, stPt.Y - pipeGap, 0);
                var pt4 = new Point3d(stPt.X + pipeLength, stPt.Y, 0);
                var nextType = "";
                if (fireHydrantSysIn.PtTypeDic.ContainsKey(rstPath[i + 1]))
                    nextType = fireHydrantSysIn.PtTypeDic[rstPath[i + 1]];
                if (nextType.Contains("Valve") || nextType.Contains("Casing"))
                {
                    pt4 = new Point3d(stPt.X + pipeLength - 640, stPt.Y, 0);
                }
                if (fireHydrantSysOut.BranchDrawDic.ContainsKey(pt))
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

        public static Point3d GetDieValvePoint(FireHydrantSystemOut fireHydrantSysOut, Point3d stPt)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                fireHydrantSysOut.DieValve.Add(stPt);
                var pt1 = new Point3d(stPt.X + 240, stPt.Y, 0);
                var pt2 = pt1.OffsetX(400);
                fireHydrantSysOut.LoopLine.Add(new Line(pt1, pt2));
                return pt2;
            } 
        }

        public static Point3d GetGateValvePoint(FireHydrantSystemOut fireHydrantSysOut, Point3d stPt)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                fireHydrantSysOut.GateValve.Add(stPt);
                var pt1 = new Point3d(stPt.X + 300, stPt.Y, 0);
                var pt2 = pt1.OffsetX(400);
                fireHydrantSysOut.LoopLine.Add(new Line(pt1, pt2));
                return pt2;
            }
        }


        public static Point3d GetCasingPoint(FireHydrantSystemOut fireHydrantSysOut, Point3d stPt)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                fireHydrantSysOut.Casing.Add(stPt);
                var pt1 = new Point3d(stPt.X + 200, stPt.Y, 0);
                var pt2 = pt1.OffsetX(400);
                fireHydrantSysOut.LoopLine.Add(new Line(stPt, pt2));
                return pt2;
            }
        }

        public static void AddDN(ref FireHydrantSystemOut fireHydrantSysOut, int i, Point3d pt1, 
            FireHydrantSystemIn fireHydrantSysIn, List<Point3dEx> rstPath)
        {
            var pipeLine = new LineSegEx(rstPath[i]._pt, rstPath[i + 1]._pt);
            var position = new Point3d(pt1.X + 350, pt1.Y + 100, 0);
            if (fireHydrantSysIn.PtDNDic.ContainsKey(pipeLine))
            {

                var dn = ThTextSet.ThText(position, fireHydrantSysIn.PtDNDic[pipeLine]);
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

        public static void GetMainLoopDetial(FireHydrantSystemOut fireHydrantSysOut, Point3d stPt, Point3d ptStart)
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

        public static void GetSubLoopDetial(ref FireHydrantSystemOut fireHydrantSysOut, Point3d stPt, Point3d ptStart, 
            List<Point3dEx> rstPath, Dictionary<Point3dEx, string> markList)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                string str1 = "";
                string str2 = "";
                if(!markList.ContainsKey(rstPath.First()))
                {
                    foreach(var pt in markList.Keys)
                    {
                        if(pt._pt.DistanceTo(rstPath.First()._pt) < 5)
                        {
                            str1 = markList[pt];
                        }
                    }
                }
                else
                {
                    str1 = markList[rstPath.First()];
                }
                if (!markList.ContainsKey(rstPath.Last()))
                {
                    foreach (var pt in markList.Keys)
                    {
                        if (pt._pt.DistanceTo(rstPath.Last()._pt) < 5)
                        {
                            str2 = markList[pt];
                        }
                    }
                }
                else
                {
                    str2 = markList[rstPath.Last()];
                }
                var textMark1 = ThTextSet.ThText(new Point3d(ptStart.X - 340, ptStart.Y - 180, 0), str1);
                var textMark2 = ThTextSet.ThText(new Point3d(stPt.X + 120, stPt.Y - 180, 0), str2);

                fireHydrantSysOut.TextList.Add(textMark1);
                fireHydrantSysOut.TextList.Add(textMark2);
                fireHydrantSysOut.PipeInterrupted.Add(ptStart, Math.PI);
                fireHydrantSysOut.PipeInterrupted.Add(stPt, Math.PI);
            }      
        }
    }
}
