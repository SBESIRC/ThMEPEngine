using Autodesk.AutoCAD.Geometry;
using System.Linq;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.UndergroundSpraySystem.Model;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.UndergroundSpraySystem.Block;
using System;

namespace ThMEPWSS.UndergroundSpraySystem.Service
{
    class BranchLoop
    {
        public static void Get(SprayOut sprayOut, SpraySystem spraySystem, SprayIn sprayIn)
        {
            double valveSize = 300;
            double valveGapX = 50;
            double alarmGap = sprayIn.PipeGap;
            double valveGapX2 = 600;
            double floorHeight = sprayIn.FloorHeight;
            foreach (var rstPath in spraySystem.BranchLoops)
            {
                int alarmValveNums = spraySystem.SubLoopAlarmsDic[rstPath.Last()][0];
                int fireAreaIndex = 0;//当前支管的防火分区index
                var tpt1 = spraySystem.BranchLoopPtDic[rstPath.First()];
                var tpt2 = spraySystem.BranchLoopPtDic[rstPath.Last()];
                Point3d sPt, ePt;
                if(tpt1.X < tpt2.X)
                {
                    sPt = tpt1;
                    ePt = tpt2;
                }
                else
                {
                    sPt = tpt2;
                    ePt = tpt1;
                }
                Point3d ePt1 = ePt.OffsetX(-2 * valveGapX - valveSize);
                Point3d sPt1 = sPt.OffsetX(2 * valveGapX + valveSize);
                Point3d sPt12 = sPt1.OffsetY(2700 - floorHeight);
                Point3d ePt12 = ePt1.OffsetY(3300 - floorHeight);
                Point3d ePt2 = ePt12.OffsetX(1700 + (alarmValveNums - 1) * alarmGap + 1000);
                Point3d sPt2 = ePt2.OffsetY(-600);

                sprayOut.PipeLine.Add(new Line(sPt, sPt.OffsetX(valveGapX)));
                sprayOut.PipeLine.Add(new Line(ePt, ePt.OffsetX(-valveGapX)));
                sprayOut.PipeLine.Add(new Line(sPt.OffsetX(valveGapX + valveSize), sPt1));
                sprayOut.PipeLine.Add(new Line(ePt.OffsetX(-valveGapX - valveSize), ePt1));
                sprayOut.PipeLine.Add(new Line(sPt1, sPt12));
                sprayOut.PipeLine.Add(new Line(ePt1, ePt12));
                sprayOut.PipeLine.Add(new Line(sPt12, sPt2));
                sprayOut.PipeLine.Add(new Line(sPt2, ePt2));

                sprayOut.SprayBlocks.Add(new Block.SprayBlock("遥控信号阀", sPt.OffsetX(valveGapX)));
                sprayOut.SprayBlocks.Add(new Block.SprayBlock("遥控信号阀", ePt1.OffsetX(valveGapX)));

                Point3d nextPt = ePt12; //起始点
                Point3d curPt = ePt12;
                bool valveFlag = false;
                bool firstAlarmValve = true;//第一个报警阀

                for (int i = 1; i < rstPath.Count - 1; i++)
                {
                    try
                    {
                        var pt = rstPath[i];
                        if(pt._pt.DistanceTo(new Point3d(18240983.2767, 21199036.1798, 0)) < 10)
                        {
                            ;
                        }
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
                            if (valveFlag)
                            {
                                valveFlag = false;

                                var alarmValve = new AlarmValveSys(curPt, fireAreaIndex, floorHeight);
                                spraySystem.BranchPtDic.Add(pt, alarmValve.EndPt);
                                sprayOut.AlarmValves.Add(alarmValve);//插入湿式报警阀平面
                                foreach (var apt in sprayIn.AlarmTextDic.Keys)
                                {
                                    if (apt._pt.DistanceTo(pt._pt) < 50)
                                    {
                                        var text = new Text(sprayIn.AlarmTextDic[apt], nextPt.OffsetXY(-200, -550));
                                        sprayOut.Texts.Add(text);
                                    }
                                }

                                spraySystem.FireAreaStPtDic.Add(pt, ePt2.OffsetXY(sprayIn.PipeGap, 5200));
                            }
                            else
                            {
                                var alarmValve = new AlarmValveSys(nextPt, fireAreaIndex, floorHeight);
                                spraySystem.BranchPtDic.Add(pt, alarmValve.EndPt);
                                sprayOut.AlarmValves.Add(alarmValve);//插入湿式报警阀平面
                                foreach (var apt in sprayIn.AlarmTextDic.Keys)
                                {
                                    if (apt._pt.DistanceTo(pt._pt) < 50)
                                    {
                                        var text = new Text(sprayIn.AlarmTextDic[apt], nextPt.OffsetXY(-200, -550));
                                        sprayOut.Texts.Add(text);
                                    }
                                }
                                spraySystem.FireAreaStPtDic.Add(pt, ePt2.OffsetXY(sprayIn.PipeGap, 5200));

                                sprayOut.PipeLine.Add(new Line(curPt, nextPt));//添加湿式报警阀前面一段线
                                curPt = new Point3d(nextPt.X, nextPt.Y, 0);
                                nextPt = nextPt.OffsetX(alarmGap);
                            }

                            if (!spraySystem.BranchDic.ContainsKey(pt))
                            {
                                continue;
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
    }
}
