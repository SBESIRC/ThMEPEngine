using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.Command;
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
                    double pipeLength = fireHydrantSysIn.pipeWidth;
                    if (branchDic.ContainsKey(pt))
                    {
                        if (branchDic[pt].Count == 3)
                        {
                            pipeLength = 2 * fireHydrantSysIn.pipeWidth;
                        }
                    }

                    if (fireHydrantSysIn.ptTypeDic[pt].Equals("MainLoop"))
                    {
                        stPt = GetPipePart.GetMainLoopPoint(ref fireHydrantSysOut, i, stPt, rstPath, fireHydrantSysIn, valveWidth, pipeLength);
                        continue;
                    }
                    if (fireHydrantSysIn.ptTypeDic[pt].Equals("SubLoop"))
                    {
                        bool isSubLoop = false;
                        stPt = GetPipePart.GetSubLoopPoint(ref fireHydrantSysOut, isSubLoop, i, pt, stPt, rstPath, fireHydrantSysIn, pipeGap, pipeLength);
                        continue;
                    }
                    if (fireHydrantSysIn.ptTypeDic[pt].Equals("Branch"))
                    {
                        stPt = GetPipePart.GetBranchPoint(ref fireHydrantSysOut, i, pt, stPt, rstPath, pipeGap, pipeLength, fireHydrantSysIn);
                        continue;
                    }
                    if (fireHydrantSysIn.ptTypeDic[pt].Contains("Valve"))
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
                var stPt = new Point3d(stPt1.X, stPt1.Y - 12000 * index, 0);
                index += 1;
                var ptStart = new Point3d(stPt.X, stPt.Y, 0);
                var pipeGap = 400;
                var valveWidth = 240;
                var valveCheck = true;

                for (int i = 0; i < rstPath.Count - 1; i++)
                {
                    var pt = rstPath[i];
                    double pipeLength = fireHydrantSysIn.pipeWidth;
                    if (branchDic.ContainsKey(pt))
                    {
                        if (branchDic[pt].Count == 3)
                        {
                            pipeLength = 2 * fireHydrantSysIn.pipeWidth;
                        }
                    }
                    if (fireHydrantSysIn.ptTypeDic[pt].Equals("MainLoop"))
                    {
                        stPt = GetPipePart.GetMainLoopPoint(ref fireHydrantSysOut, i, stPt, rstPath, fireHydrantSysIn, valveWidth, pipeLength);
                        continue;
                    }

                    if (fireHydrantSysIn.ptTypeDic[pt].Equals("SubLoop"))
                    {
                        bool isSubLoop = true;
                        stPt = GetPipePart.GetSubLoopPoint(ref fireHydrantSysOut, isSubLoop, i, pt, stPt, rstPath, fireHydrantSysIn, pipeGap, pipeLength);
                        continue;
                    }

                    if (fireHydrantSysIn.ptTypeDic[pt].Equals("Branch"))
                    {
                        stPt = GetPipePart.GetBranchPoint(ref fireHydrantSysOut, i, pt, stPt, rstPath, pipeGap, pipeLength, fireHydrantSysIn);
                        continue;
                    }

                    if (fireHydrantSysIn.ptTypeDic[pt].Contains("Valve"))
                    {
                        stPt = GetPipePart.GetValvePoint(pt, ref fireHydrantSysOut, stPt, ref valveCheck, fireHydrantSysIn);
                        continue;
                    }
                }
                GetPipePart.GetSubLoopDetial(ref fireHydrantSysOut, stPt, ptStart, rstPath, fireHydrantSysIn.markList);
            }
        }

        public static void GetBranch(ref FireHydrantSystemOut fireHydrantSysOut, Dictionary<Point3dEx, List<Point3dEx>> branchDic,
            Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn)
        {
            var list = new List<Point3d>();
            foreach (var pt in fireHydrantSysIn.termPointDic.Keys)
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
                    if (fireHydrantSysIn.termPointDic.ContainsKey(branchDic[pt][0]))
                    {
                        if (fireHydrantSysIn.termPointDic[branchDic[pt].First()].Type.Equals(1))//终点是类型1，消火栓
                        {
                            string pipeNumber = fireHydrantSysIn.termPointDic[branchDic[pt][0]].PipeNumber;//立管标号
                            if (pipeNumber[0].Equals('X') || pipeNumber[0].Equals('B'))//消火栓
                            {
                                GetBranchType1(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], ValveDic, fireHydrantSysIn);
                            }
                        }

                        if (fireHydrantSysIn.termPointDic[branchDic[pt][0]].Type.Equals(2))//终点是类型2，其他区域
                        {
                            GetBranchType2(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], ValveDic, fireHydrantSysIn);
                        }
                        if (fireHydrantSysIn.termPointDic[branchDic[pt][0]].Type.Equals(3))//终点是类型3，消火栓和其他楼层共用支管
                        {
                            GetBranchType4(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], ValveDic, fireHydrantSysIn);
                        }
                        if (fireHydrantSysIn.termPointDic[branchDic[pt][0]].Type.Equals(4))//终点是类型4，消火栓和其他楼层共用支管
                        {
                            GetBranchType5(pt, ref fireHydrantSysOut, stPt, branchDic[pt][0], ValveDic, fireHydrantSysIn);
                        }
                    }
                }
                if (branchDic[pt].Count == 2)//两个支路
                {
                    var stPt = fireHydrantSysOut.BranchDrawDic[pt];
                    if (branchDic[pt].Count != 0)
                    {
                        GetBranchType3(pt, ref fireHydrantSysOut, stPt, branchDic[pt], ValveDic, fireHydrantSysIn);
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
                        if (!fireHydrantSysIn.termPointDic.ContainsKey(tpt))
                        {
                            continue;
                        }
                        if (fireHydrantSysIn.termPointDic[tpt].Type.Equals(1))
                        {
                            type1Nums += 1;
                        }
                    }
                    if (type1Nums == 0)
                    {
                        continue;//暂时不作处理
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

        private static void valveCheck(ref FireHydrantSystemOut fireHydrantSysOut, Point3d valve, Point3d stPt,
            FireHydrantSystemIn fireHydrantSysIn, ref bool flag)
        {
            foreach (var pt in fireHydrantSysIn.GateValves)
            {
                if (valve.DistanceTo(pt) < 250)
                {
                    fireHydrantSysOut.IsGateValve.Add(stPt);
                    flag = true;
                    break;
                }
            }
        }

        public static void ValveAdd(Point3d pt1, Point3d pt4, ref FireHydrantSystemOut fireHydrantSysOut, FireHydrantSystemIn fireHydrantSysIn,
            Point3d valveSite, bool valve, ref List<Line> lineList, int isCasing)
        {
            double valveSize = 240;
            if (valve)
            {
                double pt2X = 280;
                if (isCasing == 2)
                {
                    pt2X = 180;
                }
                var pt2 = new Point3d(pt1.X + pt2X, pt1.Y, 0);
                var flag = false;
                valveCheck(ref fireHydrantSysOut, valveSite, pt2, fireHydrantSysIn, ref flag);
                if (flag)
                {
                    valveSize = 300;
                }

                var pt3 = new Point3d(pt2.X + valveSize, pt2.Y, 0);
                lineList.Add(new Line(pt1, pt2));
                lineList.Add(new Line(pt3, pt4));
                fireHydrantSysOut.Valve.Add(pt2);
                if (isCasing == 1)
                {
                    fireHydrantSysOut.IsCasing.Add(new Point3d(pt2.X - 250, pt2.Y, 0));
                }
                if (isCasing == 2)
                {
                    fireHydrantSysOut.IsCasing.Add(new Point3d(pt3.X + 50, pt3.Y, 0));
                }
            }
            else
            {
                lineList.Add(new Line(pt1, pt4));
            }

        }

        /// <summary>
        /// 绘制向下的单分支
        /// </summary>
        /// <param name="输出"></param>
        /// <param name="起始点"></param>
        /// <param name="分支路径"></param>
        /// <param name="输入"></param>
        private static void GetBranchType1(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, Point3dEx tpt,
            Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn)
        {
            var valve = false;//是否存在阀门
            double XGap = 800;
            var YGap = 5800;
            var textWidth = fireHydrantSysIn.textWidth;
            var textHeight = 1700;

            string pipeNumber = fireHydrantSysIn.termPointDic[tpt].PipeNumber;//立管标号

            var pt1 = stpt._pt;

            var pt4 = new Point3d(pt1.X + XGap, pt1.Y, 0);
            var pt5 = new Point3d(pt4.X, pt4.Y - YGap, 0);
            var pt6 = new Point3d(pt1.X, pt5.Y, 0);
            var lineList = new List<Line>();
            lineList.Add(new Line(pt4, pt5));
            lineList.Add(new Line(pt5, pt6));

            Point3d valveSite = new Point3d();
            int isCasing = 0;
            if (ValveDic.ContainsKey(branchPt))
            {
                if (ValveDic[branchPt].Count > 0)
                {
                    valve = true;
                    valveSite = ValveDic[branchPt][0]._pt;
                    isCasing = Casing.HasCasing(ValveDic[branchPt], fireHydrantSysIn);
                }
            }

            ValveAdd(pt1, pt4, ref fireHydrantSysOut, fireHydrantSysIn, valveSite, valve, ref lineList, isCasing);

            foreach (var line in lineList)
            {
                fireHydrantSysOut.LoopLine.Add(line);

            }
            if (pipeNumber[0].Equals('X') || pipeNumber[0].Equals('B'))
            {
                var textPt1 = new Point3d(pt4.X - textWidth, pt4.Y - textHeight, 0);
                var textPt2 = new Point3d(pt4.X, pt4.Y - textHeight, 0);
                var textLine = ThTextSet.ThTextLine(textPt1, textPt2);
                fireHydrantSysOut.TextLine.Add(textLine);

                var text = ThTextSet.ThText(textPt1, pipeNumber);
                fireHydrantSysOut.TextList.Add(text);
            }
            fireHydrantSysOut.FireHydrant.Add(pt6);
            var DN = ThTextSet.ThText(new Point3d(pt4.X + 400, pt4.Y - 2800, 0), Math.PI / 2, "DN65");
            fireHydrantSysOut.DNList.Add(DN);
        }

        /// <summary>
        /// 绘制向上的单分支
        /// </summary>
        /// <param name="输出"></param>
        /// <param name="起始点"></param>
        /// <param name="分支路径"></param>
        /// <param name="输入"></param>
        private static void GetBranchType2(Point3dEx branchPt, ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, Point3dEx tpt,
            Dictionary<Point3dEx, List<Point3dEx>> ValveDic, FireHydrantSystemIn fireHydrantSysIn)
        {
            var valve = false;//是否存在阀门
            double XGap = 800;
            var YGap = 3100;
            var textHeight = 970;
            var textWidth = fireHydrantSysIn.textWidth;
            if (!fireHydrantSysIn.termPointDic.ContainsKey(tpt))
            {
                return;
            }
            string pipeNumber = fireHydrantSysIn.termPointDic[tpt].PipeNumber;//立管标号

            var pt1 = stpt._pt;

            var pt4 = new Point3d(pt1.X + XGap, pt1.Y, 0);
            var pt5 = new Point3d(pt4.X, pt4.Y + YGap, 0);
            var pt6 = new Point3d(pt4.X, pt5.Y + textHeight, 0);
            var pt7 = new Point3d(pt6.X + textWidth, pt6.Y, 0);
            foreach (var line in fireHydrantSysOut.LoopLine.ToList())
            {
                if ((line.StartPoint.X - pt4.X) * (line.EndPoint.X - pt4.X) < 0 && line.StartPoint.Y > pt4.Y && line.StartPoint.Y < pt5.Y)
                {
                    fireHydrantSysOut.LoopLine.Remove(line);
                    var line1 = new Line();
                    var line2 = new Line();
                    if (line.StartPoint.X < line.EndPoint.X)
                    {
                        line1 = new Line(line.StartPoint, new Point3d(pt4.X - 100, line.StartPoint.Y, 0));
                        line2 = new Line(line.EndPoint, new Point3d(pt4.X + 100, line.StartPoint.Y, 0));
                    }
                    else
                    {
                        line1 = new Line(line.EndPoint, new Point3d(pt4.X - 100, line.StartPoint.Y, 0));
                        line2 = new Line(line.StartPoint, new Point3d(pt4.X + 100, line.StartPoint.Y, 0));
                    }
                    fireHydrantSysOut.LoopLine.Add(line1);
                    fireHydrantSysOut.LoopLine.Add(line2);
                }
            }
            var lineList = new List<Line>();
            lineList.Add(new Line(pt4, pt5));

            Point3d valveSite = new Point3d();
            int isCasing = 0;
            if (ValveDic.ContainsKey(branchPt))
            {
                valve = true;
                valveSite = ValveDic[branchPt][0]._pt;
                isCasing = Casing.HasCasing(ValveDic[branchPt], fireHydrantSysIn);
            }

            ValveAdd(pt1, pt4, ref fireHydrantSysOut, fireHydrantSysIn, valveSite, valve, ref lineList, isCasing);
            foreach (var line in lineList)
            {
                fireHydrantSysOut.LoopLine.Add(line);
            }
            fireHydrantSysOut.PipeInterrupted.Add(pt5, Math.PI * 3 / 2);
            var textLine1 = ThTextSet.ThTextLine(pt5, pt6);
            var textLine2 = ThTextSet.ThTextLine(pt6, pt7);

            var text = ThTextSet.ThText(pt6, pipeNumber);
            if (!pipeNumber.Trim().Equals(""))
            {
                fireHydrantSysOut.TextLine.Add(textLine1);
                fireHydrantSysOut.TextLine.Add(textLine2);
                fireHydrantSysOut.TextList.Add(text);
            }

            var DN1 = ThTextSet.ThText(new Point3d(pt5.X + 400, pt4.Y + 1370, 0), Math.PI / 2, "DN100");
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
            var valve = false;//是否存在阀门
            double XGap = 800;
            double upRightX = 200;
            var YGap1 = 3100;
            var YGap2 = 5800;
            var textWidth1 = fireHydrantSysIn.textWidth;
            var textHeight1 = 1700;
            var textHeight2 = 970;
            var textWidth2 = fireHydrantSysIn.textWidth;

            string pipeNumber1 = "";
            string pipeNumber2 = "";

            if (fireHydrantSysIn.termPointDic.ContainsKey(tpts[0]))
            {
                pipeNumber1 = fireHydrantSysIn.termPointDic[tpts[0]].PipeNumber;//立管标号 1
            }
            if (fireHydrantSysIn.termPointDic.ContainsKey(tpts[1]))
            {
                pipeNumber2 = fireHydrantSysIn.termPointDic[tpts[1]].PipeNumber;//立管标号 2

            }

            var pt1 = stpt._pt;

            var pt4 = new Point3d(pt1.X + XGap, pt1.Y, 0);
            var pt5 = new Point3d(pt4.X, pt4.Y - YGap2, 0);
            var pt6 = new Point3d(pt1.X, pt5.Y, 0);
            var pt7 = new Point3d(pt4.X - upRightX, pt1.Y, 0);
            var pt8 = new Point3d(pt7.X, pt7.Y + YGap1, 0);
            var lineList = new List<Line>();
            lineList.Add(new Line(pt4, pt5));
            lineList.Add(new Line(pt5, pt6));
            lineList.Add(new Line(pt7, pt8));

            fireHydrantSysOut.PipeInterrupted.Add(pt8, Math.PI * 3 / 2);

            foreach (var line in fireHydrantSysOut.LoopLine.ToList())
            {
                if ((line.StartPoint.X - pt7.X) * (line.EndPoint.X - pt7.X) < 0 && line.StartPoint.Y > pt7.Y && line.StartPoint.Y < pt8.Y)
                {
                    fireHydrantSysOut.LoopLine.Remove(line);
                    var line1 = new Line();
                    var line2 = new Line();
                    if (line.StartPoint.X < line.EndPoint.X)
                    {
                        line1 = new Line(line.StartPoint, new Point3d(pt7.X - 100, line.StartPoint.Y, 0));
                        line2 = new Line(line.EndPoint, new Point3d(pt7.X + 100, line.StartPoint.Y, 0));
                    }
                    else
                    {
                        line1 = new Line(line.EndPoint, new Point3d(pt7.X - 100, line.StartPoint.Y, 0));
                        line2 = new Line(line.StartPoint, new Point3d(pt7.X + 100, line.StartPoint.Y, 0));
                    }
                    fireHydrantSysOut.LoopLine.Add(line1);
                    fireHydrantSysOut.LoopLine.Add(line2);
                }
            }
            Point3d valveSite = new Point3d();
            int isCasing = 0;
            if (ValveDic.ContainsKey(branchPt))
            {
                valve = true;
                valveSite = ValveDic[branchPt][0]._pt;
                isCasing = Casing.HasCasing(ValveDic[branchPt], fireHydrantSysIn);
            }

            ValveAdd(pt1, pt4, ref fireHydrantSysOut, fireHydrantSysIn, valveSite, valve, ref lineList, isCasing);

            foreach (var line in lineList)
            {
                fireHydrantSysOut.LoopLine.Add(line);
            }
            fireHydrantSysOut.FireHydrant.Add(pt6);
            if (pipeNumber1.Equals(""))
            {
                return;
            }
            if (pipeNumber1[0].Equals('X') || pipeNumber1[0].Equals('B'))
            {
                var textPt1 = new Point3d(pt4.X - textWidth1, pt4.Y - textHeight1, 0);
                var textPt2 = new Point3d(pt4.X, pt4.Y - textHeight1, 0);
                var textLine = ThTextSet.ThTextLine(textPt1, textPt2);
                fireHydrantSysOut.TextLine.Add(textLine);

                var text1 = ThTextSet.ThText(textPt1, pipeNumber1);
                fireHydrantSysOut.TextList.Add(text1);

                var textLine1 = ThTextSet.ThTextLine(pt8, new Point3d(pt8.X, pt8.Y + textHeight2, 0));
                var textLine2 = ThTextSet.ThTextLine(new Point3d(pt8.X, pt8.Y + textHeight2, 0), new Point3d(pt8.X + textWidth2, pt8.Y + textHeight2, 0));

                var text2 = ThTextSet.ThText(textLine2.StartPoint, pipeNumber2);
                if (!pipeNumber2.Trim().Equals(""))
                {
                    fireHydrantSysOut.TextLine.Add(textLine1);
                    fireHydrantSysOut.TextLine.Add(textLine2);
                    fireHydrantSysOut.TextList.Add(text2);
                }
            }

            else
            {
                var textPt1 = new Point3d(pt4.X - textWidth1, pt4.Y - textHeight1, 0);
                var textPt2 = new Point3d(pt4.X, pt4.Y - textHeight1, 0);
                var textLine = ThTextSet.ThTextLine(textPt1, textPt2);
                fireHydrantSysOut.TextLine.Add(textLine);

                var text1 = ThTextSet.ThText(textPt1, pipeNumber2);
                fireHydrantSysOut.TextList.Add(text1);

                var textLine1 = ThTextSet.ThTextLine(pt8, new Point3d(pt8.X, pt8.Y + textHeight2, 0));
                var textLine2 = ThTextSet.ThTextLine(new Point3d(pt8.X, pt8.Y + textHeight2, 0), new Point3d(pt8.X + textWidth2, pt8.Y + textHeight2, 0));

                var text2 = ThTextSet.ThText(textLine2.StartPoint, pipeNumber1);
                if (!pipeNumber2.Trim().Equals(""))
                {
                    fireHydrantSysOut.TextLine.Add(textLine1);
                    fireHydrantSysOut.TextLine.Add(textLine2);
                    fireHydrantSysOut.TextList.Add(text2);
                }
            }

            var DN = ThTextSet.ThText(new Point3d(pt4.X + 400, pt4.Y - 2800, 0), Math.PI / 2, "DN65");
            fireHydrantSysOut.DNList.Add(DN);
            var DN1 = ThTextSet.ThText(new Point3d(pt7.X + 400, pt4.Y + 1370, 0), Math.PI / 2, "DN100");
            fireHydrantSysOut.DNList.Add(DN1);
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

            double XGap = 800;
            var YGap = 3100;
            var textHeight = 970;
            var textWidth = fireHydrantSysIn.textWidth;

            string pipeNumber = fireHydrantSysIn.termPointDic[tpt].PipeNumber;//立管标号

            var pt1 = stpt._pt;

            var pt4 = new Point3d(pt1.X + XGap, pt1.Y, 0);

            var pt7 = new Point3d(pt4.X, pt1.Y + YGap, 0);
            var lineList = new List<Line>();
            lineList.Add(new Line(pt4, pt7));

            foreach (var line in fireHydrantSysOut.LoopLine.ToList())
            {
                if ((line.StartPoint.X - pt4.X) * (line.EndPoint.X - pt4.X) < 0 && line.StartPoint.Y > pt4.Y && line.StartPoint.Y < pt7.Y)
                {
                    fireHydrantSysOut.LoopLine.Remove(line);
                    var line1 = new Line();
                    var line2 = new Line();
                    if (line.StartPoint.X < line.EndPoint.X)
                    {
                        line1 = new Line(line.StartPoint, new Point3d(pt4.X - 100, line.StartPoint.Y, 0));
                        line2 = new Line(line.EndPoint, new Point3d(pt4.X + 100, line.StartPoint.Y, 0));
                    }
                    else
                    {
                        line1 = new Line(line.EndPoint, new Point3d(pt4.X - 100, line.StartPoint.Y, 0));
                        line2 = new Line(line.StartPoint, new Point3d(pt4.X + 100, line.StartPoint.Y, 0));
                    }
                    fireHydrantSysOut.LoopLine.Add(line1);
                    fireHydrantSysOut.LoopLine.Add(line2);
                }
            }

            foreach (var line in lineList)
            {
                fireHydrantSysOut.LoopLine.Add(line);
            }
            fireHydrantSysOut.PipeInterrupted.Add(pt7, Math.PI * 3 / 2);

            var textLine1 = ThTextSet.ThTextLine(pt7, new Point3d(pt7.X, pt7.Y + textHeight, 0));
            var textLine2 = ThTextSet.ThTextLine(new Point3d(pt7.X, pt7.Y + textHeight, 0), new Point3d(pt7.X + textWidth, pt7.Y + textHeight, 0));

            var text2 = ThTextSet.ThText(textLine2.StartPoint, pipeNumber);
            if (!pipeNumber.Trim().Equals(""))
            {
                fireHydrantSysOut.TextLine.Add(textLine1);
                fireHydrantSysOut.TextLine.Add(textLine2);
                fireHydrantSysOut.TextList.Add(text2);
            }

            var DN1 = ThTextSet.ThText(new Point3d(pt7.X + 400, pt4.Y + 1370, 0), Math.PI / 2, "DN100");
            fireHydrantSysOut.DNList.Add(DN1);
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
            var valve = false;//是否存在阀门
            double XGap = 800;
            var YGap = 2200;
            var textWidth = fireHydrantSysIn.textWidth;

            string pipeNumber = fireHydrantSysIn.termPointDic[tpt].PipeNumber;//立管标号

            var pt1 = stpt._pt;

            var pt4 = new Point3d(pt1.X + XGap, pt1.Y, 0);
            var pt5 = new Point3d(pt4.X, pt4.Y + YGap, 0);
            var pt51 = new Point3d(pt5.X + 900, pt5.Y, 0);
            var pt6 = new Point3d(pt51.X, pt51.Y + 2870, 0);
            var pt7 = new Point3d(pt6.X + 3500, pt6.Y, 0);
            foreach (var line in fireHydrantSysOut.LoopLine.ToList())
            {
                if ((line.StartPoint.X - pt4.X) * (line.EndPoint.X - pt4.X) < 0 && line.StartPoint.Y > pt4.Y && line.StartPoint.Y < pt5.Y)
                {
                    fireHydrantSysOut.LoopLine.Remove(line);
                    var line1 = new Line();
                    var line2 = new Line();
                    if (line.StartPoint.X < line.EndPoint.X)
                    {
                        line1 = new Line(line.StartPoint, new Point3d(pt4.X - 100, line.StartPoint.Y, 0));
                        line2 = new Line(line.EndPoint, new Point3d(pt4.X + 100, line.StartPoint.Y, 0));
                    }
                    else
                    {
                        line1 = new Line(line.EndPoint, new Point3d(pt4.X - 100, line.StartPoint.Y, 0));
                        line2 = new Line(line.StartPoint, new Point3d(pt4.X + 100, line.StartPoint.Y, 0));
                    }
                    fireHydrantSysOut.LoopLine.Add(line1);
                    fireHydrantSysOut.LoopLine.Add(line2);
                }
            }
            var lineList = new List<Line>();
            lineList.Add(new Line(pt4, pt5));
            lineList.Add(new Line(pt5, pt51));
            Point3d valveSite = new Point3d();

            int isCasing = 0;
            if (ValveDic.ContainsKey(branchPt))
            {
                valve = true;
                valveSite = ValveDic[branchPt][0]._pt;
                isCasing = Casing.HasCasing(ValveDic[branchPt], fireHydrantSysIn);
            }
            ValveAdd(pt1, pt4, ref fireHydrantSysOut, fireHydrantSysIn, valveSite, valve, ref lineList, isCasing);

            foreach (var line in lineList)
            {
                fireHydrantSysOut.LoopLine.Add(line);
            }
            fireHydrantSysOut.PipeInterrupted.Add(pt51, 0);
            var textLine1 = ThTextSet.ThTextLine(pt51, pt6);
            var textLine2 = ThTextSet.ThTextLine(pt6, pt7);


            var text = ThTextSet.ThText(pt6, pipeNumber);
            var text1 = ThTextSet.ThText(new Point3d(pt6.X, pt6.Y - 500, 0), "详见总图");
            if (!pipeNumber.Trim().Equals(""))
            {
                fireHydrantSysOut.TextLine.Add(textLine1);
                fireHydrantSysOut.TextLine.Add(textLine2);
                fireHydrantSysOut.TextList.Add(text);
                if (!pipeNumber.Contains("详见总图"))
                {
                    fireHydrantSysOut.TextList.Add(text1);
                }
            }

            var DN1 = ThTextSet.ThText(new Point3d(pt1.X, pt1.Y - 500, 0), 0, "DN100");
            fireHydrantSysOut.DNList.Add(DN1);
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
                if (!fireHydrantSysIn.termPointDic.ContainsKey(pt))
                {
                    continue;
                }
                if (fireHydrantSysIn.termPointDic[pt].Type.Equals(1))
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
                if (!fireHydrantSysIn.termPointDic.ContainsKey(pt))
                {
                    newPt = pt;
                    tpts.Remove(pt);
                    break;
                }
                if (fireHydrantSysIn.termPointDic[pt].Type.Equals(2))
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
