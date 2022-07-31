using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundSpraySystem.Method
{
    public class BranchDeal
    {
        public static void AlarmValveGet(ref HashSet<Point3dEx> visited, SprayIn sprayIn, SpraySystem spraySystem, SprayOut sprayOut)
        {
            var alarmNums = 0;//支路数
            var drawPt = sprayOut.InsertPoint.OffsetY(6200);
            foreach (var pt in sprayIn.AlarmValveStPts)
            {
                var fireAreaNums = 0;//防火分区数

                var stpt = pt;
                alarmNums += 1;
                var termPts = new List<Point3dEx>();
                var valvePts = new List<Point3dEx>();
                var flowPts = new List<Point3dEx>();
                Queue<Point3dEx> q = new Queue<Point3dEx>();
                q.Enqueue(pt);
                HashSet<Point3dEx> visited2 = new HashSet<Point3dEx>();
                visited.Add(pt);
                int level = 0;
                var ptLevelDic = new Dictionary<Point3dEx, int>();//每个点及其level

                while (q.Count > 0)
                {
                    var curPt = q.Dequeue();
                    if (!ptLevelDic.ContainsKey(curPt))
                    {
                        ptLevelDic.Add(curPt, 0);
                    }
                    if (sprayIn.PtTypeDic.ContainsKey(curPt))
                    {
                        if (sprayIn.PtTypeDic[curPt].Contains("Flow"))
                        {
                            flowPts.Add(pt);
                        }
                    }
                    //if (sprayIn.ThroughPt.Contains(curPt))//当前点是楼层穿越点
                    //{
                    //    termPts.Add(curPt);
                    //    sprayIn.CurThroughPt.AddItem(curPt);
                    //    continue;
                    //}
                    if(!sprayIn.PtDic.ContainsKey(curPt))
                    {
                        break;
                    }
                    var adjs = sprayIn.PtDic[curPt];
                    if (adjs.Count == 1 && !curPt.Equals(pt))
                    {
                        termPts.Add(curPt);
                        continue;
                    }

                    foreach (var adj in adjs)
                    {
                        if (visited2.Contains(adj))
                        {
                            continue;
                        }

                        visited2.Add(adj);
                        q.Enqueue(adj);
                        if(ptLevelDic.ContainsKey(adj))
                        {
                            ;
                        }
                        else
                        {
                            if (adjs.Count == 3)
                            {
                                ptLevelDic.Add(adj, ptLevelDic[curPt] + 1);
                            }
                            else
                            {
                                ptLevelDic.Add(adj, ptLevelDic[curPt]);
                            }
                        }

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
                    spraySystem.BranchPtDic.Add(pt, drawPt);
                    drawPt = drawPt.OffsetX((fireAreaNums + 2.5) * 5500 + 5000);
                }
            }
        }
        
        
        public static void Get(ref HashSet<Point3dEx> visited, SprayIn sprayIn, SpraySystem spraySystem)
        {
            MainLoopGet(ref visited, sprayIn, spraySystem);
            SubLoopGet(ref visited, sprayIn, spraySystem);
            foreach (var branchLoop in spraySystem.BranchLoops)
            {
                BfsBranch(out int alarmNums, out int fireAreaNums, branchLoop, visited, sprayIn, spraySystem);
                BfsBranchAcross(branchLoop, sprayIn, spraySystem);

                foreach (var subLoop in spraySystem.SubLoops)
                {
                    SubLoopAdd(spraySystem, subLoop, branchLoop, alarmNums, fireAreaNums);
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

        
        public static void BfsBranch(out int alarmNums, out int fireAreaNums, List<Point3dEx> branchLoop, 
            HashSet<Point3dEx> visited, SprayIn sprayIn, SpraySystem spraySystem)
        {
            alarmNums = 0;
            fireAreaNums = 0;
            for (int i = 1; i < branchLoop.Count - 1; i++)
            {

                var pt = branchLoop[i];

                if (sprayIn.PtTypeDic[branchLoop[i]].Contains("AlarmValve"))
                {
                    bool hasFireArea = false;
                    alarmNums += 1;
                    var termPts = new List<Point3dEx>();
                    var valvePts = new List<Point3dEx>();
                    var flowPts = new List<Point3dEx>();
                    Queue<Point3dEx> q = new Queue<Point3dEx>();
                    q.Enqueue(pt);
                    HashSet<Point3dEx> visited2 = new HashSet<Point3dEx>();
                    visited.Add(pt);
                    int level = 0;
                    var ptLevelDic = new Dictionary<Point3dEx, int>();//每个点及其level
                    while (q.Count > 0)
                    {
                        var curPt = q.Dequeue();
                        if (!ptLevelDic.ContainsKey(curPt))
                        {
                            ptLevelDic.Add(curPt, 0);
                        }
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
                            if (adjs.Count == 3)
                            {
                                ptLevelDic.Add(adj, ptLevelDic[curPt] + 1);
                            }
                            else
                            {
                                ptLevelDic.Add(adj, ptLevelDic[curPt]);
                            }
                        }
                    }
                    if (termPts.Count != 0)
                    {
                        fireAreaNums += termPts.Count;
                        //foreach (var tpt in termPts)
                        //{
                        //    if (sprayIn.TermPtTypeDic.ContainsKey(tpt))
                        //    {
                        //        if (sprayIn.TermPtTypeDic[tpt] == 1) //防火分区
                        //        {
                        //            hasFireArea = true;
                        //            fireAreaNums += 1;
                        //        }
                        //    }
                        //}
                        //if (!hasFireArea)
                        //{
                        //    fireAreaNums += 1;
                        //}
                        if (spraySystem.BranchDic.ContainsKey(pt))
                        {
                            continue;
                        }
                        termPts = termPts.OrderBy(p => ptLevelDic[p]).ToList();
                        spraySystem.BranchDic.Add(pt, termPts);

                    }
                }
            }
        }


        public static void BfsBranchAcross(List<Point3dEx> branchLoop, SprayIn sprayIn, SpraySystem spraySystem)
        {
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
                        bool flag = false;
                        string flowType = "";
                        BranchDeal2.DfsBranch(spt, spt, ept, branchLoop, tempPath, visited2, sprayIn, ref hasValve, ref hasFlow, stopwatch, flag, ref flowType);
                        if (hasValve)
                        {
                            spraySystem.ValveDic.Add(ept);
                        }
                        if (hasFlow)
                        {
                            if(flowType.Equals(""))
                            {
                                spraySystem.FlowDIc.Add(ept,flowType);
                            }
                        }
                    }
                }
            }
        }

        
        public static void SubLoopAdd(SpraySystem spraySystem, List<Point3dEx> subLoop, List<Point3dEx> branchLoop, int alarmNums, int fireAreaNums)
        {
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
        }


        public static void GetThrough(ref HashSet<Point3dEx> visited, SprayIn sprayIn, SpraySystem spraySystem)
        {
            for (int i = 0; i < sprayIn.CurThroughPt.Count; i++)
            {
                var pt = sprayIn.CurThroughPt[i];//穿越点
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
                        termPts.Add(curPt);
                        continue;
                    }

                    foreach (var adj in adjs)
                    {
                        if (sprayIn.CurThroughPt.Contains(curPt))
                        {
                            if (!sprayIn.ThroughPt.Contains(adj))//当前点不是楼层穿越点
                            {
                                continue;//跳过
                            }
                        }

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
                    if (!spraySystem.BranchThroughDic.ContainsKey(pt))
                        spraySystem.BranchThroughDic.Add(pt, termPts);
                }
            }
        }

        
        private static void MainLoopGet(ref HashSet<Point3dEx> visited, SprayIn sprayIn, SpraySystem spraySystem)
        {
            var mainLoop = spraySystem.MainLoop;
            {
                for (int i = 1; i < mainLoop.Count - 1; i++)
                {
                    var pt = mainLoop[i];
                    if (sprayIn.PtTypeDic[mainLoop[i]].Equals("Branch"))
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

                            var adjs = sprayIn.PtDic[curPt];
                            if (adjs.Count == 1)
                            {
                                termPts.Add(curPt);
                                continue;
                            }

                            foreach (var adj in adjs)
                            {
                                if (mainLoop.Contains(adj))
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
                            if (spraySystem.BranchDic.ContainsKey(pt))
                            {
                                continue;
                            }
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
            }
        }

        
        private static void SubLoopGet(ref HashSet<Point3dEx> visited, SprayIn sprayIn, SpraySystem spraySystem)
        {
            foreach (var subLoop in spraySystem.SubLoops)
            {
                for (int i = 1; i < subLoop.Count - 1; i++)
                {
                    var pt = subLoop[i];
                    if (sprayIn.PtTypeDic[subLoop[i]].Equals("Branch"))
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

                            var adjs = sprayIn.PtDic[curPt];
                            if (adjs.Count == 1)
                            {
                                termPts.Add(curPt);
                                continue;
                            }

                            foreach (var adj in adjs)
                            {
                                if (subLoop.Contains(adj))
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

                            if (spraySystem.BranchThroughDic.ContainsKey(pt))
                            {
                                continue;
                            }
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
            }
        }
    }
}
