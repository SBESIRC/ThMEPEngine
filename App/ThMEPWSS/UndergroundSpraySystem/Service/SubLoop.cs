using System.Linq;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.UndergroundSpraySystem.Model;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.UndergroundSpraySystem.Block;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using System;
using System.Collections.Generic;

namespace ThMEPWSS.UndergroundSpraySystem.Service
{
    public class SubLoop
    {
        public static void Get(SprayOut sprayOut, SpraySystem spraySystem, SprayIn sprayIn)
        {
            double floorHeight = sprayIn.FloorHeight;
            foreach (var rstPath in spraySystem.SubLoops)
            {
                if (!spraySystem.SubLoopPtDic.ContainsKey(rstPath.First()))
                {
                    continue;
                }
                if (!spraySystem.SubLoopPtDic.ContainsKey(rstPath.Last()))
                {
                    continue;
                }
                var stPt1 = spraySystem.SubLoopPtDic[rstPath.First()];
                var endPt = spraySystem.SubLoopPtDic[rstPath.Last()];//次环的结束点

                var stPt = stPt1;
                bool pressure = true;
                bool firstBranch = true;
                int branchLoopNum = 0;
                int branchIndex = 1;//次环上的支路索引

                bool waterPumpFlag = false;
                var branchs = new List<Point3dEx>();
                var branchLoopPtDic = new Dictionary<Point3dEx, Point3d>();
                var branchPtDic = new Dictionary<Point3dEx, Point3d>();
                for (int i = 0; i < rstPath.Count - 1; i++)
                {
                    try
                    {
                        var pt = rstPath[i];
                        var nextType = sprayIn.PtTypeDic[rstPath[i + 1]];
                        var type = sprayIn.PtTypeDic[pt];

                        if (type.Contains("PressureValve"))
                        {
                            stPt = GetPrussureValve(stPt, sprayOut, floorHeight, ref pressure);
                        }
                        if (type.Equals("BranchLoop"))
                        {
                            var branchNums = branchs.Count;
                            bool firstFlag = true;
                            for (int j = branchNums - 1; j >= 0; j--)
                            {
                                try
                                {
                                    var bpt = branchs[j];
                                    waterPumpFlag = GetBranchPt(bpt, stPt, sprayOut, spraySystem, sprayIn, ref firstBranch,
                                        branchIndex, waterPumpFlag, firstFlag, ref branchLoopPtDic, ref branchPtDic);
                                    firstFlag = false;
                                    branchIndex++;
                                }
                                catch (Exception ex)
                                {

                                }

                            }
                            branchs.Clear();
                            stPt = GetBranchLoopPt(pt, stPt, sprayOut, spraySystem, sprayIn, branchLoopNum, rstPath);
                            branchLoopNum++;
                        }
                        if (type.Equals("SignalValve"))
                        {
                            stPt = GetValve(stPt, "遥控信号阀", ref sprayOut);
                        }
                        if (type.Equals("Branch"))
                        {
                            branchs.Add(pt);
                        }

                        if (nextType.Equals("SubLoop"))
                        {
                            if (i != rstPath.Count - 2)
                            {
                                sprayOut.PipeLine.Add(new Line(stPt, spraySystem.SubLoopPtDic[rstPath[i + 1]]));

                            }
                        }
                    }
                    catch
                    {
                        ;
                    }
                }



                for (int i = branchLoopPtDic.Count - 1; i >= 0; i--)
                {
                    spraySystem.BranchLoopPtDic.Add(branchLoopPtDic.ElementAt(i).Key, branchLoopPtDic.ElementAt(i).Value);
                    spraySystem.BranchPtDic.Add(branchPtDic.ElementAt(i).Key, branchPtDic.ElementAt(i).Value);
                }
            }
        }
        private static Point3d GetPrussureValve(Point3d stPt, SprayOut sprayOut, double floorHeight, ref bool pressure)
        {
            double sigma = 0.1;
            double valveSize = 300;
            var pt = stPt;
            if (pressure)
            {
                pt = stPt.OffsetX(2600);
                sprayOut.PipeLine.Add(new Line(stPt, stPt.OffsetX(200)));
                sprayOut.SprayBlocks.Add(new SprayBlock("减压阀", stPt.OffsetX(200)));
                sprayOut.Texts.Add(new Text("喷淋减压阀组一", stPt.OffsetXY(350, -sigma * floorHeight)));
                sprayOut.NoteLine.Add(new Line(stPt.OffsetX(350), stPt.OffsetXY(350, -sigma * floorHeight)));
                sprayOut.NoteLine.Add(new Line(stPt.OffsetXY(2150, -sigma * floorHeight), stPt.OffsetXY(350, -sigma * floorHeight)));
                sprayOut.PipeLine.Add(new Line(stPt.OffsetX(200 + valveSize), pt));
            }
            pressure = !pressure;
            return pt;
        }

