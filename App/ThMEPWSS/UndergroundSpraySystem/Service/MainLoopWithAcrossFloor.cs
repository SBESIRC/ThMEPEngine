using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.Block;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.UndergroundSpraySystem.Model;
using System.Linq;
using ThMEPWSS.Uitl.ExtensionsNs;
using System.Collections.Generic;

namespace ThMEPWSS.UndergroundSpraySystem.Service
{
    public class MainLoopWithAcrossFloor
    {
        public static void Get(SprayOut sprayOut, SpraySystem spraySystem, SprayIn sprayIn, int mainLoopIndex = 0)
        {
            var rstPath = spraySystem.MainLoop;
            var floorHeight = sprayIn.FloorHeight;
            var stPt1 = sprayOut.PipeInsertPoint;
            if (mainLoopIndex == 1)
            {
                stPt1 = stPt1.OffsetY(-floorHeight);
            }
            var stPt = stPt1;
            int flag = 1;//支环类型
            int acrossFlag = 1;//跨层主环类型
            int currentFloor = Convert.ToInt32(sprayOut.CurrentFloor.Last());
            double LastBranchLoopAlarmLength = 0;//前一个环路的报警阀间长度
            bool lastPtIsBranchLoop = false;//上一个点的类型是环路点
            int LastBranchLoopIndex = 100;//默认100(基本用不到)，上一层 +1，下一层 -1
            double curFloorMaxSite = 0;//当前层最大位置（当前层防火分区最远的位置）
            double upperFloorMaxSite = 0;//上一层最大移动位置（记录环管终点位置）
            double curMaxSite = 0;//当前最大位置，用于处理当前层向上延伸立管和上一层相撞的情况

            var usedPts = new List<Point3dEx>();
            for (int i = 0; i < rstPath.Count - 1; i++)
            {
                try
                {
                    var pt = rstPath[i];
                    if (usedPts.Contains(pt)) 
                        continue;
                    var nextPt = rstPath[i + 1];
                    var line = new Line(pt._pt, nextPt._pt);
                    var lineEx = new LineSegEx(line);
                    var pipeDn = "";
                    if (sprayIn.PtDNDic.ContainsKey(lineEx))
                    {
                        pipeDn += sprayIn.PtDNDic[lineEx];
                    }

                    foreach (var slptEx in sprayIn.SlashDic.Keys)
                    {
                        var slpt = slptEx._pt;
                        if (line.GetClosestPointTo(slpt, false).DistanceTo(slpt) < 10.0)
                        {
                            pipeDn += sprayIn.SlashDic[slptEx];
                            break;
                        }
                    }
                    if (pipeDn.StartsWith("DN"))
                    {
                        sprayOut.Texts.Add(new Text(pipeDn, stPt.OffsetX(100)));
                        sprayOut.Texts.Add(new Text(pipeDn, stPt.OffsetXY(100, -600)));
                    }
                    if (i == 0)
                    {
                        stPt = GetMainPt(stPt, ref sprayOut);
                    }
                    if (!sprayIn.PtTypeDic.ContainsKey(pt)) continue;

                    var ptType = sprayIn.PtTypeDic[pt];//当前点类型
                    if (ptType.Contains("MainLoopAcross"))
                    {
                        stPt = GetAcrossMainLoop(stPt, sprayOut, spraySystem, floorHeight, ref acrossFlag);
                    }
                    if (ptType.Contains("MainLoop")) continue;

                    if (ptType.Contains("BranchLoop"))
                    {
                        lastPtIsBranchLoop = true;
                        var typeNum = Convert.ToInt32(ptType.Last());
                        if (typeNum == currentFloor)//楼层号是当前层
                        {
                            if (spraySystem.LoopPtPairs.ContainsKey(pt))
                            {
                                stPt = GetBranchLoopPt2(pt, stPt, sprayOut, ref flag, spraySystem, sprayIn, LastBranchLoopIndex,
                              ref LastBranchLoopAlarmLength, ref curFloorMaxSite, lastPtIsBranchLoop, upperFloorMaxSite, curMaxSite);
                                LastBranchLoopIndex = 0;
                                var entPt = spraySystem.LoopPtPairs[pt];
                                stPt = GetBranchLoopPt2(entPt, stPt, sprayOut, ref flag, spraySystem, sprayIn, LastBranchLoopIndex,
                                ref LastBranchLoopAlarmLength, ref curFloorMaxSite, lastPtIsBranchLoop, upperFloorMaxSite, curMaxSite);
                                LastBranchLoopIndex = 1;
                                usedPts.Add(entPt);
                            }
                            else
                            {
                                stPt = GetBranchLoopPt(pt, stPt, sprayOut, ref flag, spraySystem, sprayIn, LastBranchLoopIndex,
                               ref LastBranchLoopAlarmLength, ref curFloorMaxSite, lastPtIsBranchLoop, upperFloorMaxSite, curMaxSite);
                                LastBranchLoopIndex = 0;
                            }
                        }
                        if (typeNum < currentFloor)//楼层号是上一层
                        {
                            stPt = GetBranchLoopPtUpper(pt, stPt, sprayOut, ref flag, floorHeight, spraySystem, sprayIn,
                                LastBranchLoopIndex, ref LastBranchLoopAlarmLength, ref curFloorMaxSite, ref upperFloorMaxSite, ref curMaxSite);
                            LastBranchLoopIndex = 1;
                        }
                        if(typeNum > currentFloor)//楼层号是下一层
                        {
                            stPt = GetBranchLoopPtDown(pt, stPt, sprayOut, ref flag, floorHeight, spraySystem, sprayIn,
                                LastBranchLoopIndex, ref LastBranchLoopAlarmLength, ref curFloorMaxSite, ref upperFloorMaxSite, ref curMaxSite);
                            LastBranchLoopIndex = 1;
                        }
                        continue;
                    }
                    if (ptType.Contains("Branch"))
                    {
                        stPt = GetBranchPt(pt, stPt, sprayOut, spraySystem, sprayIn.PtTypeDic[nextPt],
                            400, sprayIn, lastPtIsBranchLoop, LastBranchLoopAlarmLength);
                        lastPtIsBranchLoop = false;
                        continue;
                    }

                    if (ptType.Contains("SignalValve"))
                    {
                        GetSignalValvePt(stPt, sprayOut, sprayIn);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    ;
                }

            }
            GetDetail(stPt1, stPt, sprayOut, sprayIn, 600);

        }

        public static void GetInOtherFloor(SprayOut sprayOut, SpraySystem spraySystem, SprayIn sprayIn, int mainLoopIndex = 0)
        {
            var rstPath = spraySystem.MainLoop;
            var floorHeight = sprayIn.FloorHeight;
            var stPt1 = sprayOut.PipeInsertPoint;
            if(mainLoopIndex != 0)
            {
                stPt1 = spraySystem.TempMainLoopPt;
            }
   
            var stPt = stPt1;
            int flag = 1;//支环类型
            int acrossFlag = 1;//跨层主环类型
            int currentFloor = Convert.ToInt32(sprayOut.CurrentFloor.Last());
            double LastBranchLoopAlarmLength = 0;//前一个环路的报警阀间长度
            bool lastPtIsBranchLoop = false;//上一个点的类型是环路点
            int LastBranchLoopIndex = 100;//默认100(基本用不到)，上一层 +1，下一层 -1
            double curFloorMaxSite = 0;//当前层最大位置（当前层防火分区最远的位置）
            double upperFloorMaxSite = 0;//上一层最大移动位置（记录环管终点位置）
            double curMaxSite = 0;//当前最大位置，用于处理当前层向上延伸立管和上一层相撞的情况

            var usedPts = new List<Point3dEx>();
            for (int i = 0; i < rstPath.Count - 1; i++)
            {
                try
                {
                    var pt = rstPath[i];
                    if (usedPts.Contains(pt)) continue;
                    var nextPt = rstPath[i + 1];
                    var line = new Line(pt._pt, nextPt._pt);
                    var lineEx = new LineSegEx(line);
                    var pipeDn = "";
                    if(sprayIn.PtDNDic.ContainsKey(lineEx))
                    {
                        pipeDn += sprayIn.PtDNDic[lineEx];
                    }

                    foreach(var slptEx in sprayIn.SlashDic.Keys)
                    {
                        var slpt = slptEx._pt;
                        if(line.GetClosestPointTo(slpt,false).DistanceTo(slpt) < 10.0)
                        {
                            pipeDn += sprayIn.SlashDic[slptEx];
                            break;
                        }
                    }
                    if (pipeDn.StartsWith("DN"))
                    {
                        sprayOut.Texts.Add(new Text(pipeDn, stPt.OffsetX(100)));
                        sprayOut.Texts.Add(new Text(pipeDn, stPt.OffsetXY(100, -600)));
                    }
                    if (i == 0)
                    {
                        stPt = GetMainPt(stPt, ref sprayOut);
                    }
                    if (!sprayIn.PtTypeDic.ContainsKey(pt)) continue;
                    
                    var ptType = sprayIn.PtTypeDic[pt];//当前点类型
                    if(ptType.Contains("MainLoopAcross"))
                    {
                        if (spraySystem.LoopPtPairs.ContainsKey(pt))
                        {
                            stPt = GetAcrossMainLoop(stPt, sprayOut, spraySystem, floorHeight, ref acrossFlag);
                            var entPt = spraySystem.LoopPtPairs[pt];
                            stPt = GetAcrossMainLoop(stPt, sprayOut, spraySystem, floorHeight, ref acrossFlag, true);
                            usedPts.Add(entPt);
                        }
                        else
                        {
                            stPt = GetAcrossMainLoop(stPt, sprayOut, spraySystem, floorHeight, ref acrossFlag);
                        }
                    }
                    if (ptType.Contains("MainLoop")) continue;
                    
                    if (ptType.Contains("BranchLoop"))
                    {
                        lastPtIsBranchLoop = true;
         
                        stPt = GetBranchLoopPt(pt, stPt, sprayOut, ref flag, spraySystem, sprayIn, LastBranchLoopIndex, 
                                ref LastBranchLoopAlarmLength, ref curFloorMaxSite, lastPtIsBranchLoop, upperFloorMaxSite, curMaxSite);
                            LastBranchLoopIndex = 0;
                        continue;
                    }
                    if (ptType.Contains("Branch"))
                    {
                        stPt = GetBranchPt(pt, stPt, sprayOut, spraySystem, sprayIn.PtTypeDic[nextPt],
                            400, sprayIn, lastPtIsBranchLoop, LastBranchLoopAlarmLength);
                        lastPtIsBranchLoop = false;
                        continue;
                    }

                    if (ptType.Contains("SignalValve"))
                    {
                        GetSignalValvePt(stPt, sprayOut, sprayIn);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    ;
                }

            }
            GetDetail(stPt1, stPt, sprayOut, sprayIn, 600);

        }

      
        private static Point3d GetMainPt(Point3d stPt, ref SprayOut sprayOut)
        {
            var pt = stPt.OffsetX(500);
            sprayOut.PipeLine.Add(new Line(stPt, pt));
            return pt;
        }

        /// <summary>
        /// 实现跨层主环的逻辑
        /// </summary>
        /// <param name="stPt"></param>
        /// <param name="sprayOut"></param>
        /// <returns></returns>
        private static Point3d GetAcrossMainLoop(Point3d stPt, SprayOut sprayOut, SpraySystem spraySystem, double floorHeight, ref int acrossFlag, bool newDrawing = false)
        {
            var offsetVal = 0.0;
            if(newDrawing)
            {
                offsetVal = -600.0;
            }
            
            
                if (acrossFlag == 1)
                {
                    var pt = stPt.OffsetX(600);
                    sprayOut.PipeLine.Add(new Line(stPt, pt));
                    sprayOut.PipeLine.Add(new Line(stPt.OffsetY(offsetVal), stPt.OffsetY(-floorHeight - 600)));
                    sprayOut.PipeLine.Add(new Line(stPt.OffsetXY(600, -floorHeight - 600), stPt.OffsetY(-floorHeight - 600)));
                    acrossFlag = -1;
                    return pt;
                }
                else
                {
                    var pt = stPt.OffsetX(1200);
                    sprayOut.PipeLine.Add(new Line(stPt, pt));
                    sprayOut.PipeLine.Add(new Line(stPt.OffsetY(offsetVal), stPt.OffsetY(-floorHeight)));
                    acrossFlag = 1;
                    spraySystem.TempMainLoopPt = stPt.OffsetY(-floorHeight);
                    return pt;
                }
            


        }

        /// <summary>
        /// 向上分支的支路
        /// </summary>
        /// <param name="curPt"></param>
        /// <param name="stPt"></param>
        /// <param name="sprayOut"></param>
        /// <param name="flag"></param>
        /// <param name="floorHeight"></param>
        /// <param name="spraySystem"></param>
        /// <param name="sprayIn"></param>
        /// <param name="LastBranchLoopIndex"></param>
        /// <returns></returns>
        private static Point3d GetBranchLoopPtUpper(Point3dEx curPt, Point3d stPt, SprayOut sprayOut, ref int flag,
           double floorHeight, SpraySystem spraySystem, SprayIn sprayIn, int LastBranchLoopIndex, 
           ref double LastBranchLoopAlarmLength, ref double curFloorMaxSite, ref double upperFloorMaxSite, ref double curMaxSite)
        {
            double height = -550;
            var alarmNums = 0;//当前支环（报警阀间）上的报警阀数目
            var branchLoopNums = 0;
            var fireNums = 0;//当前支环（报警阀间）上的防火分区数目
            var pt = new Point3d();
            if (spraySystem.SubLoopAlarmsDic.ContainsKey(curPt))
            {
                foreach (var num in spraySystem.SubLoopAlarmsDic[curPt])
                {
                    alarmNums += num;
                    branchLoopNums += 1;
                }
            }
            if (flag == 1)//支环起点
            {
                if (LastBranchLoopIndex == 0)//从上一层跳到当前层
                {
                    stPt = stPt.OffsetX(-LastBranchLoopAlarmLength);

                    var dist1 = curFloorMaxSite;//  当前层移动到的位置的X值
                    var dist2 = stPt.X - LastBranchLoopAlarmLength;//  当前层左移到最左的X值
                    var curSiteX = Math.Max(dist1, dist2);//取二者最大（右）值
                    stPt = new Point3d(curSiteX, stPt.Y, 0);
                }
                sprayOut.PipeLine.Add(new Line(stPt, stPt.OffsetY(-height)));
                pt = stPt.OffsetX(1200);
                sprayOut.PipeLine.Add(new Line(stPt, pt));
                spraySystem.TempSubLoopStartPt = new Point3d(stPt.X, stPt.Y, 0);
                spraySystem.BranchLoopPtDic.Add(curPt, stPt.OffsetY(-height));//保存支环的起始点    
            }
            else
            {
                var waterPumpNum = GetWaterPumpNum(curPt, spraySystem, sprayIn);//水泵接合器数目
                if (spraySystem.SubLoopBranchPtDic.ContainsKey(curPt))
                {
                    var branchNums = spraySystem.SubLoopBranchDic[curPt];//支路数
                    sprayOut.PipeLine.Add(new Line(stPt, stPt.OffsetX(branchNums * sprayIn.PipeGap + waterPumpNum * 5000)));
                    stPt = stPt.OffsetX(branchNums * sprayIn.PipeGap + waterPumpNum * 5000);
                }

                sprayOut.PipeLine.Add(new Line(stPt, stPt.OffsetY(-height)));
                spraySystem.BranchLoopPtDic.Add(curPt, stPt.OffsetY(-height));//保存支环的起始点
                pt = spraySystem.TempSubLoopStartPt;
                if (spraySystem.SubLoopAlarmsDic.ContainsKey(curPt))//支路的报警阀数目
                {
                    pt = pt.OffsetX(5150 * branchLoopNums + sprayIn.PipeGap * (alarmNums - 1) + 2500);
                }
                if (spraySystem.SubLoopFireAreasDic.ContainsKey(curPt))//支路的防火分区数目
                {
                    foreach (var num in spraySystem.SubLoopFireAreasDic[curPt])
                    {
                        fireNums += num;
                    }
                    pt = pt.OffsetX(fireNums * 5500 - 2500 * branchLoopNums + 1500);
                }
                sprayOut.PipeLine.Add(new Line(stPt, pt));
                upperFloorMaxSite = stPt.X;//记录上一层的环管终点位置
                curMaxSite = pt.X;
            }
            flag *= -1;
            return pt;
        }


        /// <summary>
        /// 向下分支的支路
        /// </summary>
        /// <param name="curPt"></param>
        /// <param name="stPt"></param>
        /// <param name="sprayOut"></param>
        /// <param name="flag"></param>
        /// <param name="floorHeight"></param>
        /// <param name="spraySystem"></param>
        /// <param name="sprayIn"></param>
        /// <param name="LastBranchLoopIndex"></param>
        /// <returns></returns>
        private static Point3d GetBranchLoopPtDown(Point3dEx curPt, Point3d stPt, SprayOut sprayOut, ref int flag,
           double floorHeight, SpraySystem spraySystem, SprayIn sprayIn, int LastBranchLoopIndex,
           ref double LastBranchLoopAlarmLength, ref double curFloorMaxSite, ref double upperFloorMaxSite, ref double curMaxSite)
        {
            double height = 1200;
            var alarmNums = 0;//当前支环（报警阀间）上的报警阀数目
            var branchLoopNums = 0;
            var fireNums = 0;//当前支环（报警阀间）上的防火分区数目
            var pt = new Point3d();
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
                if (LastBranchLoopIndex == 0)//从上一层跳到当前层
                {
                    stPt = stPt.OffsetX(-LastBranchLoopAlarmLength);

                    var dist1 = curFloorMaxSite;//  当前层移动到的位置的X值
                    var dist2 = stPt.X - LastBranchLoopAlarmLength;//  当前层左移到最左的X值
                    var curSiteX = Math.Max(dist1, dist2);//取二者最大（右）值
                    stPt = new Point3d(curSiteX, stPt.Y, 0);
                }
                var pt1 = stPt.OffsetY(-height - floorHeight);
                pt = stPt.OffsetX(1200);
                sprayOut.PipeLine.Add(new Line(stPt, pt1));
                sprayOut.PipeLine.Add(new Line(stPt, pt));
                spraySystem.TempSubLoopStartPt = new Point3d(stPt.X, stPt.Y, 0);
                spraySystem.BranchLoopPtDic.Add(curPt, pt1);//保存支环的起始点    
            }
            else
            {
                var waterPumpNum = GetWaterPumpNum(curPt, spraySystem, sprayIn);//水泵接合器数目
                if (spraySystem.SubLoopBranchPtDic.ContainsKey(curPt))
                {
                    var branchNums = spraySystem.SubLoopBranchDic[curPt];//支路数
                    sprayOut.PipeLine.Add(new Line(stPt, stPt.OffsetX(branchNums * sprayIn.PipeGap + waterPumpNum * 5000)));
                    stPt = stPt.OffsetX(branchNums * sprayIn.PipeGap + waterPumpNum * 5000);
                }
                var pt1 = stPt.OffsetY(-height-floorHeight);
                sprayOut.PipeLine.Add(new Line(stPt, pt1));
                spraySystem.BranchLoopPtDic.Add(curPt, pt1);//保存支环的起始点
                pt = spraySystem.TempSubLoopStartPt;
                if (spraySystem.SubLoopAlarmsDic.ContainsKey(curPt))//支路的报警阀数目
                {
                    pt = pt.OffsetX(5150 * branchLoopNums + sprayIn.PipeGap * (alarmNums - 1) + 2500);
                }
                if (spraySystem.SubLoopFireAreasDic.ContainsKey(curPt))//支路的防火分区数目
                {
                    foreach (var num in spraySystem.SubLoopFireAreasDic[curPt])
                    {
                        fireNums += num;
                    }
                    pt = pt.OffsetX(fireNums * 5500 - 2500 * branchLoopNums + 1500);
                }
                sprayOut.PipeLine.Add(new Line(stPt, pt));
                upperFloorMaxSite = stPt.X;//记录上一层的环管终点位置
                curMaxSite = pt.X;
            }
            flag *= -1;
            return pt;
        }


        /// <summary>
        /// 本层支路
        /// </summary>
        /// <param name="curPt"></param>
        /// <param name="stPt"></param>
        /// <param name="sprayOut"></param>
        /// <param name="flag"></param>
        /// <param name="spraySystem"></param>
        /// <param name="sprayIn"></param>
        /// <param name="LastBranchLoopIndex"></param>
        /// <returns></returns>
        private static Point3d GetBranchLoopPt(Point3dEx curPt, Point3d stPt, SprayOut sprayOut, ref int flag,
            SpraySystem spraySystem, SprayIn sprayIn, int LastBranchLoopIndex, 
            ref double LastBranchLoopAlarmLength, ref double curFloorMaxSite, bool lastPtIsBranchLoop, double upperFloorMaxSite, double curMaxSite)
        {
            double height = 1200;
            var alarmNums = 0;//当前支环（报警阀间）上的报警阀数目
            var branchLoopNums = 0;
            var fireNums = 0;//当前支环（报警阀间）上的防火分区数目
            var pt = new Point3d();
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
                if(LastBranchLoopIndex == 1 && lastPtIsBranchLoop)//当前层的上一层
                {
                    var branchLoopCross = HasVerticalCrossUpperFloor(curPt, spraySystem, sprayIn);
                    if(branchLoopCross)
                    {
                        var dist1 = curFloorMaxSite;//  当前层移动到的位置的X值
                        var BranchLoopLength = GetBranchLoopLength(curPt, spraySystem, sprayIn, alarmNums, branchLoopNums);
                        var dist2 = stPt.X - BranchLoopLength;//  当前层左移到最左的X值
                        var dist3 = upperFloorMaxSite;//上一层立管结束的位置
                        var dist4 = curMaxSite;
                        var curSiteX = Math.Max(Math.Max(dist1, dist2), Math.Max(dist3, dist4));//取三者最大（右）值
                        stPt = new Point3d(curSiteX, stPt.Y, 0);
                    }
                    else
                    {
                        var dist1 = curFloorMaxSite;//  当前层移动到的位置的X值
                        var BranchLoopLength = GetBranchLoopLength(curPt, spraySystem, sprayIn, alarmNums, branchLoopNums);
                        var dist2 = stPt.X - BranchLoopLength;//  当前层左移到最左的X值
                        var dist3 = upperFloorMaxSite;//上一层立管结束的位置
                        var curSiteX = Math.Max(Math.Max(dist1, dist2), dist3);//取三者最大（右）值
                        stPt = new Point3d(curSiteX, stPt.Y, 0);
                    }
                    
                }
                sprayOut.PipeLine.Add(new Line(stPt, stPt.OffsetY(-height)));
                pt = stPt.OffsetX(1200);
                sprayOut.PipeLine.Add(new Line(stPt, pt));
                spraySystem.TempSubLoopStartPt = new Point3d(stPt.X, stPt.Y, 0);
                spraySystem.BranchLoopPtDic.Add(curPt, stPt.OffsetY(-height));//保存支环的起始点
            }
            else
            {
                if (spraySystem.SubLoopBranchPtDic.ContainsKey(curPt))
                {
                    var branchNums = spraySystem.SubLoopBranchDic[curPt];//支路数
                    sprayOut.PipeLine.Add(new Line(stPt, stPt.OffsetX(branchNums * sprayIn.PipeGap)));
                    stPt = stPt.OffsetX(branchNums * sprayIn.PipeGap);
                }

                sprayOut.PipeLine.Add(new Line(stPt, stPt.OffsetY(-height)));
                spraySystem.BranchLoopPtDic.Add(curPt, stPt.OffsetY(-height));//保存支环的起始点
                pt = spraySystem.TempSubLoopStartPt;
                if (spraySystem.SubLoopAlarmsDic.ContainsKey(curPt))//支路的报警阀数目
                {
                    pt = pt.OffsetX(5150 * branchLoopNums + sprayIn.PipeGap * (alarmNums - 1) + 2500);
                }
                if (spraySystem.SubLoopFireAreasDic.ContainsKey(curPt))//支路的防火分区数目
                {
                    foreach (var num in spraySystem.SubLoopFireAreasDic[curPt])
                    {
                        fireNums += Math.Max(num,1);
                    }
                    pt = pt.OffsetX(fireNums * 5500 - 2500 * branchLoopNums + 1500);
                }
                LastBranchLoopAlarmLength = 5500 * fireNums;
                sprayOut.PipeLine.Add(new Line(stPt, pt));

                curFloorMaxSite = pt.X;//当前层延伸到的最远点
            }
            curFloorMaxSite = pt.X;//当前层延伸到的最远点
            flag *= -1;
            return pt;
        }
        private static Point3d GetBranchLoopPt2(Point3dEx curPt, Point3d stPt, SprayOut sprayOut, ref int flag,
            SpraySystem spraySystem, SprayIn sprayIn, int LastBranchLoopIndex,
            ref double LastBranchLoopAlarmLength, ref double curFloorMaxSite, bool lastPtIsBranchLoop, double upperFloorMaxSite, double curMaxSite)
        {
            double height = 1200;
            double heigth15 = height*1.5;
            double pipeLen = 6000;
            var alarmNums = 0;//当前支环（报警阀间）上的报警阀数目
            var branchLoopNums = 0;
            var fireNums = 0;//当前支环（报警阀间）上的防火分区数目
            var pt = new Point3d();
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
                if (LastBranchLoopIndex == 1 && lastPtIsBranchLoop)//当前层的上一层
                {
                    var branchLoopCross = HasVerticalCrossUpperFloor(curPt, spraySystem, sprayIn);
                    if (branchLoopCross)
                    {
                        var dist1 = curFloorMaxSite;//  当前层移动到的位置的X值
                        var BranchLoopLength = GetBranchLoopLength(curPt, spraySystem, sprayIn, alarmNums, branchLoopNums);
                        var dist2 = stPt.X - BranchLoopLength;//  当前层左移到最左的X值
                        var dist3 = upperFloorMaxSite;//上一层立管结束的位置
                        var dist4 = curMaxSite;
                        var curSiteX = Math.Max(Math.Max(dist1, dist2), Math.Max(dist3, dist4));//取三者最大（右）值
                        stPt = new Point3d(curSiteX, stPt.Y, 0);
                    }
                    else
                    {
                        var dist1 = curFloorMaxSite;//  当前层移动到的位置的X值
                        var BranchLoopLength = GetBranchLoopLength(curPt, spraySystem, sprayIn, alarmNums, branchLoopNums);
                        var dist2 = stPt.X - BranchLoopLength;//  当前层左移到最左的X值
                        var dist3 = upperFloorMaxSite;//上一层立管结束的位置
                        var curSiteX = Math.Max(Math.Max(dist1, dist2), dist3);//取三者最大（右）值
                        stPt = new Point3d(curSiteX, stPt.Y, 0);
                    }

                }
                var pt1 = stPt.OffsetY(-heigth15);
                var pt2 = pt1.OffsetX(pipeLen + 1200);
                sprayOut.PipeLine.Add(new Line(stPt, pt1));
                sprayOut.PipeLine.Add(new Line(pt1, pt2));
                pt = stPt.OffsetX(1200);
                sprayOut.PipeLine.Add(new Line(stPt, pt));
                spraySystem.TempSubLoopStartPt = new Point3d(stPt.X, stPt.Y, 0);
                spraySystem.BranchLoopPtDic.Add(curPt, pt2);//保存支环的起始点
                spraySystem.BranchLoopPtNewDic.Add(curPt, pt1);//保存支环的新画法起始点
            }
            else
            {
                if (spraySystem.SubLoopBranchPtDic.ContainsKey(curPt))
                {
                    var branchNums = spraySystem.SubLoopBranchDic[curPt];//支路数
                    sprayOut.PipeLine.Add(new Line(stPt, stPt.OffsetX(branchNums * sprayIn.PipeGap)));
                    stPt = stPt.OffsetX(branchNums * sprayIn.PipeGap);
                }

                
                pt = spraySystem.TempSubLoopStartPt;
                if (spraySystem.SubLoopAlarmsDic.ContainsKey(curPt))//支路的报警阀数目
                {
                    pt = pt.OffsetX(5150 * branchLoopNums + sprayIn.PipeGap * (alarmNums - 1) + 2500);
                }
                if (spraySystem.SubLoopFireAreasDic.ContainsKey(curPt))//支路的防火分区数目
                {
                    foreach (var num in spraySystem.SubLoopFireAreasDic[curPt])
                    {
                        fireNums += num;
                    }
                    pt = pt.OffsetX(fireNums * 5500 - 2500 * branchLoopNums + 1500);
                }
                LastBranchLoopAlarmLength = 5500 * fireNums;

                var pt0 = stPt.OffsetY(-height * 0.5);
                var pt1 = stPt.OffsetY(-height);
                var pt2 = pt1.OffsetX(pipeLen);

                sprayOut.PipeLine.Add(new Line(stPt, pt));

                sprayOut.PipeLine.Add(new Line(pt0, pt1));
                sprayOut.PipeLine.Add(new Line(pt1, pt2));
                spraySystem.BranchLoopPtDic.Add(curPt, pt2);//保存支环的起始点
                spraySystem.BranchLoopPtNewDic.Add(curPt, pt1);//保存支环的新画法起始点
                curFloorMaxSite = pt.X;//当前层延伸到的最远点
            }
            curFloorMaxSite = pt.X;//当前层延伸到的最远点
            flag *= -1;
            return pt;
        }

