using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    class GetFireHydrantPipe
    {
        public static void GetMainLoop(ref FireHydrantSystemOut fireHydrantSysOut, List<Point3dEx> rstPath, FireHydrantSystemIn fireHydrantSysIn)
        {
            using ( AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //var stPt = new Point3d(12650, 50000, 0);
                var stPt = fireHydrantSysOut.InsertPoint;
                var ptStart = new Point3d(stPt.X, stPt.Y, 0);
                double pipeLength = 1600;
                double pipeGap = 400;
                double valveWidth = 240;
                bool valveCheck = true;
                
                for (int i = 0; i < rstPath.Count - 2; i++)
                {
                    var pt = rstPath[i];
                    if (fireHydrantSysIn.ptTypeDic[pt].Equals("MainLoop"))
                    {
                        stPt = GetPipePart.GetMainLoopPoint(ref fireHydrantSysOut, i, stPt, rstPath, fireHydrantSysIn.ptTypeDic, valveWidth, pipeLength);
                        continue;
                    }
                    if (fireHydrantSysIn.ptTypeDic[pt].Equals("SubLoop"))
                    {
                        bool isSubLoop = false;
                        stPt = GetPipePart.GetSubLoopPoint(ref fireHydrantSysOut, isSubLoop, i, pt, stPt, fireHydrantSysIn.markList, pipeGap, pipeLength);
                        continue;
                    }
                    if (fireHydrantSysIn.ptTypeDic[pt].Equals("Branch"))
                    {
                        stPt = GetPipePart.GetBranchPoint(ref fireHydrantSysOut, pt, stPt, pipeGap, pipeLength);
                        continue;
                    }
                    if (fireHydrantSysIn.ptTypeDic[pt].Equals("Valve"))
                    {
                        stPt = GetPipePart.GetValvePoint(ref fireHydrantSysOut, stPt, ref valveCheck);
                        continue;
                    }
                }
                GetPipePart.GetMainLoopDetial(ref fireHydrantSysOut, stPt, ptStart);
            } 
        }

        public static void GetSubLoop(ref FireHydrantSystemOut fireHydrantSysOut, List<List<Point3dEx>> subPathList, FireHydrantSystemIn fireHydrantSysIn)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var index = 0;
                foreach(var rstPath in subPathList)
                {
                    index += 1;
                    var stPt1 = fireHydrantSysOut.InsertPoint;
                    var stPt = new Point3d(stPt1.X, stPt1.Y + 20000 * index, 0);
                    var ptStart = new Point3d(stPt.X, stPt.Y, 0);
                    var pipeLength = 1600;
                    var pipeGap = 400;
                    var valveWidth = 240;
                    var valveCheck = true;

                    for (int i = 0; i < rstPath.Count - 1; i++)
                    {
                        var pt = rstPath[i];
                        if (fireHydrantSysIn.ptTypeDic[pt].Equals("MainLoop"))
                        {
                            stPt = GetPipePart.GetMainLoopPoint(ref fireHydrantSysOut, i, stPt, rstPath, fireHydrantSysIn.ptTypeDic, valveWidth, pipeLength);
                            continue;
                        }

                        if (fireHydrantSysIn.ptTypeDic[pt].Equals("SubLoop"))
                        {
                         
                            bool isSubLoop = true;
                            stPt = GetPipePart.GetSubLoopPoint(ref fireHydrantSysOut, isSubLoop, i, pt, stPt, fireHydrantSysIn.markList, pipeGap, pipeLength);
                            continue;
                        }

                        if (fireHydrantSysIn.ptTypeDic[pt].Equals("Branch"))
                        {
                            stPt = GetPipePart.GetBranchPoint(ref fireHydrantSysOut, pt, stPt, pipeGap, pipeLength);
                            continue;
                        }

                        if (fireHydrantSysIn.ptTypeDic[pt].Equals("Valve"))
                        {
                            stPt = GetPipePart.GetValvePoint(ref fireHydrantSysOut, stPt, ref valveCheck);
                            continue;
                        }
                    }
                    GetPipePart.GetSubLoopDetial(ref fireHydrantSysOut, stPt, ptStart, rstPath, fireHydrantSysIn.markList);
                }  

            }
        }

        public static void GetBranch(ref FireHydrantSystemOut fireHydrantSysOut, Dictionary<Point3dEx, List<List<Point3dEx>>> branchDic, FireHydrantSystemIn fireHydrantSysIn)
        {
            var list = new List<Point3d>();
            foreach(var pt in fireHydrantSysIn.termPointDic.Keys)
            {
                list.Add(pt._pt);
            }
        
            foreach (var pt in branchDic.Keys)//对于支路的每一个起始点
            {

                if (branchDic[pt].Count == 1)//单支路
                {
                    var stPt = fireHydrantSysOut.BranchDrawDic[pt];
                    if (fireHydrantSysIn.termPointDic[branchDic[pt].First().Last()].Type.Equals(1))//终点是类型1，消火栓
                    {
                        string pipeNumber = fireHydrantSysIn.termPointDic[branchDic[pt][0].Last()].PipeNumber;//立管标号
                        if (pipeNumber[0].Equals('X'))//消火栓
                        {
                            GetBranchType1(ref fireHydrantSysOut, stPt, branchDic[pt][0], fireHydrantSysIn);
                        }  
                    }

                    if(fireHydrantSysIn.termPointDic[branchDic[pt].First().Last()].Type.Equals(2))//终点是类型2，其他区域
                    {
                        GetBranchType2(ref fireHydrantSysOut, stPt, branchDic[pt][0], fireHydrantSysIn);
                    }
                    if(fireHydrantSysIn.termPointDic[branchDic[pt].First().Last()].Type.Equals(3))//终点是类型3，消火栓和其他楼层共用支管
                    {
                        GetBranchType4(ref fireHydrantSysOut, stPt, branchDic[pt][0], fireHydrantSysIn);
                    }
                }
                else//两个支路
                {
                    var stPt = fireHydrantSysOut.BranchDrawDic[pt];
                    GetBranchType3(ref fireHydrantSysOut, stPt, branchDic[pt], fireHydrantSysIn);
                }
            }

        }

        private static void GetBranchType1(ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, List<Point3dEx> ptList, FireHydrantSystemIn fireHydrantSysIn)
        {
            using var acadDatabase = AcadDatabase.Active();
            var valve = false;//是否存在阀门
            double valveSize = 240;
            double valveLeft = 300;
            double XGap = 800;
            var YGap = 5800;
            var textWidth = 1300;
            var textHeight = 1700;

            string pipeNumber = fireHydrantSysIn.termPointDic[ptList.Last()].PipeNumber;//立管标号

            var pt1 = stpt._pt;
            var pt2 = new Point3d(pt1.X + valveLeft - valveSize / 2, pt1.Y, 0);
            var pt3 = new Point3d(pt2.X + valveSize / 2, pt2.Y, 0);
            var pt4 = new Point3d(pt1.X + XGap, pt2.Y, 0);
            var pt5 = new Point3d(pt4.X, pt4.Y - YGap, 0);
            var pt6 = new Point3d(pt1.X, pt5.Y, 0);
            var lineList = new List<Line>();
            lineList.Add(new Line(pt4, pt5));
            lineList.Add(new Line(pt5, pt6));

            foreach(var pt in ptList)
            {
                if(fireHydrantSysIn.ptTypeDic[pt].Equals("Valve"))
                {
                    valve = true;
                }
            }
            if (valve)
            {
                lineList.Add(new Line(pt1, pt2));
                lineList.Add(new Line(pt3, pt4));
                fireHydrantSysOut.Valve.Add(pt2);
            }
            else
            {
                lineList.Add(new Line(pt1, pt4));
            }
            foreach (var line in lineList)
            {
                fireHydrantSysOut.LoopLine.Add(line);

            }
            if (pipeNumber[0].Equals('X'))
            {
                var textPt1 = new Point3d(pt4.X - textWidth, pt4.Y - textHeight, 0);
                var textPt2 = new Point3d(pt4.X, pt4.Y - textHeight, 0);
                var textLine = ThTextSet.ThTextLine(textPt1, textPt2);
                fireHydrantSysOut.TextLine.Add(textLine);

                var text = ThTextSet.ThText(textPt1, pipeNumber);
                fireHydrantSysOut.TextList.Add(text);
            }
            fireHydrantSysOut.FireHydrant.Add(pt6);

        }

        private static void GetBranchType2(ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, List<Point3dEx> ptList, FireHydrantSystemIn fireHydrantSysIn)
        {
            using var acadDatabase = AcadDatabase.Active();
            var valve = false;//是否存在阀门
            double valveSize = 240;
            double valveLeft = 400;
            double XGap = 800;
            var YGap = 3100;
            var textHeight = 970;
            var textWidth = 1500;

            string pipeNumber = fireHydrantSysIn.termPointDic[ptList.Last()].PipeNumber;//立管标号

            var pt1 = stpt._pt;
            var pt2 = new Point3d(pt1.X + valveLeft - valveSize / 2, pt1.Y, 0);
            var pt3 = new Point3d(pt2.X + valveSize, pt2.Y, 0);
            var pt4 = new Point3d(pt1.X + XGap, pt2.Y, 0);
            var pt5 = new Point3d(pt4.X, pt4.Y + YGap, 0);
            var pt6 = new Point3d(pt4.X, pt5.Y + textHeight, 0);
            var pt7 = new Point3d(pt6.X + textWidth, pt6.Y, 0);
            foreach(var line in fireHydrantSysOut.LoopLine.ToList())
            {
                if((line.StartPoint.X - pt4.X)*(line.EndPoint.X - pt4.X) < 0  &&  line.StartPoint.Y > pt4.Y && line.StartPoint.Y < pt5.Y)
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

            foreach (var pt in ptList)
            {
                if (fireHydrantSysIn.ptTypeDic[pt].Equals("Valve"))
                {
                    valve = true;
                }
            }
            if (valve)
            {
                lineList.Add(new Line(pt1, pt2));
                lineList.Add(new Line(pt3, pt4));
                fireHydrantSysOut.Valve.Add(pt2);
            }
            else
            {
                lineList.Add(new Line(pt1, pt4));
            }
            foreach (var line in lineList)
            {
                fireHydrantSysOut.LoopLine.Add(line);
            }
            fireHydrantSysOut.PipeInterrupted.Add(pt5, Math.PI * 3 / 2);
            var textLine1 = ThTextSet.ThTextLine(pt5, pt6);
            var textLine2 = ThTextSet.ThTextLine(pt6, pt7);
            fireHydrantSysOut.TextLine.Add(textLine1);
            fireHydrantSysOut.TextLine.Add(textLine2);

            var text = ThTextSet.ThText(pt6, pipeNumber);
            fireHydrantSysOut.TextList.Add(text);
        }

        private static void GetBranchType3(ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, List<List<Point3dEx>> ptList, FireHydrantSystemIn fireHydrantSysIn)
        {
            using var acadDatabase = AcadDatabase.Active();
            var valve = false;//是否存在阀门
            double valveSize = 240;
            double valveLeft = 300;
            double XGap = 800;
            double upRightX = 200;
            var YGap1 = 3100;
            var YGap2 = 5800;
            var textWidth1 = 1300;
            var textHeight1 = 1700;
            var textHeight2 = 970;
            var textWidth2 = 1500;

            string pipeNumber1 = fireHydrantSysIn.termPointDic[ptList.First().Last()].PipeNumber;//立管标号 1
            string pipeNumber2 = fireHydrantSysIn.termPointDic[ptList.Last().Last()].PipeNumber;//立管标号 2

            var pt1 = stpt._pt;
            var pt2 = new Point3d(pt1.X + valveLeft - valveSize / 2, pt1.Y, 0);
            var pt3 = new Point3d(pt2.X + valveSize, pt1.Y, 0);
            var pt4 = new Point3d(pt1.X + XGap, pt1.Y, 0);
            var pt5 = new Point3d(pt4.X, pt4.Y - YGap2, 0);
            var pt6 = new Point3d(pt1.X, pt5.Y, 0);
            var pt7 = new Point3d(pt4.X - upRightX, pt1.Y, 0);
            var pt8 = new Point3d(pt7.X, pt7.Y + YGap1, 0);
            var lineList = new List<Line>();
            lineList.Add(new Line(pt4, pt5));
            lineList.Add(new Line(pt5, pt6));
            lineList.Add(new Line(pt7, pt8));
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

            foreach (var pt in ptList.First())
            {
                if (fireHydrantSysIn.ptTypeDic[pt].Equals("Valve"))
                {
                    valve = true;
                }
            }
            if (valve)
            {
                lineList.Add(new Line(pt1, pt2));
                lineList.Add(new Line(pt3, pt4));
                fireHydrantSysOut.Valve.Add(pt2);
            }
            else
            {
                lineList.Add(new Line(pt1, pt4));
            }
            foreach (var line in lineList)
            {
                fireHydrantSysOut.LoopLine.Add(line);
            }
            fireHydrantSysOut.FireHydrant.Add(pt6);
            if (pipeNumber1[0].Equals('X'))
            {
                var textPt1 = new Point3d(pt4.X - textWidth1, pt4.Y - textHeight1, 0);
                var textPt2 = new Point3d(pt4.X, pt4.Y - textHeight1, 0);
                var textLine = ThTextSet.ThTextLine(textPt1, textPt2);
                fireHydrantSysOut.TextLine.Add(textLine);

                var text1 = ThTextSet.ThText(textPt1, pipeNumber1);
                fireHydrantSysOut.TextList.Add(text1);

                var textLine1 = ThTextSet.ThTextLine(pt8, new Point3d(pt8.X, pt8.Y + textHeight2, 0));
                var textLine2 = ThTextSet.ThTextLine(new Point3d(pt8.X, pt8.Y + textHeight2, 0), new Point3d(pt8.X + textWidth2, pt8.Y + textHeight2, 0));
                fireHydrantSysOut.TextLine.Add(textLine1);
                fireHydrantSysOut.TextLine.Add(textLine2);
                var text2 = ThTextSet.ThText(textLine2.StartPoint, pipeNumber2);
                fireHydrantSysOut.TextList.Add(text2);
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
                fireHydrantSysOut.TextLine.Add(textLine1);
                fireHydrantSysOut.TextLine.Add(textLine2);

                var text2 = ThTextSet.ThText(textLine2.StartPoint, pipeNumber1);
                fireHydrantSysOut.TextList.Add(text2);
            }
        }

        private static void GetBranchType4(ref FireHydrantSystemOut fireHydrantSysOut, Point3dEx stpt, List<Point3dEx> ptList, FireHydrantSystemIn fireHydrantSysIn)
        {
            GetBranchType1(ref fireHydrantSysOut, stpt, ptList, fireHydrantSysIn);
            using var acadDatabase = AcadDatabase.Active();
            
            double XGap = 800;
            var YGap = 3100;
            var textHeight = 970;
            var textWidth = 1500;

            string pipeNumber = fireHydrantSysIn.termPointDic[ptList.Last()].PipeNumber;//立管标号

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

            fireHydrantSysOut.TextLine.Add(textLine1);
            fireHydrantSysOut.TextLine.Add(textLine2);
            var text2 = ThTextSet.ThText(textLine2.StartPoint, pipeNumber);
            fireHydrantSysOut.TextList.Add(text2);
        }

    }
}
