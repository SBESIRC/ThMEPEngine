using Autodesk.AutoCAD.Geometry;
using System.Linq;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.UndergroundSpraySystem.Model;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.UndergroundSpraySystem.Block;

namespace ThMEPWSS.UndergroundSpraySystem.Service
{
    class BranchLoop
    {
        public static void Get(SprayOut sprayOut, SpraySystem spraySystem, SprayIn sprayIn)
        {
            double valveSize = 300;
            double valveGapX = 50;
            double alarmGap = 1500;
            double valveGapX2 = 600;
            double floorHeight = sprayIn.FloorHeight;
            foreach (var rstPath in spraySystem.BranchLoops)
            {
                int alarmValveNums = spraySystem.SubLoopAlarmsDic[rstPath.Last()][0];
                int fireAreaIndex = 0;//当前支管的防火分区index
                Point3d sPt = spraySystem.BranchLoopPtDic[rstPath.First()];
                Point3d ePt = spraySystem.BranchLoopPtDic[rstPath.Last()];
                Point3d ePt1 = ePt.OffsetX(-2 * valveGapX - valveSize);
                Point3d sPt1 = sPt.OffsetX(2 * valveGapX + valveSize);
                Point3d ePt2 = ePt1.OffsetXY(1700 + (alarmValveNums - 1) * alarmGap + 1000, -floorHeight * 0.64);
                Point3d sPt2 = ePt2.OffsetY(- 0.06 * floorHeight);
                sprayOut.PipeLine.Add(new Line(sPt, sPt.OffsetX(valveGapX)));
                sprayOut.PipeLine.Add(new Line(ePt, ePt.OffsetX(-valveGapX)));
                sprayOut.PipeLine.Add(new Line(sPt.OffsetX(valveGapX + valveSize), sPt1));
                sprayOut.PipeLine.Add(new Line(ePt.OffsetX(-valveGapX - valveSize), ePt1));
                sprayOut.PipeLine.Add(new Line(sPt1, sPt1.OffsetY(- floorHeight * 0.7)));
                sprayOut.PipeLine.Add(new Line(ePt1, ePt1.OffsetY(- floorHeight * 0.64)));
                sprayOut.PipeLine.Add(new Line(sPt1.OffsetY(-floorHeight * 0.7), sPt2));
                sprayOut.PipeLine.Add(new Line(sPt2, ePt2));

                sprayOut.SprayBlocks.Add(new Block.SprayBlock("遥控信号阀", sPt.OffsetX(valveGapX)));
                sprayOut.SprayBlocks.Add(new Block.SprayBlock("遥控信号阀", ePt1.OffsetX(valveGapX)));

                Point3d stPt = ePt1.OffsetXY(1700, -floorHeight * 0.64);//起始点
                sprayOut.PipeLine.Add(new Line(ePt1.OffsetY(-floorHeight * 0.64), stPt));
                for (int i = 1; i < rstPath.Count - 1; i++)
                {
                    var pt = rstPath[i];
                    var preType = sprayIn.PtTypeDic[rstPath[i - 1]];
                    var nextType = sprayIn.PtTypeDic[rstPath[i + 1]];
                    var type = sprayIn.PtTypeDic[pt];

                    if (type.Equals("SignalValve"))
                    {
                        if(preType.Equals("BranchLoop") || nextType.Equals("BranchLoop"))
                        {
                            continue;
                        }
                        sprayOut.PipeLine.Add(new Line(stPt, stPt.OffsetX(valveGapX2)));
                        stPt = stPt.OffsetX(valveGapX2);
                        sprayOut.SprayBlocks.Add(new Block.SprayBlock("遥控信号阀", stPt));
                        sprayOut.PipeLine.Add(new Line(stPt.OffsetX(valveSize), stPt.OffsetX(valveGapX2 + valveSize)));
                        stPt = stPt.OffsetX(valveGapX2 + valveSize);
                    }
                    if (type.Contains("AlarmValve"))
                    {
                        var alarmValve = new AlarmValveSys(stPt, fireAreaIndex, floorHeight);
                        spraySystem.BranchPtDic.Add(pt, alarmValve.EndPt);
                        sprayOut.AlarmValves.Add(alarmValve);//插入湿式报警阀平面
                        spraySystem.FireAreaStPtDic.Add(pt, ePt2.OffsetXY(1700, 4900));

                        if(nextType.Contains("AlarmValve"))
                        {
                            sprayOut.PipeLine.Add(new Line(stPt, stPt.OffsetX(alarmGap)));
                            stPt = stPt.OffsetX(alarmGap);
                        }
                        if(!spraySystem.BranchDic.ContainsKey(pt))
                        {
                            continue;
                        }
                        foreach(var tpt in spraySystem.BranchDic[pt])//遍历支路端点
                        {
                            if(!sprayIn.TermPtDic.ContainsKey(tpt))
                            {
                                continue;
                            }
                            if(sprayIn.TermPtDic[tpt].Type == 1)//端点类型是防火分区
                            {
                                fireAreaIndex++;//防火分区index+1
                                break;//统计到一个就退出
                            }
                        }
                    }
                }
                sprayOut.PipeLine.Add(new Line(stPt, ePt2));
            }
        }
    }
}
