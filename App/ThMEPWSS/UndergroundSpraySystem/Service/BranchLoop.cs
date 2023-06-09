﻿using Autodesk.AutoCAD.Geometry;
using System.Linq;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.UndergroundSpraySystem.Model;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.UndergroundSpraySystem.Block;
using System;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using System.Collections.Generic;

namespace ThMEPWSS.UndergroundSpraySystem.Service
{
    class BranchLoop
    {
        public static void Get(SprayOut sprayOut, SpraySystem spraySystem, SprayIn sprayIn)
        {
            double valveSize = sprayIn.ValveSize;
            
            double alarmGap = sprayIn.PipeGap;
            double valveGapX2 = 800;//存在阀门时，阀门与管道间距
            double floorHeight = sprayIn.FloorHeight;
            foreach (var rstPath in spraySystem.BranchLoops)
            {
                double valveGapX = 50;
                int fireAreaIndex = 0;//当前支管的防火分区index
                int alarmValveNums = spraySystem.SubLoopAlarmsDic[rstPath.Last()][0];

                GetStartEndPt(spraySystem, rstPath, out Point3d sPt, out Point3d ePt);//获取报警阀间的起始终止点
                AddPipeLine(sprayOut, spraySystem, sprayIn, rstPath, sPt, ePt);//添加报警阀支环管线

                Point3d ePt1 = ePt.OffsetX(-2 * valveGapX - valveSize);
                Point3d ePt12 = ePt1.OffsetY(3300 - floorHeight);
                Point3d ePt2 = ePt12.OffsetX(1700 + (alarmValveNums - 1) * sprayIn.PipeGap + 1000);

                Point3d nextPt = ePt12; //起始点
                Point3d curPt = ePt12;
                bool valveFlag = false;
                bool firstAlarmValve = true;//第一个报警阀

                for (int i = 1; i < rstPath.Count - 1; i++)
                {
                    try
                    {
                        var pt = rstPath[i];
                        var preType = sprayIn.PtTypeDic[rstPath[i - 1]];
                        var nextType = sprayIn.PtTypeDic[rstPath[i + 1]];
                        var type = sprayIn.PtTypeDic[pt];
                        var alValveGap = alarmGap;
                        if(!type.Contains("AlarmValve") && sprayIn.PtDic[pt].Count == 3)
                        {
                            sprayIn.PtTypeDic[pt] = "Branch";
                            type = sprayIn.PtTypeDic[pt];
                        }
                        if(type.Equals("Branch"))
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
                            if (firstAlarmValve)
                            {
                                alValveGap = 1700;
                                firstAlarmValve = false;
                            }
                            nextPt = curPt.OffsetX(alValveGap);
                            AddAlarmValve(sprayOut, spraySystem, sprayIn, fireAreaIndex, ePt2, ref nextPt, ref curPt, ref valveFlag, pt);

                            CountfireAreaNums(pt, spraySystem, sprayIn, ref fireAreaIndex);//统计防火分区的数目
                        }

                        if (type.Equals("SignalValve"))
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
                    catch (Exception ex)
                    {
                        ;
                    }
                }
                sprayOut.PipeLine.Add(new Line(curPt, ePt2));
            }
        }

        private static void AddAlarmValve(SprayOut sprayOut, SpraySystem spraySystem, SprayIn sprayIn, int fireAreaIndex, Point3d ePt2, ref Point3d nextPt, ref Point3d curPt, ref bool valveFlag, Point3dEx pt)
        {
            double floorHeight = sprayIn.FloorHeight;
            double alarmGap = sprayIn.PipeGap;

            var insertPt = new Point3d(nextPt.X, nextPt.Y, 0);//默认插入点
            if (valveFlag)
            {
                valveFlag = false;
                insertPt = new Point3d(curPt.X, curPt.Y, 0);
            }
            else
            {
                sprayOut.PipeLine.Add(new Line(curPt, nextPt));//添加湿式报警阀前面一段线
                curPt = new Point3d(nextPt.X, nextPt.Y, 0);
                nextPt = nextPt.OffsetX(alarmGap);
            }
            var alarmValve = new AlarmValveSys(insertPt, fireAreaIndex, floorHeight);
            spraySystem.BranchPtDic.Add(pt, alarmValve.EndPt);
            sprayOut.AlarmValves.Add(alarmValve);//插入湿式报警阀平面
            AddAlarmText(sprayOut, sprayIn, pt, insertPt);//添加报警阀编号
            spraySystem.FireAreaStPtDic.Add(pt, ePt2.OffsetXY(sprayIn.PipeGap, 5200));
        }

