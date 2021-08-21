﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.Command;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.UndergroundFireHydrantSystem.Method;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    class GetFireHydrantPipe
    {
        public static void GetMainLoop(ref FireHydrantSystemOut fireHydrantSysOut, List<List<Point3dEx>> mainPathList,
            FireHydrantSystemIn fireHydrantSysIn, Dictionary<Point3dEx, List<Point3dEx>> branchDic)
        {
            var index = 0;
            var stPt1 = fireHydrantSysOut.InsertPoint;
            foreach (var rstPath in mainPathList)
            {
                var stPt = new Point3d(stPt1.X, stPt1.Y - 12000 * index, 0);
                var ptStart = new Point3d(stPt.X, stPt.Y, 0);
                index += 1;
                double pipeGap = 400;
                double valveWidth = 240;
                bool valveCheck = true;

                for (int i = 0; i < rstPath.Count - 1; i++)
                {
                    var pt = rstPath[i];
                    double pipeLength = fireHydrantSysIn.PipeWidth;
                    if (branchDic.ContainsKey(pt))
                    {
                        var tpts = branchDic[pt];
                        if (tpts.Count == 2)
                        {
                            if(!fireHydrantSysIn.TermPointDic.ContainsKey(tpts[0]))
                            {
                                continue;
                            }
                            if (!fireHydrantSysIn.TermPointDic.ContainsKey(tpts[1]))
                            {
                                continue;
                            }
                            var str1 = fireHydrantSysIn.TermPointDic[tpts[0]].PipeNumber;
                            var str2 = fireHydrantSysIn.TermPointDic[tpts[1]].PipeNumber;
                            if (!(str1.StartsWith("X") || str1.StartsWith("B")) &&
                                !(str2.StartsWith("X") || str2.StartsWith("B")))
                            {
                                pipeLength = 2 * fireHydrantSysIn.PipeWidth;
                            }
                        }
                        if (tpts.Count == 3)
                        {
                            var str1 = fireHydrantSysIn.TermPointDic[tpts[0]].PipeNumber;
                            var str2 = fireHydrantSysIn.TermPointDic[tpts[1]].PipeNumber;
                            var str3 = fireHydrantSysIn.TermPointDic[tpts[2]].PipeNumber;
                            if (!(str1.StartsWith("X") || str1.StartsWith("B")) &&
                                !(str2.StartsWith("X") || str2.StartsWith("B")) &&
                                !(str3.StartsWith("X") || str3.StartsWith("B")))
                            {
                                pipeLength = 3 * fireHydrantSysIn.PipeWidth;
                            }
                            else
                            {
                                pipeLength = 2 * fireHydrantSysIn.PipeWidth;
                            }
                        }
                    }

                    if (fireHydrantSysIn.PtTypeDic[pt].Equals("MainLoop"))
                    {
                        stPt = GetPipePart.GetMainLoopPoint(ref fireHydrantSysOut, i, stPt, rstPath, fireHydrantSysIn, valveWidth, pipeLength);
                        continue;
                    }
                    if (fireHydrantSysIn.PtTypeDic[pt].Equals("SubLoop"))
                    {
                        bool isSubLoop = false;
                        stPt = GetPipePart.GetSubLoopPoint(ref fireHydrantSysOut, isSubLoop, i, pt, stPt, rstPath, fireHydrantSysIn, pipeGap, pipeLength);
                        continue;
                    }
                    if (fireHydrantSysIn.PtTypeDic[pt].Equals("Branch"))
                    {
                        stPt = GetPipePart.GetBranchPoint(ref fireHydrantSysOut, i, pt, stPt, rstPath, pipeGap, pipeLength, fireHydrantSysIn);
                        continue;
                    }
                    if (fireHydrantSysIn.PtTypeDic[pt].Contains("Valve"))
                    {
                        stPt = GetPipePart.GetValvePoint(pt, ref fireHydrantSysOut, stPt, ref valveCheck, fireHydrantSysIn);
                        continue;
                    }
                }
                GetPipePart.GetMainLoopDetial(ref fireHydrantSysOut, stPt, ptStart);
            }
            fireHydrantSysOut.InsertPoint = new Point3d(stPt1.X, stPt1.Y - 12000 * index, 0);
        }

        public static void GetSubLoop(ref FireHydrantSystemOut fireHydrantSysOut, List<List<Point3dEx>> subPathList, FireHydrantSystemIn fireHydrantSysIn,
            Dictionary<Point3dEx, List<Point3dEx>> branchDic)
        {
            var index = 0;
            foreach (var rstPath in subPathList)
            {
                var stPt1 = fireHydrantSysOut.InsertPoint;
                var stPt = new Point3d(stPt1.X, stPt1.Y - fireHydrantSysIn.FloorHeight * index, 0);
                index += 1;
                var ptStart = new Point3d(stPt.X, stPt.Y, 0);
                var pipeGap = 400;
                var valveWidth = 240;
                var valveCheck = true;

                for (int i = 0; i < rstPath.Count - 1; i++)
                {
                    var pt = rstPath[i];
                    double pipeLength = fireHydrantSysIn.PipeWidth;
                    if (branchDic.ContainsKey(pt))
                    {
                        if (branchDic[pt].Count == 3)
                        {
                            pipeLength = 2 * fireHydrantSysIn.PipeWidth;
                        }
                    }
                    if (fireHydrantSysIn.PtTypeDic[pt].Equals("MainLoop"))
                    {
                        stPt = GetPipePart.GetMainLoopPoint(ref fireHydrantSysOut, i, stPt, rstPath, fireHydrantSysIn, valveWidth, pipeLength);
                        continue;
                    }

                    if (fireHydrantSysIn.PtTypeDic[pt].Equals("SubLoop"))
                    {
                        bool isSubLoop = true;
                        stPt = GetPipePart.GetSubLoopPoint(ref fireHydrantSysOut, isSubLoop, i, pt, stPt, rstPath, fireHydrantSysIn, pipeGap, pipeLength);
                        continue;
                    }

                    if (fireHydrantSysIn.PtTypeDic[pt].Equals("Branch"))
                    {
                        stPt = GetPipePart.GetBranchPoint(ref fireHydrantSysOut, i, pt, stPt, rstPath, pipeGap, pipeLength, fireHydrantSysIn);
                        continue;
                    }

                    if (fireHydrantSysIn.PtTypeDic[pt].Contains("Valve"))
                    {
                        stPt = GetPipePart.GetValvePoint(pt, ref fireHydrantSysOut, stPt, ref valveCheck, fireHydrantSysIn);
                        continue;
                    }
                }
                GetPipePart.GetSubLoopDetial(ref fireHydrantSysOut, stPt, ptStart, rstPath, fireHydrantSysIn.MarkList);
            }
        }

        public static void GetBranch(ref FireHydrantSystemOut fireHydrantSysOut, Dictionary<Point3dEx, List<Point3dEx>> branchDic,
            Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn)
        {
            var list = new List<Point3d>();
            foreach (var pt in fireHydrantSysIn.TermPointDic.Keys)
            {
                list.Add(pt._pt);
            }
            foreach (var pt in branchDic.Keys)//对于支路的每一个起始点
            {
                if (!fireHydrantSysOut.BranchDrawDic.ContainsKey(pt))
                {
                    continue;
                }
                
                if (branchDic[pt].Count == 1)//单支路
                {
                    var stPt = fireHydrantSysOut.BranchDrawDic[pt];
                    var x = branchDic[pt];
                    if (fireHydrantSysIn.TermPointDic.ContainsKey(branchDic[pt][0]))
                    {
                        if (fireHydrantSysIn.TermPointDic[branchDic[pt].First()].Type.Equals(1))//终点是类型1，消火栓
                        {
                            string pipeNumber = fireHydrantSysIn.TermPointDic[branchDic[pt][0]].PipeNumber;//立管标号
                            if (pipeNumber.IsCurrentFloor())//消火栓
                            {
                                GetBranchType1(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], ValveDic, fireHydrantSysIn);
                            }
                        }

                        if (fireHydrantSysIn.TermPointDic[branchDic[pt][0]].Type.Equals(2))//终点是类型2，其他区域
                        {
                            GetBranchType2(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], ValveDic, fireHydrantSysIn);
                        }
                        if (fireHydrantSysIn.TermPointDic[branchDic[pt][0]].Type.Equals(3))//终点是类型3，消火栓和其他楼层共用支管
                        {
                            GetBranchType4(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], ValveDic, fireHydrantSysIn);
                        }
                        if (fireHydrantSysIn.TermPointDic[branchDic[pt][0]].Type.Equals(4))//终点是类型4，消火栓和其他楼层共用支管
                        {
                            GetBranchType5(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], ValveDic, fireHydrantSysIn);
                        }
                    }
                }
                if (branchDic[pt].Count == 2)//两个支路
                {
                    int type1Nums = 0;
                    foreach (var tpt in branchDic[pt])
                    {
                        if (!fireHydrantSysIn.TermPointDic.ContainsKey(tpt))
                        {
                            continue;
                        }
                        if (fireHydrantSysIn.TermPointDic[tpt].Type.Equals(1))
                        {
                            type1Nums += 1;
                        }
                    }
                    var stPt = fireHydrantSysOut.BranchDrawDic[pt];
                    if(type1Nums == 1)
                    {
                        if (branchDic[pt].Count != 0)
                        {
                            GetBranchType3(pt, ref fireHydrantSysOut, stPt, branchDic[pt], ValveDic, fireHydrantSysIn);
                        }
                    }
                    else
                    {
                        GetBranchType02(pt, ref fireHydrantSysOut, stPt, branchDic[pt], ValveDic, fireHydrantSysIn);
                    }
                }
                if (branchDic[pt].Count == 3)//三个支路
                {
                    if (!fireHydrantSysOut.BranchDrawDic.ContainsKey(pt))
                    {
                        continue;
                    }
                    var stPt = fireHydrantSysOut.BranchDrawDic[pt];
                    
                    int type1Nums = 0;
                    foreach (var tpt in branchDic[pt])
                    {
                        if (!fireHydrantSysIn.TermPointDic.ContainsKey(tpt))
                        {
                            continue;
                        }
                        if (fireHydrantSysIn.TermPointDic[tpt].Type.Equals(1))
                        {
                            type1Nums += 1;
                        }
                    }
                    if (type1Nums == 0)
                    {
                        GetBranchType03(pt, ref fireHydrantSysOut, stPt, branchDic[pt], ValveDic, fireHydrantSysIn);
                    }
                    if (type1Nums == 1)
                    {
                        GetBranchType12(pt, ref fireHydrantSysOut, stPt, branchDic[pt], ValveDic, fireHydrantSysIn);
                    }
                    if (type1Nums == 2)
                    {
                        GetBranchType21(pt, ref fireHydrantSysOut, stPt, branchDic[pt], ValveDic, fireHydrantSysIn);
                    }
                    if (type1Nums == 3)
                    {
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// 绘制向下的单分支
        /// </summary>
        /// <param name="输出"></param>
        /// <param name="起始点"></param>
        /// <param name="分支路径"></param>
        /// <param name="输入"></param>
        private static void GetBranchType1(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, 
            Point3dEx tpt, Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn, bool flag3 = false)
        {
            var pt1 = stpt._pt;
            var pt4 = pt1.OffsetX(800);
            double floorHeight = fireHydrantSysIn.FloorHeight;
            var pt5 = pt4.OffsetY(-floorHeight * 0.58);
            var pt6 = pt1.OffsetY(-floorHeight * 0.58);
            var lineList = new List<Line>
            {
                new Line(pt4, pt5),
                new Line(pt5, pt6)
            };
            TextGet.GetText(tpt, fireHydrantSysIn, ref fireHydrantSysOut, pt4, pt6);
            ValveGet.GetValve(branchPt, ValveDic, fireHydrantSysIn, ref lineList, ref fireHydrantSysOut, pt1, pt4, flag3);
            foreach (var line in lineList)
            {
                fireHydrantSysOut.LoopLine.Add(line);
            }
        }

        /// <summary>
        /// 绘制向上的单分支
        /// </summary>
        /// <param name="输出"></param>
        /// <param name="起始点"></param>
        /// <param name="分支路径"></param>
        /// <param name="输入"></param>
        private static void GetBranchType2(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, Point3dEx tpt,
            Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn, double type = 2)
        {
            double floorHeight = fireHydrantSysIn.FloorHeight;

            var textWidth = fireHydrantSysIn.TextWidth;
            string pipeNumber1 = "";
            string pipeNumber12 = "";
            if (fireHydrantSysIn.TermPointDic.ContainsKey(tpt))
            {
                pipeNumber1 = fireHydrantSysIn.TermPointDic[tpt].PipeNumber;//立管标号
                pipeNumber12 = fireHydrantSysIn.TermPointDic[tpt].PipeNumber2;//立管标号
            }
            
            var pt1 = stpt._pt;
            double pipeWidth = 800;
            if (type == 3)
            {
                pipeWidth = 600;
            }
            var pt4 = pt1.OffsetX(pipeWidth);
            var pt5 = pt1.OffsetXY(pipeWidth, floorHeight * 0.4);
            var pt6 = pt1.OffsetXY(pipeWidth, floorHeight * 0.5);

            LoopLine.Split(ref fireHydrantSysOut, pt4, pt5);
            var lineList = new List<Line>();
            lineList.Add(new Line(pt4, pt5));
            if(type == 2)
            {
                ValveGet.GetValve(branchPt, ValveDic, fireHydrantSysIn, ref lineList, ref fireHydrantSysOut, pt1, pt4);
            }
            foreach (var line in lineList)
            {
                fireHydrantSysOut.LoopLine.Add(line);
            }
            fireHydrantSysOut.PipeInterrupted.Add(pt5, Math.PI * 3 / 2);
            var textLine1 = ThTextSet.ThTextLine(pt5, pt6);
            var textLine2 = ThTextSet.ThTextLine(pt6, pt6.OffsetX(textWidth-50));
            var p2Flag = false;
            var text = ThTextSet.ThText(pt6, pipeNumber1.Trim());
            if (!pipeNumber1.Trim().Equals(""))
            {
                fireHydrantSysOut.TextLine.Add(textLine1);
                fireHydrantSysOut.TextList.Add(text);
                if (!pipeNumber12?.Equals("")==true)
                {
                    text = ThTextSet.ThText(new Point3d(pt6.X, pt6.Y - 400, 0), pipeNumber12.Trim());
                    double textLength = text.GeometricExtents.MaxPoint.X - text.GeometricExtents.MinPoint.X;
                    if(textLength > textWidth)
                    {
                        var textLine3 = ThTextSet.ThTextLine(pt6, pt6.OffsetX(textLength));
                        fireHydrantSysOut.TextLine.Add(textLine3);
                        p2Flag = true;

                    }

                    fireHydrantSysOut.TextList.Add(text);
                }
                if(!p2Flag)
                {
                    fireHydrantSysOut.TextLine.Add(textLine2);
                }
            }
            var strDN = "DN100";
            if (fireHydrantSysIn.TermDnDic.ContainsKey(tpt))
            {
                strDN = fireHydrantSysIn.TermDnDic[tpt];
            }
            var DN1 = ThTextSet.ThText(new Point3d(pt5.X + 350, pt4.Y + floorHeight * 0.2, 0), Math.PI / 2, strDN);
            fireHydrantSysOut.DNList.Add(DN1);
        }

        /// <summary>
        /// 绘制双分支
        /// </summary>
        /// <param name="输出"></param>
        /// <param name="起始点"></param>
        /// <param name="分支路径"></param>
        /// <param name="输入"></param>
        private static void GetBranchType3(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, List<Point3dEx> tpts,
            Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn)
        {
            string pipeNumber1 = "";
            if (fireHydrantSysIn.TermPointDic.ContainsKey(tpts[0]))
            {
                pipeNumber1 = fireHydrantSysIn.TermPointDic[tpts[0]].PipeNumber;//立管标号 1
            }
            if(pipeNumber1.Equals(""))
            {
                GetBranchType1(branchPt, ref fireHydrantSysOut, stpt, tpts[1], ValveDic, fireHydrantSysIn, true);
                GetBranchType2(branchPt, ref fireHydrantSysOut, stpt, tpts[0], ValveDic, fireHydrantSysIn, 3);
                return;
            }
            if(pipeNumber1.IsCurrentFloor())
            {
                GetBranchType1(branchPt, ref fireHydrantSysOut, stpt, tpts[0], ValveDic, fireHydrantSysIn, true);
                GetBranchType2(branchPt, ref fireHydrantSysOut, stpt, tpts[1], ValveDic, fireHydrantSysIn, 3);
            }
            else
            {
                GetBranchType1(branchPt, ref fireHydrantSysOut, stpt, tpts[1], ValveDic, fireHydrantSysIn, true);
                GetBranchType2(branchPt, ref fireHydrantSysOut, stpt, tpts[0], ValveDic, fireHydrantSysIn, 3);
            }
        }

        /// <summary>
        /// 绘制共用支管的双分支
        /// </summary>
        /// <param name="输出"></param>
        /// <param name="起始点"></param>
        /// <param name="分支路径"></param>
        /// <param name="输入"></param>
        private static void GetBranchType4(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, Point3dEx tpt,
            Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn)
        {
            GetBranchType1(branchPt, ref fireHydrantSysOut, stpt, tpt, ValveDic, fireHydrantSysIn);
            GetBranchType2(branchPt, ref fireHydrantSysOut, stpt, tpt, ValveDic, fireHydrantSysIn, 4);
        }

        /// <summary>
        /// 绘制水泵接合器
        /// </summary>
        /// <param name="输出"></param>
        /// <param name="起始点"></param>
        /// <param name="分支路径"></param>
        /// <param name="输入"></param>
        private static void GetBranchType5(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, Point3dEx tpt,
            Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn)
        {
            double XGap = 800;
            var floorHeight = fireHydrantSysIn.FloorHeight;
            string pipeNumber1 = "";
            if (fireHydrantSysIn.TermPointDic.ContainsKey(tpt))
            {
                pipeNumber1 = fireHydrantSysIn.TermPointDic[tpt].PipeNumber;//立管标号
            }
            var pt1 = stpt._pt;
            var pt4 = new Point3d(pt1.X + XGap, pt1.Y, 0);
            var pt5 = new Point3d(pt4.X, pt4.Y + floorHeight * 0.25, 0);
            var pt51 = new Point3d(pt5.X + 900, pt5.Y, 0);
            var pt6 = new Point3d(pt51.X, pt4.Y + floorHeight * 0.5 + 800, 0);
            var pt7 = new Point3d(pt6.X + floorHeight * 0.3500, pt6.Y, 0);
            LoopLine.Split(ref fireHydrantSysOut, pt4, pt5);
            var lineList = new List<Line>();
            lineList.Add(new Line(pt4, pt5));
            lineList.Add(new Line(pt5, pt51));
            ValveGet.GetValve(branchPt, ValveDic, fireHydrantSysIn, ref lineList, ref fireHydrantSysOut, pt1, pt4);
            foreach (var line in lineList)
            {
                fireHydrantSysOut.LoopLine.Add(line);
            }
            fireHydrantSysOut.PipeInterrupted.Add(pt51, 0);
            var textLine1 = ThTextSet.ThTextLine(pt51, pt6);
            var textLine2 = ThTextSet.ThTextLine(pt6, pt7);


            var text = ThTextSet.ThText(pt6, pipeNumber1);
            var text1 = ThTextSet.ThText(new Point3d(pt6.X, pt6.Y - 500, 0), "详见总图");
            if (!pipeNumber1.Trim().Equals(""))
            {
                fireHydrantSysOut.TextLine.Add(textLine1);
                fireHydrantSysOut.TextLine.Add(textLine2);
                fireHydrantSysOut.TextList.Add(text);
                if (!pipeNumber1.Contains("详见总图"))
                {
                    fireHydrantSysOut.TextList.Add(text1);
                }
            }
            var DN1 = ThTextSet.ThText(new Point3d(pt1.X, pt1.Y - 500, 0), 0, "DN100");
            fireHydrantSysOut.DNList.Add(DN1);
        }

        /// <summary>
        /// 绘制2根立管
        /// </summary>
        /// <param name="输出"></param>
        /// <param name="起始点"></param>
        /// <param name="分支路径"></param>
        /// <param name="输入"></param>
        private static void GetBranchType02(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, List<Point3dEx> tpts,
            Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn)
        {
            var pt1 = tpts[0];
            var pt2 = tpts[1];
            string pipeNumber1 = "";
            string pipeNumber12 = "";
            if (fireHydrantSysIn.TermPointDic.ContainsKey(tpts[0]))
            {
                pipeNumber1 = fireHydrantSysIn.TermPointDic[tpts[0]].PipeNumber;//立管标号
                pipeNumber12 = fireHydrantSysIn.TermPointDic[tpts[0]].PipeNumber2;//立管标号
            }
            if (pipeNumber12?.Equals("") == false)
            {
                pt1 = tpts[1];
                pt2 = tpts[0];
            }
            var stpt1 = new Point3dEx();
            if(fireHydrantSysIn.TermPointDic.ContainsKey(pt1))
            {
                if (!fireHydrantSysIn.TermPointDic[pt1].PipeNumber.Equals(""))
                {
                    double XGap = 1600;
                    GetBranchType2(branchPt, ref fireHydrantSysOut, stpt, pt1, new Dictionary<Point3dEx, List<Point3dEx>>(), fireHydrantSysIn);
                    var pt4 = new Point3dEx(stpt._pt.X + 800, stpt._pt.Y, 0);
                    stpt1 = new Point3dEx(stpt._pt.X + XGap, stpt._pt.Y, 0);
                    fireHydrantSysOut.LoopLine.Add(new Line(pt4._pt, stpt1._pt));
                }
            }
            else
            {
                stpt1 = stpt;
            }
            if (fireHydrantSysIn.TermPointDic.ContainsKey(pt2))
            {
                if (!fireHydrantSysIn.TermPointDic[pt2].PipeNumber.Equals(""))
                {
                    GetBranchType2(branchPt, ref fireHydrantSysOut, stpt1, pt2, new Dictionary<Point3dEx, List<Point3dEx>>(), fireHydrantSysIn);

                }
            }   
        }

        /// <summary>
        /// 绘制两根供水管，一根立管
        /// </summary>
        /// <param name="输出"></param>
        /// <param name="起始点"></param>
        /// <param name="分支路径"></param>
        /// <param name="输入"></param>
        private static void GetBranchType21(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, List<Point3dEx> tpts,
            Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn)
        {
            var newPt = new Point3dEx(0, 0, 0);
            foreach (var pt in tpts)
            {
                if (!fireHydrantSysIn.TermPointDic.ContainsKey(pt))
                {
                    continue;
                }
                if (fireHydrantSysIn.TermPointDic[pt].Type.Equals(1))
                {
                    newPt = pt;
                    tpts.Remove(pt);
                    break;
                }
            }
            GetBranchType3(branchPt, ref fireHydrantSysOut, stpt, tpts, ValveDic, fireHydrantSysIn);
            double XGap = 1600;
            var pt4 = new Point3dEx(stpt._pt.X + 800, stpt._pt.Y, 0);
            var stpt1 = new Point3dEx(stpt._pt.X + XGap, stpt._pt.Y, 0);
            fireHydrantSysOut.LoopLine.Add(new Line(pt4._pt, stpt1._pt));
            GetBranchType1(branchPt, ref fireHydrantSysOut, stpt1, newPt, new Dictionary<Point3dEx, List<Point3dEx>>(), fireHydrantSysIn);
        }

        /// <summary>
        /// 绘制三根立管
        /// </summary>
        /// <param name="输出"></param>
        /// <param name="起始点"></param>
        /// <param name="分支路径"></param>
        /// <param name="输入"></param>
        private static void GetBranchType03(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, List<Point3dEx> tpts,
            Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn)
        {
            var ptls = new List<Point3dEx>(tpts);
            var pt1 = new Point3dEx(0,0,0);
            foreach(var pt in tpts)
            {
                if(fireHydrantSysIn.TermPointDic[pt].PipeNumber.Contains("水泵接合器"))
                {
                    pt1 = new Point3dEx(pt._pt);
                    ptls.Remove(pt1);
                }
            }
            GetBranchType5(branchPt, ref fireHydrantSysOut, stpt, pt1, new Dictionary<Point3dEx, List<Point3dEx>>(), fireHydrantSysIn);
            double XGap = 1600;
            var pt4 = new Point3dEx(stpt._pt.X + 800, stpt._pt.Y, 0);
            var stpt1 = new Point3dEx(stpt._pt.X + XGap, stpt._pt.Y, 0);
            fireHydrantSysOut.LoopLine.Add(new Line(pt4._pt, stpt1._pt));
            GetBranchType2(branchPt, ref fireHydrantSysOut, stpt1, ptls[0], new Dictionary<Point3dEx, List<Point3dEx>>(), fireHydrantSysIn);
            pt4 = new Point3dEx(stpt1._pt.X + 800, stpt1._pt.Y, 0);
            stpt1 = new Point3dEx(stpt1._pt.X + XGap, stpt1._pt.Y, 0);
            fireHydrantSysOut.LoopLine.Add(new Line(pt4._pt, stpt1._pt));
            GetBranchType2(branchPt, ref fireHydrantSysOut, stpt1, ptls[1], new Dictionary<Point3dEx, List<Point3dEx>>(), fireHydrantSysIn);
        }

        /// <summary>
        /// 绘制一根供水管，两根立管
        /// </summary>
        /// <param name="输出"></param>
        /// <param name="起始点"></param>
        /// <param name="分支路径"></param>
        /// <param name="输入"></param>
        private static void GetBranchType12(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, List<Point3dEx> tpts,
            Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn)
        {
            var newPt = new Point3dEx(0, 0, 0);
            foreach (var pt in tpts)
            {
                if (!fireHydrantSysIn.TermPointDic.ContainsKey(pt))
                {
                    newPt = pt;
                    tpts.Remove(pt);
                    break;
                }
                if (fireHydrantSysIn.TermPointDic[pt].Type.Equals(2))
                {
                    newPt = pt;
                    tpts.Remove(pt);
                    break;
                }
            }
            GetBranchType3(branchPt, ref fireHydrantSysOut, stpt, tpts, ValveDic, fireHydrantSysIn);
            double XGap = 1600;
            var pt4 = new Point3dEx(stpt._pt.X + 800, stpt._pt.Y, 0);
            var stpt1 = new Point3dEx(stpt._pt.X + XGap, stpt._pt.Y, 0);
            fireHydrantSysOut.LoopLine.Add(new Line(pt4._pt, stpt1._pt));
            GetBranchType2(branchPt, ref fireHydrantSysOut, stpt1, newPt, new Dictionary<Point3dEx, List<Point3dEx>>(), fireHydrantSysIn);
        }
    }
}
