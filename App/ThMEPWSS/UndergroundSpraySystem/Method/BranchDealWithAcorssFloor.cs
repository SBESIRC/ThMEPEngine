using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundSpraySystem.Method
{
    public class BranchDealWithAcorssFloor
    {
        public static void Get(ref HashSet<Point3dEx> visited, SprayIn sprayIn, SpraySystem spraySystem)
        {
            MainLoopGet(ref visited, sprayIn, spraySystem);
            //CrossMainLoopGet(ref visited, sprayIn, spraySystem);
            if(spraySystem.BranchLoops.Count > 0)
            {
                BranchLoopGet(1, ref visited, sprayIn, spraySystem);
            }
            if (spraySystem.BranchLoops2.Count > 0)
            {
                BranchLoopGet(2, ref visited, sprayIn, spraySystem);
            }
            if (spraySystem.BranchLoops3.Count > 0)
            {
                BranchLoopGet(3, ref visited, sprayIn, spraySystem);
            }
        }

        private static void MainLoopGet(ref HashSet<Point3dEx> visited, SprayIn sprayIn, SpraySystem spraySystem)
        {
            var mainLoop = spraySystem.MainLoop;
            var branchLoopStartEndPts = new List<Point3dEx>();
            foreach(var branchLoop in spraySystem.BranchLoops)
            {
                var spt = branchLoop.First();
                var ept = branchLoop.Last();
                branchLoopStartEndPts.Add(spt);
                branchLoopStartEndPts.Add(ept);
            }
            
            for (int i = 1; i < mainLoop.Count - 1; i++)
            {
                var pt = mainLoop[i];
                if(branchLoopStartEndPts.Contains(pt))
                {
                    continue;
                }
                if (sprayIn.PtTypeDic[mainLoop[i]].Equals("Branch"))
                {
                    MainLoopBranchBfs(pt, ref visited, sprayIn, spraySystem);
                }
            }
        }

        private static void BranchLoopGet(int branchLoopIndex, ref HashSet<Point3dEx> visited, SprayIn sprayIn, SpraySystem spraySystem)
        {
            var branchLoopsI = new List<List<Point3dEx>>();
            if(branchLoopIndex == 1)
            {
                spraySystem.BranchLoops.ForEach(bloop => branchLoopsI.Add(bloop));
            }
            if (branchLoopIndex == 2)
            {
                spraySystem.BranchLoops2.ForEach(bloop => branchLoopsI.Add(bloop));
            }
            if (branchLoopIndex == 3)
            {
                spraySystem.BranchLoops3.ForEach(bloop => branchLoopsI.Add(bloop));
            }
            foreach (var branchLoop in branchLoopsI)
            {
                var alarmNums = 0;//支路数
                var fireAreaNums = 0;//防火分区数

                for (int i = 1; i < branchLoop.Count - 1; i++)
                {
                    var pt = branchLoop[i];
                    if (sprayIn.PtTypeDic[branchLoop[i]].Contains("AlarmValve"))
                    {
                        alarmNums += 1;
                        var termPts = new List<Point3dEx>();
                        var valvePts = new List<Point3dEx>();
                        var flowPts = new List<Point3dEx>();
                        Queue<Point3dEx> q = new Queue<Point3dEx>();
                        q.Enqueue(pt);
                        HashSet<Point3dEx> visited2 = new HashSet<Point3dEx>();
                        visited.Add(pt);
                        while (q.Count > 0)
                        {
                            var curPt = q.Dequeue();
                            if (sprayIn.PtTypeDic.ContainsKey(curPt))
                            {
                                if (sprayIn.PtTypeDic[curPt].Contains("Flow"))
                                {
                                    flowPts.Add(pt);
                                }
                            }
                            if (sprayIn.ThroughPt.Contains(curPt))//当前点是楼层穿越点
                            {
                                termPts.Add(curPt);
                                sprayIn.CurThroughPt.AddItem(curPt);
                                continue;
                            }
                            var adjs = sprayIn.PtDic[curPt];
                            if (adjs.Count == 1)
                            {
                                termPts.Add(curPt);
                                continue;
                            }

                            foreach (var adj in adjs)
                            {
                                if (branchLoop.Contains(adj))
                                    continue;

                                if (visited2.Contains(adj))
                                {
                                    continue;
                                }

                                visited2.Add(adj);
                                q.Enqueue(adj);
                            }
                        }
                        if (termPts.Count != 0)
                        {
                            foreach (var tpt in termPts)
                            {
                                if (sprayIn.TermPtTypeDic.ContainsKey(tpt))
                                {
                                    if (sprayIn.TermPtTypeDic[tpt] == 1) //防火分区
                                    {
                                        fireAreaNums += 1;
                                    }
                                }
                            }
                            if (spraySystem.BranchDic.ContainsKey(pt))
                            {
                                continue;
                            }
                            spraySystem.BranchDic.Add(pt, termPts);

                        }
                    }
                    else if(sprayIn.PtTypeDic[branchLoop[i]].Contains("BranchLoop"))
                    {
                        continue;
                    }
                    else if (sprayIn.PtDic[branchLoop[i]].Count == 3)
                    {
                        var termPts = new List<Point3dEx>();
                        var valvePts = new List<Point3dEx>();
                        var flowPts = new List<Point3dEx>();
                        Queue<Point3dEx> q = new Queue<Point3dEx>();
                        q.Enqueue(pt);
                        HashSet<Point3dEx> visited2 = new HashSet<Point3dEx>();
                        visited.Add(pt);
                        while (q.Count > 0)
                        {
                            var curPt = q.Dequeue();
                            if (sprayIn.PtTypeDic.ContainsKey(curPt))
                            {
                                if (sprayIn.PtTypeDic[curPt].Contains("Valve"))
                                {
                                    valvePts.Add(pt);
                                }
                                if (sprayIn.PtTypeDic[curPt].Contains("Flow"))
                                {
                                    flowPts.Add(pt);
                                }
                            }
                            if (sprayIn.ThroughPt.Contains(curPt))//当前点是楼层穿越点
                            {
                                termPts.Add(curPt);
                                sprayIn.CurThroughPt.AddItem(curPt);
                                continue;
                            }
                            var adjs = sprayIn.PtDic[curPt];
                            if (adjs.Count == 1)
                            {
                                termPts.Add(curPt);
                                continue;
                            }

                            foreach (var adj in adjs)
                            {
                                if (branchLoop.Contains(adj))
                                    continue;

                                if (visited2.Contains(adj))
                                {
                                    continue;
                                }

                                visited2.Add(adj);
                                q.Enqueue(adj);
                            }
                        }
                        if (spraySystem.BranchDic.ContainsKey(pt))
                        {
                            continue;
                        }
                        spraySystem.BranchDic.Add(pt, termPts);
                    }
                }

                foreach (var spt in branchLoop)//每个支路起点
                {
                    if (sprayIn.PtDic[spt].Count == 3)
                    {
                        if (!spraySystem.BranchDic.ContainsKey(spt))
                        {
                            continue;
                        }
                        foreach (var ept in spraySystem.BranchDic[spt])//每个支路终点
                        {
                            var tempPath = new List<Point3dEx>();
                            var visited2 = new HashSet<Point3dEx>();
                            bool hasValve = false;
                            bool hasFlow = false;
                            var stopwatch = new Stopwatch();
                            stopwatch.Start();
                            bool flag = true;
                            BranchDeal2.DfsBranch(spt, spt, ept, branchLoop, tempPath, visited2, sprayIn, ref hasValve, ref hasFlow, stopwatch, flag);
                            if (hasValve)
                            {
                                spraySystem.ValveDic.Add(ept);
                            }
                            if (hasFlow)
                            {
                                spraySystem.FlowDIc.Add(ept);
                            }
                        }
                    }
                }


                var subLoop = new List<Point3dEx>();
                if(branchLoopIndex == 1)
                {
                    subLoop = spraySystem.MainLoop;
                }
                if (branchLoopIndex == 2)
                {
                    subLoop = spraySystem.BranchLoops.First();
                }
                if (branchLoopIndex == 3)
                {
                    subLoop = spraySystem.BranchLoops2.First();
                }
                
                if (subLoop.Contains(branchLoop.Last()))
                {
                    if (spraySystem.SubLoopAlarmsDic.ContainsKey(subLoop.First()))
                    {
                        spraySystem.SubLoopAlarmsDic[subLoop.First()].Add(alarmNums);
                        spraySystem.SubLoopFireAreasDic[subLoop.First()].Add(fireAreaNums);
                    }
                    else
                    {
                        spraySystem.SubLoopAlarmsDic.Add(subLoop.First(), new List<int>() { alarmNums });
                        spraySystem.SubLoopFireAreasDic.Add(subLoop.First(), new List<int>() { fireAreaNums });
                    }

                    if (spraySystem.SubLoopAlarmsDic.ContainsKey(subLoop.Last()))
                    {
                        spraySystem.SubLoopAlarmsDic[subLoop.Last()].Add(alarmNums);
                        spraySystem.SubLoopFireAreasDic[subLoop.Last()].Add(fireAreaNums);
                    }
                    else
                    {
                        spraySystem.SubLoopAlarmsDic.Add(subLoop.Last(), new List<int>() { alarmNums });
                        spraySystem.SubLoopFireAreasDic.Add(subLoop.Last(), new List<int>() { fireAreaNums });
                    }
                }
                

                foreach (var branchLoopPt in branchLoop)
                {
                    if (!spraySystem.SubLoopAlarmsDic.ContainsKey(branchLoopPt))
                    {
                        spraySystem.SubLoopAlarmsDic.Add(branchLoopPt, new List<int>() { alarmNums });
                        spraySystem.SubLoopFireAreasDic.Add(branchLoopPt, new List<int>() { fireAreaNums });
                    }
                }
            }
        }

        private static void MainLoopBranchBfs(Point3dEx pt, ref HashSet<Point3dEx> visited, SprayIn sprayIn, SpraySystem spraySystem)
        {
            var mainLoop = spraySystem.MainLoop;

            var termPts = new List<Point3dEx>();
            var valvePts = new List<Point3dEx>();
            var flowPts = new List<Point3dEx>();
            Queue<Point3dEx> q = new Queue<Point3dEx>();
            q.Enqueue(pt);
            HashSet<Point3dEx> visited2 = new HashSet<Point3dEx>();
            visited.Add(pt);
            while (q.Count > 0)
            {
                var curPt = q.Dequeue();
                if (sprayIn.PtTypeDic.ContainsKey(curPt))
                {
                    if (sprayIn.PtTypeDic[curPt].Contains("Valve"))
                    {
                        valvePts.Add(pt);
                    }
                    if (sprayIn.PtTypeDic[curPt].Contains("Flow"))
                    {
                        flowPts.Add(pt);
                    }
                }

                var adjs = sprayIn.PtDic[curPt];
                if (adjs.Count == 1)
                {
                    var vpt = GetVerticalPt(curPt, sprayIn);
                    if(vpt is null)
                    {
                        ;
                    }
                    else
                    {
                        ;
                        if(sprayIn.TermPtDic.ContainsKey(curPt))
                        {
                            ;
                            sprayIn.TermPtDic[curPt] = sprayIn.TermPtDic[vpt];
                        }
                        else
                        {
                            sprayIn.TermPtDic.Add(curPt, sprayIn.TermPtDic[vpt]);
                        }
                    }
                    termPts.Add(curPt);
                    continue;
                }

                foreach (var adj in adjs)
                {
                    if (mainLoop.Contains(adj)) continue;
                    if (visited2.Contains(adj)) continue;

                    visited2.Add(adj);
                    q.Enqueue(adj);
                }
            }

            if (termPts.Count > 0)
            {
                if (!spraySystem.BranchDic.ContainsKey(pt))
                {
                    spraySystem.BranchDic.Add(pt, termPts);
                    if (valvePts.Count != 0)
                    {
                        foreach (var tpt in termPts)
                        {
                            spraySystem.ValveDic.Add(tpt);
                        }
                    }
                }
            }
        }

        private static Point3dEx GetVerticalPt(Point3dEx curPt, SprayIn sprayIn)
        {
            double tor = 100;
            foreach(var vpt in sprayIn.TermPtDic.Keys)
            {
                if(vpt._pt.DistanceTo(curPt._pt) < tor)
                {
                    return vpt;
                }
            }
            return  null;
        }
    }
}
