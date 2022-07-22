using Autodesk.AutoCAD.Geometry;
using System.Linq;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.UndergroundSpraySystem.Model;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.UndergroundSpraySystem.Block;
using System;

namespace ThMEPWSS.UndergroundSpraySystem.Service.MultiBranchLoop
{
    internal class BranchLoop2
    {
        public static void Get(SprayOut sprayOut, SpraySystem spraySystem, SprayIn sprayIn)
        {
            double valveSize = sprayIn.ValveSize;

            double alarmGap = sprayIn.PipeGap;
            double valveGapX2 = 800;//存在阀门时，阀门与管道间距
            double floorHeight = sprayIn.FloorHeight;

            int currentFloor = Convert.ToInt32(sprayOut.CurrentFloor.Last());

            foreach (var rstPath in spraySystem.BranchLoops2)
            {
                var firstPt = rstPath.First();
                var lastPt = rstPath.Last();
                var ptType = sprayIn.PtTypeDic[firstPt];
                var typeNum = Convert.ToInt32(ptType.Last());

                double valveGapX = 50;
                int fireAreaIndex = 0;//当前支管的防火分区index
                int alarmValveNums = spraySystem.SubLoopAlarmsDic[rstPath.Last()][0];

                BranchLoop1.GetStartEndPt(spraySystem, rstPath, out Point3d sPt, out Point3d ePt);//获取报警阀间的起始终止点
                var pts = BranchLoop1.AddPipeLine(sprayOut, spraySystem, sprayIn, rstPath, sPt, ePt);//添加报警阀支环管线

                Point3d ePt1 = pts[0];
                Point3d ePt12 = pts[1];
                Point3d ePt2 = pts[2];

                Point3d nextPt = ePt12; //起始点
                Point3d curPt = ePt12;
                bool valveFlag = false;
                bool firstAlarmValveVisited = false;//第一个报警阀
                var lastValveVisited = false;//遍历到最后一个报警阀
                var visitedAlarmValveNums = 0;//遍历过的报警阀数目


                //新逻辑
                var newSpt = ePt12.OffsetX(0);
                var newEpt = ePt12.OffsetX(0);
                if (spraySystem.BranchLoopPtNewDic.ContainsKey(firstPt))
                {
                    newSpt = spraySystem.BranchLoopPtNewDic[firstPt].OffsetX(1200);
                    newEpt = spraySystem.BranchLoopPtNewDic[lastPt].OffsetX(1200);
                }


                for (int i = 1; i < rstPath.Count - 1; i++)
                {
                    var pt = rstPath[i];

                    var preType = sprayIn.PtTypeDic[rstPath[i - 1]];
                    var nextType = sprayIn.PtTypeDic[rstPath[i + 1]];
                    var type = sprayIn.PtTypeDic[pt];
                    var alValveGap = alarmGap;
                    if (type.Contains("3BranchLoop"))
                    {
                        if (!firstAlarmValveVisited)
                        {
                            BranchLoop1.AddCrossPipe(true, newSpt, floorHeight, sprayOut, spraySystem, pt);
                        }
                        if (lastValveVisited)
                        {
                            BranchLoop1.AddCrossPipe(false, newEpt, floorHeight, sprayOut, spraySystem, pt);
                        }
                    }
                    else if (!type.Contains("AlarmValve") && sprayIn.PtDic[pt].Count == 3)
                    {
                        sprayIn.PtTypeDic[pt] = "Branch";
                        type = sprayIn.PtTypeDic[pt];
                    }
                    if (type.Equals("Branch"))
                    {
                        if (spraySystem.BranchPtDic.ContainsKey(pt))
                        {
                            spraySystem.BranchPtDic.Remove(pt);
                        }
                        sprayOut.PipeLine.Add(new Line(ePt1, ePt1.OffsetY(1200)));
                        spraySystem.BranchPtDic.Add(pt, ePt1.OffsetY(1200));
                    }
                    if (type.Contains("AlarmValve"))
                    {
                        if (!firstAlarmValveVisited)
                        {
                            alValveGap = 1700;
                            firstAlarmValveVisited = true;
                        }
                        nextPt = curPt.OffsetX(alValveGap);
                        BranchLoop1.AddAlarmValve(sprayOut, spraySystem, sprayIn, fireAreaIndex, ePt2, ref nextPt, ref curPt, ref valveFlag, pt);

                        BranchLoop1.CountfireAreaNums(pt, spraySystem, sprayIn, ref fireAreaIndex);//统计防火分区的数目
                        visitedAlarmValveNums++;
                        if (visitedAlarmValveNums == alarmValveNums)//遍历到最后一个报警阀
                        {
                            lastValveVisited = true;
                        }
                    }

                    if (type.Equals("SignalValve") && firstAlarmValveVisited && !lastValveVisited)
                    {
                        if (preType.Equals("BranchLoop") || nextType.Equals("BranchLoop"))
                        {
                            continue;
                        }
                        sprayOut.PipeLine.Add(new Line(curPt, curPt.OffsetX(valveGapX2)));
                        curPt = curPt.OffsetX(valveGapX2);
                        sprayOut.SprayBlocks.Add(new SprayBlock("遥控信号阀", curPt));
                        sprayOut.PipeLine.Add(new Line(curPt.OffsetX(valveSize), curPt.OffsetX(valveGapX2 + valveSize)));
                        curPt = curPt.OffsetX(valveGapX2 + valveSize);

                        valveFlag = true;
                    }

                }
                sprayOut.PipeLine.Add(new Line(curPt, ePt2));


            }
        }
    }
}