        private static void GetStartEndPt(SpraySystem spraySystem, List<Point3dEx> rstPath, out Point3d sPt, out Point3d ePt)
        {
            var tpt1 = spraySystem.BranchLoopPtDic[rstPath.First()];
            var tpt2 = spraySystem.BranchLoopPtDic[rstPath.Last()];
            if (tpt1.X < tpt2.X)
            {
                sPt = tpt1;
                ePt = tpt2;
            }
            else
            {
                sPt = tpt2;
                ePt = tpt1;
            }
        }

        private static void AddPipeLine(SprayOut sprayOut, SpraySystem spraySystem, SprayIn sprayIn, List<Point3dEx> rstPath, Point3d sPt, Point3d ePt)
        {
            double valveGapX = 50;
            double floorHeight = sprayIn.FloorHeight;
            double valveSize = sprayIn.ValveSize;

            int alarmValveNums = spraySystem.SubLoopAlarmsDic[rstPath.Last()][0];

            Point3d ePt1 = ePt.OffsetX(-2 * valveGapX - valveSize);
            Point3d sPt1 = sPt.OffsetX(2 * valveGapX + valveSize);
            Point3d sPt12 = sPt1.OffsetY(2700 - floorHeight);
            Point3d ePt12 = ePt1.OffsetY(3300 - floorHeight);
            Point3d ePt2 = ePt12.OffsetX(1700 + (alarmValveNums - 1) * sprayIn.PipeGap + 1000);
            Point3d sPt2 = ePt2.OffsetY(-600);

            sprayOut.PipeLine.Add(new Line(sPt, sPt.OffsetX(valveGapX)));
            sprayOut.PipeLine.Add(new Line(ePt, ePt.OffsetX(-valveGapX)));
            sprayOut.PipeLine.Add(new Line(sPt.OffsetX(valveGapX + valveSize), sPt1));
            sprayOut.PipeLine.Add(new Line(ePt.OffsetX(-valveGapX - valveSize), ePt1));
            sprayOut.PipeLine.Add(new Line(sPt1, sPt12));
            sprayOut.PipeLine.Add(new Line(ePt1, ePt12));
            sprayOut.PipeLine.Add(new Line(sPt12, sPt2));
            sprayOut.PipeLine.Add(new Line(sPt2, ePt2));

            sprayOut.SprayBlocks.Add(new SprayBlock("遥控信号阀", sPt.OffsetX(valveGapX)));
            sprayOut.SprayBlocks.Add(new SprayBlock("遥控信号阀", ePt1.OffsetX(valveGapX)));
        }

        private static void AddAlarmText(SprayOut sprayOut, SprayIn sprayIn, Point3dEx pt, Point3d insertPt)
        {
            double minDist = 500;
            string alarmText = "";//报警阀编号
            foreach (var apt in sprayIn.AlarmTextDic.Keys)
            {
                double dist = apt._pt.DistanceTo(pt._pt);
                if (dist < minDist)
                {
                    minDist = dist;
                    alarmText = sprayIn.AlarmTextDic[apt];
                    if (dist < 50) break;
                }
            }
            var text = new Text(alarmText, insertPt.OffsetXY(-200, -550));
            sprayOut.Texts.Add(text);
        }

        private static void CountfireAreaNums(Point3dEx pt, SpraySystem spraySystem, SprayIn sprayIn, ref int fireAreaIndex)
        {
            if (!spraySystem.BranchDic.ContainsKey(pt))
            {
                return;
            }
            foreach (var tpt in spraySystem.BranchDic[pt])//遍历支路端点
            {
                if (!sprayIn.TermPtDic.ContainsKey(tpt))
                {
                    continue;
                }
                if (sprayIn.TermPtDic[tpt].Type == 1 || sprayIn.TermPtDic[tpt].Type == 2)//端点类型是防火分区或者其他楼层
                {
                    fireAreaIndex++;//防火分区index+1
                    break;//统计到一个就退出
                }
            }
        }

    }
}