        private static bool HasVerticalCrossUpperFloor(Point3dEx curPt, SpraySystem spraySystem, SprayIn sprayIn)
        {
            foreach (var rstPath in spraySystem.BranchLoops)//遍历所有支环
            {
                var firstPt = rstPath.First();
                if (curPt.Equals(firstPt))//找到当前点的支环
                {
                    foreach(var pt in rstPath)//遍历当前支环
                    {
                        if(spraySystem.BranchDic.ContainsKey(pt))//判断包含支路的点
                        {
                            var tpts = spraySystem.BranchDic[pt];//获取支路的终点
                            foreach (var tpt in tpts)
                            {
                                if (!sprayIn.TermPtDic.ContainsKey(tpt))
                                {
                                    continue;
                                }
                                var type = sprayIn.TermPtDic[tpt].Type;
                                if(type == 3 || type == 4)//存在穿楼层的点
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    return false;//当前支环遍历完返回false
                }
            }
            return false;
        }

        private static int GetWaterPumpNum(Point3dEx curPt, SpraySystem spraySystem, SprayIn sprayIn)
        {
            var waterPumpNum = 0;//水泵接合器数目
            if (spraySystem.SubLoopBranchPtDic.ContainsKey(curPt))
            {
                foreach (var bpt in spraySystem.SubLoopBranchPtDic[curPt])
                {
                    if (spraySystem.BranchDic.ContainsKey(bpt))
                    {
                        if (spraySystem.BranchDic[bpt].Count == 1)//单支路
                        {
                            var tpt = spraySystem.BranchDic[bpt][0];
                            if (sprayIn.TermPtTypeDic.ContainsKey(tpt))
                            {
                                if (sprayIn.TermPtTypeDic[tpt] == 3)//支路末端是水泵接合器
                                {
                                    waterPumpNum++;
                                }
                            }
                        }
                    }
                }
            }

            return waterPumpNum;
        }
        
        private static double GetBranchLoopLength(Point3dEx curPt, SpraySystem spraySystem, SprayIn sprayIn, 
            int alarmNums, int branchLoopNums)
        {
            double length = 0;
            var fireNums = 0;//当前支环（报警阀间）上的防火分区数目
            if (spraySystem.SubLoopAlarmsDic.ContainsKey(curPt))//支路的报警阀数目
            {
                length+= 5150 * branchLoopNums + sprayIn.PipeGap * (alarmNums - 1) + 2500;
            }
            if (spraySystem.SubLoopFireAreasDic.ContainsKey(curPt))//支路的防火分区数目
            {
                foreach (var num in spraySystem.SubLoopFireAreasDic[curPt])
                {
                    fireNums += num;
                }
                length += fireNums * 5500 - 2500 * branchLoopNums + 1500;
            }

            return length;
        }
        

        private static Point3d GetBranchPt(Point3dEx curPt, Point3d stPt, SprayOut sprayOut,
            SpraySystem spraySystem, string nextPtType, double height, SprayIn sprayIn, bool lastPtIsBranchLoop, double LastBranchLoopAlarmLength)
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
            double dist = sprayIn.PipeGap;

            var pt = stPt.OffsetX(dist);
            sprayOut.PipeLine.Add(new Line(stPt, pt));
            sprayOut.PipeLine.Add(new Line(stPt, stPt.OffsetY(height)));
            if (spraySystem.BranchLoopPtDic.ContainsKey(curPt))
            {
                spraySystem.BranchLoopPtDic.Remove(curPt);
            }
            spraySystem.BranchLoopPtDic.Add(curPt, stPt.OffsetY(height));
            if (spraySystem.BranchPtDic.ContainsKey(curPt))
            {
                spraySystem.BranchPtDic.Remove(curPt);
            }
            spraySystem.BranchPtDic.Add(curPt, stPt.OffsetY(height));
            return pt;
        }
        private static void GetSignalValvePt(Point3d stPt, SprayOut sprayOut, SprayIn sprayIn)
        {
            double valveGapX = sprayIn.PipeGap / 2;
            double ValveGap = 300;
            var insertPt = stPt.OffsetX(-valveGapX);
            sprayOut.SprayBlocks.Add(new SprayBlock("遥控信号阀", insertPt));
            foreach (var line in sprayOut.PipeLine)
            {
                if (line.GetClosestPointTo(insertPt, false).DistanceTo(insertPt) < 10)
                {
                    sprayOut.PipeLine.Remove(line);
                    if (line.StartPoint.X < line.EndPoint.X)
                    {
                        sprayOut.PipeLine.Add(new Line(line.StartPoint, insertPt));
                        sprayOut.PipeLine.Add(new Line(line.EndPoint, insertPt.OffsetX(ValveGap)));
                    }
                    else
                    {
                        sprayOut.PipeLine.Add(new Line(line.EndPoint, insertPt));
                        sprayOut.PipeLine.Add(new Line(line.StartPoint, insertPt.OffsetX(ValveGap)));
                    }
                    break;
                }
            }
        }
        private static void GetDetail(Point3d stPt1, Point3d stPt, SprayOut sprayOut, SprayIn sprayIn, double height)
        {
            sprayOut.PipeLine.Add(new Line(stPt, stPt.OffsetY(-height)));
            sprayOut.PipeLine.Add(new Line(stPt1.OffsetY(-height), stPt.OffsetY(-height)));

            //绘制楼板线
            var floors = sprayIn.FloorRectDic;
            double fHeight = sprayIn.FloorHeight;
            Point3d insertPt = sprayOut.InsertPoint.Cloned();
            foreach (var fNumber in floors.Keys)
            {
                sprayOut.FloorLine.Add(new Line(insertPt, new Point3d(stPt.X + 3000, insertPt.Y, 0)));
                insertPt = insertPt.OffsetY(fHeight);
            }
            sprayOut.FloorLine.Add(new Line(insertPt, new Point3d(stPt.X + 3000, insertPt.Y, 0)));
        }
    }
}
