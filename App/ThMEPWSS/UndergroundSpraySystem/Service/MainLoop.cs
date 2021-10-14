using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.Block;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundSpraySystem.Service
{
    public class MainLoop
    {
        public static void Get(SprayOut sprayOut, SpraySystem spraySystem, SprayIn sprayIn)
        {
            var rstPath = spraySystem.MainLoop;
            var floorHeight = sprayIn.FloorHeight;
            var stPt1 = sprayOut.PipeInsertPoint;
            var stPt = stPt1;
            int flag = 1;
            bool pangTong = true;

            for (int i = 0; i < rstPath.Count - 1; i++)
            {
                try
                {
                    var pt = rstPath[i];
                    var nextPt = rstPath[i + 1];
                    if (i == 0)
                    {
                        stPt = GetMainPt(stPt, ref sprayOut);
                    }
                    if(i==10)
                    {
                        ;
                    }
                    if(!sprayIn.PtTypeDic.ContainsKey(pt))
                    {
                        continue;
                    }
                    if (sprayIn.PtTypeDic[pt].Contains("MainLoop"))
                    {
                        continue;
                    }
                    if (sprayIn.PtTypeDic[pt].Contains("SubLoop"))
                    {
                        stPt = GetSubLoopPt(pt, stPt, sprayOut, ref flag, floorHeight, spraySystem, sprayIn);
                        continue;
                    }
                    if (sprayIn.PtTypeDic[pt].Contains("Branch"))
                    {
                        stPt = GetBranchPt(pt, stPt, sprayOut, spraySystem, sprayIn.PtTypeDic[nextPt],
                            floorHeight * 0.06, sprayIn);
                        continue;
                    }
                    if (sprayIn.PtTypeDic[pt].Contains("PangTong"))
                    {
                        stPt = GetPangTongPt(stPt, sprayOut, floorHeight * 0.06, pangTong);
                        pangTong = !pangTong;
                        continue;
                    }
                    if (sprayIn.PtTypeDic[pt].Contains("DieValve"))
                    {
                        ;
                    }
                }
                catch
                {
                    ;
                }
                
            }
            GetDetail(stPt, sprayOut, floorHeight * 0.06);
        }
        private static Point3d GetMainPt(Point3d stPt, ref SprayOut sprayOut)
        {
            var pt = stPt.OffsetX(500);
            sprayOut.PipeLine.Add(new Line(stPt, pt));
            return pt;
        }
        private static Point3d GetSubLoopPt(Point3dEx curPt, Point3d stPt, SprayOut sprayOut, ref int flag, 
            double floorHeight, SpraySystem spraySystem, SprayIn sprayIn)
        {

            double height = 0.12 * floorHeight;
            var pt = stPt;
           
            
            if(spraySystem.SubLoopPtDic.ContainsKey(curPt))
            {
                spraySystem.SubLoopPtDic.Remove(curPt);
            }

            var alarmNums = 0;
            var branchLoopNums = 0;
            var fireNums = 0;
            if (spraySystem.SubLoopAlarmsDic.ContainsKey(curPt))
            {
                foreach (var num in spraySystem.SubLoopAlarmsDic[curPt])
                {
                    alarmNums += num;
                    branchLoopNums += 1;
                }
            }
            if (flag == 1)
            {
                sprayOut.PipeLine.Add(new Line(stPt, stPt.OffsetY(-height)));
                pt = stPt.OffsetX(5150 * branchLoopNums + 1500 * (alarmNums - 1) + 2500);
                sprayOut.PipeLine.Add(new Line(stPt, pt));
                spraySystem.TempSubLoopStartPt = new Point3d(stPt.X, stPt.Y, 0);
                spraySystem.SubLoopPtDic.Add(curPt, stPt.OffsetY(-height));//保存支环的起始点
            }
            else
            {
                var waterPumpNum = 0;//水泵接合器数目
                if (spraySystem.SubLoopBranchPtDic.ContainsKey(curPt))
                {
                    foreach(var bpt in spraySystem.SubLoopBranchPtDic[curPt])
                    {
                        if(spraySystem.BranchDic.ContainsKey(bpt))
                        {
                            if (spraySystem.BranchDic[bpt].Count == 1)//单支路
                            {
                                var tpt = spraySystem.BranchDic[bpt][0];
                                if(sprayIn.TermPtTypeDic.ContainsKey(tpt))
                                {
                                    if (sprayIn.TermPtTypeDic[tpt] == 3)//支路末端是水泵接合器
                                    {
                                        waterPumpNum++;
                                    }
                                }
                            }
                        }
                        
                    }

                    var branchNums = spraySystem.SubLoopBranchDic[curPt];//支路数
                    sprayOut.PipeLine.Add(new Line(stPt, stPt.OffsetX(branchNums * 1400 + waterPumpNum * 3200)));
                    stPt = stPt.OffsetX(branchNums * 1400 + waterPumpNum * 3200);
                }
                sprayOut.PipeLine.Add(new Line(stPt, stPt.OffsetY(-height)));
                spraySystem.SubLoopPtDic.Add(curPt, stPt.OffsetY(-height));//保存支环的起始点
                pt = spraySystem.TempSubLoopStartPt;
                if (spraySystem.SubLoopAlarmsDic.ContainsKey(curPt))//支路的报警阀数目
                {
                    pt = pt.OffsetX(5150 * branchLoopNums + 1500 * (alarmNums - 1) + 2500);
                }
                if (spraySystem.SubLoopFireAreasDic.ContainsKey(curPt))//支路的防火分区数目
                {
                    foreach(var num in spraySystem.SubLoopFireAreasDic[curPt])
                    {
                        fireNums += num;
                    }
                    pt = pt.OffsetX(fireNums * 5500 - 2500 * branchLoopNums + 1500);
                }
                sprayOut.PipeLine.Add(new Line(stPt, pt));
            }
            flag *= -1;
            return pt;
        }
        private static Point3d GetPangTongPt(Point3d stPt, SprayOut sprayOut, double height, bool pangTong)
        {
            if(!pangTong)
            {
                return stPt;
            }
            double dist = (height - 300) / 2;
            var pt = stPt.OffsetX(1400);
            sprayOut.PipeLine.Add(new Line(stPt, pt));
            sprayOut.PipeLine.Add(new Line(stPt, stPt.OffsetY(-dist)));
            sprayOut.PipeLine.Add(new Line(stPt.OffsetY(dist- height), stPt.OffsetY(-height)));
            sprayOut.SprayBlocks.Add(new SprayBlock("遥控信号阀", stPt.OffsetY(dist - height), Math.PI / 2));
            return pt;
        }
        private static Point3d GetBranchPt(Point3dEx curPt, Point3d stPt, SprayOut sprayOut, 
            SpraySystem spraySystem, string nextPtType, double height, SprayIn sprayIn)
        {
            if (spraySystem.BranchDic[curPt].Count == 1)//单支路
            {
                var tpt = spraySystem.BranchDic[curPt][0];
                if (sprayIn.TermPtTypeDic[tpt] == 3)//支路末端是水泵接合器
                {
                    sprayOut.PipeLine.Add(new Line(stPt, stPt.OffsetX(3200)));
                    stPt = stPt.OffsetX(3200);//起点右移
                }
            }
            double dist = 1000;
            if (nextPtType.Contains("Branch"))
            {
                dist = 1400;
            }
            var pt = stPt.OffsetX(dist);
            sprayOut.PipeLine.Add(new Line(stPt, pt));
            sprayOut.PipeLine.Add(new Line(stPt, stPt.OffsetY(height)));
            if(spraySystem.BranchLoopPtDic.ContainsKey(curPt))
            {
                spraySystem.BranchLoopPtDic.Remove(curPt);
            }
            spraySystem.BranchLoopPtDic.Add(curPt, stPt.OffsetY(height));
            if(spraySystem.BranchPtDic.ContainsKey(curPt))
            {
                spraySystem.BranchPtDic.Remove(curPt);
            }
            spraySystem.BranchPtDic.Add(curPt, stPt.OffsetY(height));
            return pt;
        }
        private static void GetDetail(Point3d stPt, SprayOut sprayOut, double height)
        {
            sprayOut.PipeLine.Add(new Line(stPt, stPt.OffsetY(-height)));
            sprayOut.PipeLine.Add(new Line(sprayOut.PipeInsertPoint.OffsetY(-height), stPt.OffsetY(-height)));
        }
    }
}
