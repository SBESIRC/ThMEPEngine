using System;
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
                //var rstPath = spraySystem.MainLoop;
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
                var rstPath = spraySystem.MainLoops[j];
                var height = 0.06 * sprayIn.FloorHeight;
                var stPt1 = sprayOut.PipeInsertPoint.OffsetX(lastGap);
                var stPt = stPt1;
                double floorHeight = sprayIn.FloorHeight;
                double alarmGap = 1500;
                int alarmValveNums = spraySystem.SubLoopAlarmsDic[rstPath.Last()][0];
                var pipeLen = 0.79 * sprayIn.FloorHeight;
                var pt1 = stPt.OffsetY(-height);
                var spt1 = stPt.OffsetX(1000);
                var ept1 = pt1.OffsetX(500);
                var spt2 = spt1.OffsetY(-pipeLen);
                var ept2 = ept1.OffsetY(-pipeLen);
                var spt3 = spt2.OffsetX(1700 + (alarmValveNums - 1) * alarmGap + 1000);
                var ept3 = ept2.OffsetX(1700 + (alarmValveNums - 1) * alarmGap + 1500);
                if(j != 0)
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
                if(pathIndex > 0)
                {
                    sprayOut.PipeLine.Add(new Line(lastInsertPt, stPt1));
                    sprayOut.PipeLine.Add(new Line(lastInsertPt.OffsetXY(-500, -height), stPt1.OffsetXY(-500, -height)));
                }
                int fireAreaIndex = -1;//当前支管的防火分区index
                int fireAreaNum = 0; //当前支管的防火分区数目
                stPt = spt2.OffsetX(1700);
                for (int i = 1; i < rstPath.Count - 1; i++)
                {
                    var pt = rstPath[i];
                    var preType = sprayIn.PtTypeDic[rstPath[i - 1]];
                    var nextType = sprayIn.PtTypeDic[rstPath[i + 1]];
                    var type = sprayIn.PtTypeDic[pt];
                    if(pt._pt.DistanceTo(new Point3d(754823.329194008, 513816.1021144, 0)) < 10)
                    {
                        ;
                    }
                    if (type.Contains("AlarmValve"))
                    {
                        var alarmValve = new AlarmValveSys(stPt, fireAreaIndex, floorHeight);

                        spraySystem.BranchPtDic.Add(pt, alarmValve.EndPt);
                        sprayOut.AlarmValves.Add(alarmValve);//插入湿式报警阀平面
                        spraySystem.FireAreaStPtDic.Add(pt, spt3.OffsetXY(1700, 4900));
                        stPt = stPt.OffsetX(alarmGap);
                        //if (nextType.Contains("AlarmValve"))
                        //{
                        //    sprayOut.PipeLine.Add(new Line(stPt, stPt.OffsetX(alarmGap)));
                        //    stPt = stPt.OffsetX(alarmGap);
                        //}
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
                }
                lastGap += 1000 + 1700 + (alarmValveNums - 1) * alarmGap + 1000 + 1700 + fireAreaNum * 5500 + 1500;
                lastInsertPt = spt1;
                pathIndex++;
            }
            ;
        }
    }
}
