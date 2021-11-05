﻿using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.Block;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.UndergroundSpraySystem.Model;
using System.Linq;
using System.Collections.Generic;

namespace ThMEPWSS.UndergroundSpraySystem.Service
{
    class MainLoop2
    {
        public static void Set(SprayOut sprayOut, SpraySystem spraySystem, SprayIn sprayIn)
        {
            foreach(var rstPath in spraySystem.MainLoops)
            {
                int alarmValveNums = 0;//湿式报警阀数目
                foreach (var pt in rstPath)
                {
                    if (sprayIn.PtTypeDic[pt].Contains("AlarmValve"))
                    {
                        alarmValveNums++;
                    }
                }
                if (!spraySystem.SubLoopAlarmsDic.ContainsKey(rstPath.First()))
                {
                    spraySystem.SubLoopAlarmsDic.Add(rstPath.First(), new List<int>() { alarmValveNums });
                }
                else
                {
                    spraySystem.SubLoopAlarmsDic[rstPath.First()].Add(alarmValveNums);
                }
                if (!spraySystem.SubLoopAlarmsDic.ContainsKey(rstPath.Last()))
                {
                    spraySystem.SubLoopAlarmsDic.Add(rstPath.Last(), new List<int>() { alarmValveNums });
                }
                else
                {
                    spraySystem.SubLoopAlarmsDic[rstPath.Last()].Add(alarmValveNums);
                }
            }           
        }
        public static void Get(SprayOut sprayOut, SpraySystem spraySystem, SprayIn sprayIn)
        {
            Set(sprayOut, spraySystem, sprayIn);
            int pathIndex = 0;
            double lastGap = 0;
            Point3d lastInsertPt = new Point3d();
            for(int j = 0; j < spraySystem.MainLoops.Count; j++)
            {
                try
                {
                    var rstPath = spraySystem.MainLoops[j];
                    var height = 600;
                    var stPt1 = sprayOut.PipeInsertPoint.OffsetX(lastGap);
                    var stPt = stPt1;
                    double floorHeight = sprayIn.FloorHeight;
                    double alarmGap = sprayIn.PipeGap;
                    int alarmValveNums = spraySystem.SubLoopAlarmsDic[rstPath.Last()][j];
                    var pipeLen = floorHeight - 1800;
                    var pt1 = stPt.OffsetY(-height);
                    var spt1 = stPt.OffsetX(1000);
                    var ept1 = pt1.OffsetX(500);
                    var spt2 = spt1.OffsetY(-pipeLen);
                    var ept2 = ept1.OffsetY(-pipeLen);
                    var spt3 = spt2.OffsetX(1700 + (alarmValveNums - 1) * alarmGap + 1000);
                    var ept3 = ept2.OffsetX(1700 + (alarmValveNums - 1) * alarmGap + 1500);
                    if (j != 0)
                    {
                        sprayOut.PipeLine.Add(new Line(pt1, pt1.OffsetX(-500)));
                    }

                    sprayOut.PipeLine.Add(new Line(stPt, spt1));
                    sprayOut.PipeLine.Add(new Line(pt1, ept1));
                    sprayOut.PipeLine.Add(new Line(spt1, spt2));
                    sprayOut.PipeLine.Add(new Line(ept1, ept2));
                    sprayOut.PipeLine.Add(new Line(spt2, spt3));
                    sprayOut.PipeLine.Add(new Line(ept2, ept3));
                    sprayOut.PipeLine.Add(new Line(spt3, ept3));
                    if (pathIndex > 0)
                    {
                        sprayOut.PipeLine.Add(new Line(lastInsertPt, stPt1));
                        sprayOut.PipeLine.Add(new Line(lastInsertPt.OffsetXY(-500, -height), stPt1.OffsetXY(-500, -height)));
                    }
                    int fireAreaIndex = -1;//当前支管的防火分区index
                    int fireAreaNum = 0; //当前支管的防火分区数目
                    stPt = spt2.OffsetX(1700);
                    for (int i = 0; i < rstPath.Count; i++)
                    {
                        var pt = rstPath[i];
                        //var preType = sprayIn.PtTypeDic[rstPath[i - 1]];
                        //var nextType = sprayIn.PtTypeDic[rstPath[i + 1]];
                        var type = sprayIn.PtTypeDic[pt];

                        if (type.Contains("AlarmValve"))
                        {
                            var alarmValve = new AlarmValveSys(stPt, fireAreaIndex, floorHeight);

                            spraySystem.BranchPtDic.Add(pt, alarmValve.EndPt);
                            sprayOut.AlarmValves.Add(alarmValve);//插入湿式报警阀平面
                            foreach (var apt in sprayIn.AlarmTextDic.Keys)
                            {
                                if (apt._pt.DistanceTo(pt._pt) < 150)
                                {
                                    var text = new Text(sprayIn.AlarmTextDic[apt], stPt.OffsetXY(-200, -550));
                                    sprayOut.Texts.Add(text);
                                }
                            }
                            spraySystem.FireAreaStPtDic.Add(pt, spt3.OffsetXY(alarmGap, 5200));
                            stPt = stPt.OffsetX(alarmGap);

                            if (!spraySystem.BranchDic.ContainsKey(pt))
                            {
                                continue;
                            }
                            foreach (var tpt in spraySystem.BranchDic[pt])//遍历支路端点
                            {
                                if (!sprayIn.TermPtDic.ContainsKey(tpt))
                                    continue;
                                if (sprayIn.TermPtDic[tpt].Type == 1)//端点类型是防火分区
                                {
                                    fireAreaIndex++;//防火分区index+1
                                    break;//统计到一个就退出
                                }
                            }
                            foreach (var tpt in spraySystem.BranchDic[pt])//遍历支路端点
                            {
                                if (!sprayIn.TermPtDic.ContainsKey(tpt))
                                    continue;
                                if (sprayIn.TermPtDic[tpt].Type == 1)//端点类型是防火分区
                                {
                                    fireAreaNum++;//防火分区数+1
                                }
                            }
                        }
                        //else if(sprayIn.PtDic[pt].Count == 3)
                        //{
                        //    sprayOut.PipeLine.Add(new Line(stPt.OffsetX(15000), stPt.OffsetXY(15000, 1200)));
                        //    spraySystem.BranchPtDic.Add(pt, stPt.OffsetXY(15000, 1200));
                        //}
                    }
                    lastGap += 1000 + 1700 + (alarmValveNums - 1) * alarmGap + 1000 + 1700 + fireAreaNum * 5500 + 1500;
                    lastInsertPt = spt1;
                    pathIndex++;
                }
                catch
                {
                    ;
                }
                
            }
            ;
        }
    }
}
