using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.UndergroundSpraySystem.Block;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundSpraySystem.Service
{
    class MainLoop2
    {
        public static void Set(SprayOut sprayOut, SpraySystem spraySystem, SprayIn sprayIn)
        {
            foreach (var rstPath in spraySystem.MainLoops)
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
            var pathIndex = 0;
            var lastGap = 0.0;
            var height = 600;
            var lastInsertPt = new Point3d();
            var alarmGap = sprayIn.PipeGap;
            for (int j = 0; j < spraySystem.MainLoops.Count; j++)
            {
                var rstPath = spraySystem.MainLoops[j];//当前主环
                var curPt = sprayOut.PipeInsertPoint.OffsetX(lastGap);//绘制当前点
                var endPt = curPt.OffsetY(-height);//绘制结束点
                for (int i = 0; i < rstPath.Count; i++)//从前往后，遍历阀前元素
                {
                    var pt = rstPath[i];
                    var type = sprayIn.PtTypeDic[pt];
                    var cnt = sprayIn.PtDic[pt].Count;
                    if (i==0)
                    {
                        sprayOut.PipeLine.Add(new Line(curPt, curPt.OffsetX(1000)));
                        curPt = curPt.OffsetX(1000);
                    }
                    if(type.Contains("PressureValves"))
                    {
                        sprayOut.SprayBlocks.Add(new SprayBlock("减压阀", curPt, 0));
                        curPt = curPt.OffsetX(240);
                        sprayOut.PipeLine.Add(new Line(curPt, curPt.OffsetX(1000)));
                        curPt = curPt.OffsetX(1000);
                    }
                    if(type.Contains("DieValves"))
                    {
                        sprayOut.SprayBlocks.Add(new SprayBlock("蝶阀", curPt, 0));
                        curPt = curPt.OffsetX(240);
                        sprayOut.PipeLine.Add(new Line(curPt, curPt.OffsetX(1000)));
                        curPt = curPt.OffsetX(1000);
                    }
                    if(type.Contains("SignalValve"))
                    {
                        sprayOut.SprayBlocks.Add(new SprayBlock("遥控信号阀", curPt, 0));
                        curPt = curPt.OffsetX(300);
                        sprayOut.PipeLine.Add(new Line(curPt, curPt.OffsetX(1000)));
                        curPt = curPt.OffsetX(1000);
                    }
                    if(type.Contains("AlarmValve")) break;//遍历到报警阀间，跳出
                    else if(cnt==3)
                    {
                        sprayOut.PipeLine.Add(new Line(curPt,curPt.OffsetX(alarmGap)));
                        curPt = curPt.OffsetX(alarmGap);
                        spraySystem.BranchPtDic.Add(pt, curPt);
                    }
                }
                var lastPt = new Point3d(curPt.X,curPt.Y,0);//存入上一层的最新点位置
                curPt = endPt;//跳到下面管线
                for (int i = rstPath.Count-1; i >= 0; i--)//从后往前，遍历阀后元素
                {
                    var pt = rstPath[i];
                    var type = sprayIn.PtTypeDic[pt];
                    var cnt = sprayIn.PtDic[pt].Count;
                    if (i == rstPath.Count - 1)
                    {
                        sprayOut.PipeLine.Add(new Line(curPt, curPt.OffsetX(1000)));
                        curPt = curPt.OffsetX(1000);
                    }
                    if (type.Contains("PressureValves"))
                    {
                        sprayOut.SprayBlocks.Add(new SprayBlock("减压阀", curPt, 0));
                        curPt = curPt.OffsetX(240);
                        sprayOut.PipeLine.Add(new Line(curPt, curPt.OffsetX(1000)));
                        curPt = curPt.OffsetX(1000);
                    }
                    if (type.Contains("DieValves"))
                    {
                        sprayOut.SprayBlocks.Add(new SprayBlock("蝶阀", curPt, 0));
                        curPt = curPt.OffsetX(240);
                        sprayOut.PipeLine.Add(new Line(curPt, curPt.OffsetX(1000)));
                        curPt = curPt.OffsetX(1000);
                    }
                    if (type.Contains("SignalValve"))
                    {
                        sprayOut.SprayBlocks.Add(new SprayBlock("遥控信号阀", curPt, 0));
                        curPt = curPt.OffsetX(300);
                        sprayOut.PipeLine.Add(new Line(curPt, curPt.OffsetX(1000)));
                        curPt = curPt.OffsetX(1000);
                    }
                    if (type.Contains("AlarmValve")) break;
                    else if (cnt == 3)
                    {
                        sprayOut.PipeLine.Add(new Line(curPt, lastPt.OffsetY(-height)));
                        curPt = lastPt.OffsetY(-height);
                        sprayOut.PipeLine.Add(new Line(curPt, curPt.OffsetX(alarmGap)));
                        curPt = curPt.OffsetX(alarmGap);
                        spraySystem.BranchPtDic.Add(pt, curPt);
                        sprayOut.PipeLine.Add(new Line(curPt, curPt.OffsetX(1000)));
                        curPt = curPt.OffsetX(1000);
                    }
                }
                sprayOut.PipeLine.Add(new Line(lastPt, curPt.OffsetXY(500,height)));//补上上面管线的部分
                lastPt = curPt.OffsetXY(500,height);
                var stPt1 = sprayOut.PipeInsertPoint.OffsetX(lastGap);
                double floorHeight = sprayIn.FloorHeight;
                int alarmValveNums = spraySystem.SubLoopAlarmsDic[rstPath.Last()][0];
                var pipeLen = floorHeight - 1800;
                var spt1 = lastPt;
                var ept1 = curPt;
                var spt2 = spt1.OffsetY(-pipeLen);
                var ept2 = ept1.OffsetY(-pipeLen);
                var spt3 = spt2.OffsetX(1700 + (alarmValveNums - 1) * alarmGap + 1000);
                var ept3 = ept2.OffsetX(1700 + (alarmValveNums - 1) * alarmGap + 1500);
     
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
                int fireAreaNum = 1; //当前支管的防火分区数目，默认1个
                Point3d stPt = spt2.OffsetX(1700);

                for (int i = 0; i < rstPath.Count; i++)
                {
                    var pt = rstPath[i];
                    var type = sprayIn.PtTypeDic[pt];
                    if (type.Contains("AlarmValve"))
                    {
                        var alarmValve = new AlarmValveSys(stPt, fireAreaIndex++, floorHeight);
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
                        spraySystem.FireAreaStPtDic.Add(pt, spt3.OffsetXY(alarmGap, 3900));//报警阀起点
                        stPt = stPt.OffsetX(alarmGap);
                        if (!spraySystem.BranchDic.ContainsKey(pt)) continue;
                        
                        foreach (var tpt in spraySystem.BranchDic[pt])//遍历支路端点
                        {
                            if (!sprayIn.TermPtDic.ContainsKey(tpt)) continue;
                            if (sprayIn.TermPtDic[tpt].Type == 1)//端点类型是防火分区
                            {
                                fireAreaNum++;//防火分区数+1
                            }
                        }
                    }
                }
                if (stPt.X > spraySystem.MaxOffSetX) spraySystem.MaxOffSetX = stPt.X;
                lastGap += 1000 + 1700 + (alarmValveNums - 1) * alarmGap + 1000 + 1700 + fireAreaNum * 5500 + 1500;
                lastInsertPt = spt1;
                pathIndex++;
            }
        }
    }
}