        private static Point3d GetBranchLoopPt(Point3dEx curPt, Point3d stPt, SprayOut sprayOut, SpraySystem spraySystem,
            SprayIn sprayIn, int branchLoopNum, List<Point3dEx> rstPath)
        {
            double xGap = 500;
            if (branchLoopNum % 2 == 1)
            {
                int nums = branchLoopNum / 2;//支环（报警阀间）数量
                xGap = 5150 * nums //5150——支环点到支环结束点的长度
                    + sprayIn.PipeGap * (spraySystem.SubLoopAlarmsDic[curPt][0] - 1) //报警阀间距
                    + 2500 //最后一个报警阀到防火分区的间距
                    + spraySystem.SubLoopFireAreasDic[curPt][0] * 3000 //防火分区数目
                    + 2500 * spraySystem.SubLoopFireAreasDic[curPt][0];  //2500——防火分区的间距  
                double minX = Math.Min(stPt.X + xGap, spraySystem.SubLoopPtDic[rstPath.Last()].X);
                sprayOut.PipeLine.Add(new Line(stPt, new Point3d(minX, stPt.Y, 0)));
            }
            else
            {
                sprayOut.PipeLine.Add(new Line(stPt, stPt.OffsetX(xGap)));
            }
            var height = 300;
            var pt = stPt.OffsetY(-height);
            sprayOut.PipeLine.Add(new Line(stPt, pt));

            spraySystem.BranchLoopPtDic.Add(curPt, stPt.OffsetY(-height));
            return stPt.OffsetX(xGap);
        }

        private static Point3d GetValve(Point3d stPt, string valve, ref SprayOut sprayOut)
        {
            double valveSize = 300;
            double GapX = 500;
            sprayOut.SprayBlocks.Add(new Block.SprayBlock(valve, stPt));
            sprayOut.PipeLine.Add(new Line(stPt.OffsetX(valveSize), stPt.OffsetX(valveSize + GapX)));
            return stPt.OffsetX(valveSize + GapX);
        }

        private static bool GetBranchPt(Point3dEx curPt, Point3d stPt, SprayOut sprayOut, SpraySystem spraySystem,
            SprayIn sprayIn, ref bool firstBranch, int branchIndex, bool waterPumpFlag, bool firstFlag,
            ref Dictionary<Point3dEx, Point3d> branchLoopPtDic, ref Dictionary<Point3dEx, Point3d> branchPtDic)
        {
            var height = 400;
            double gap = firstFlag ?-3600:- branchIndex * sprayIn.PipeGap - 3200 * Convert.ToInt32(waterPumpFlag);

            var pt = stPt.OffsetX(gap);
            sprayOut.PipeLine.Add(new Line(pt, pt.OffsetY(height)));
            branchLoopPtDic.Add(curPt, pt.OffsetY(height));
            branchPtDic.Add(curPt, pt.OffsetY(height));
            firstBranch = false;
            if (!spraySystem.BranchDic.ContainsKey(curPt))
            {
                return false;
            }
            if (spraySystem.BranchDic[curPt].Count == 1)//单支路
            {
                var tpt = spraySystem.BranchDic[curPt][0];
                if (!sprayIn.TermPtTypeDic.ContainsKey(tpt))
                {
                    return false;
                }
                if (sprayIn.TermPtTypeDic[tpt] == 3 && !firstBranch)//支路末端是水泵接合器, 且不是第一个支路
                {
                    waterPumpFlag = true;
                }
            }
            return waterPumpFlag;
        }
    }
}
