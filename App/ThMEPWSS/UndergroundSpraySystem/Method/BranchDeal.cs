using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundSpraySystem.Method
{
    public class BranchDeal
    {
        public static void Get(ref HashSet<Point3dEx> visited, SprayIn sprayIn, SpraySystem spraySystem)
        {
            MainLoopGet(ref visited, sprayIn, spraySystem);
            SubLoopGet(ref visited, sprayIn, spraySystem);
            foreach (var branchLoop in spraySystem.BranchLoops)
            {
                var alarmNums = 0;//支路数
                var fireAreaNums = 0;//防火分区数

                //判断报警阀的输出顺序
                bool indexOrder = true;
                try
                {
                    var alarmText = "";
                    for (int i = 1; i < branchLoop.Count - 1; i++)
                    {
                        var pt = branchLoop[i];
                        var type = sprayIn.PtTypeDic[pt];
                        if (type.Contains("AlarmValve"))
                        {
                            if (sprayIn.AlarmTextDic.ContainsKey(pt))
                            {
                                if (alarmText.Equals(""))
                                {
                                    alarmText = System.Text.RegularExpressions.Regex.Replace(sprayIn.AlarmTextDic[pt], @"[^0-9]+", "");
                                    continue;
                                }
                                var alarmText2 = System.Text.RegularExpressions.Regex.Replace(sprayIn.AlarmTextDic[pt], @"[^0-9]+", "");
                                if (Convert.ToInt32(alarmText) > Convert.ToInt32(alarmText2))
                                {
                                    indexOrder = false;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ;
                }
                if(!indexOrder)
                {
                    branchLoop.Reverse();
                }


                for (int i = 1; i < branchLoop.Count - 1; i++)
                {
                    try
                    {
                        var pt = branchLoop[i];

                        if (sprayIn.PtTypeDic[branchLoop[i]].Contains("Alarm"))
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
                                    if (sprayIn.PtTypeDic[curPt].Contains("Valve"))
                                    {
                                        valvePts.Add(pt);
                                    }
                                    if(sprayIn.PtTypeDic[curPt].Contains("Flow"))
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
                                foreach(var tpt in termPts)
                                {
                                    if(sprayIn.TermPtTypeDic.ContainsKey(tpt))
                                    {
                                        if(sprayIn.TermPtTypeDic[tpt] == 1) //防火分区
                                        {
                                            fireAreaNums += 1;
                                        }
                                    }
                                    //if (valvePts.Count != 0)
                                    //{
                                    //    spraySystem.ValveDic.Add(tpt, true);
                                    //}
                                }
                                if (spraySystem.BranchDic.ContainsKey(pt))
                                {
                                    continue;
                                }
                                spraySystem.BranchDic.Add(pt, termPts);
                                
                            }
                        }
                        else if(sprayIn.PtDic[branchLoop[i]].Count == 3)
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
                    catch
                    {
                    }
                }
                
                foreach(var subLoop in spraySystem.SubLoops)
                {
                    try
                    {
                        if (subLoop.Contains(branchLoop.Last()))
                        {
                            if(spraySystem.SubLoopAlarmsDic.ContainsKey(subLoop.First()))
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
                    catch
                    {
                        
                    }
                    
                }
                foreach(var branchLoopPt in branchLoop)
                {
                    try
                    {
                        if (!spraySystem.SubLoopAlarmsDic.ContainsKey(branchLoopPt))
                        {
                            spraySystem.SubLoopAlarmsDic.Add(branchLoopPt, new List<int>() { alarmNums });
                            spraySystem.SubLoopFireAreasDic.Add(branchLoopPt, new List<int>() { fireAreaNums });
                        }
                    }
                    catch
                    {
                        
                    }
                }
            }
        }


        public static void GetThrough(ref HashSet<Point3dEx> visited, SprayIn sprayIn, SpraySystem spraySystem)
        {
            for(int i = 0; i < sprayIn.CurThroughPt.Count; i++)
            {
                try 
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
                            if(sprayIn.CurThroughPt.Contains(curPt))
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
                    if(termPts.Count != 0)
                    {
                        spraySystem.BranchThroughDic.Add(pt, termPts);
                    }
                }
                catch
                {

                }
            }
        }

        private static void MainLoopGet(ref HashSet<Point3dEx> visited, SprayIn sprayIn, SpraySystem spraySystem)
        {
            var mainLoop = spraySystem.MainLoop;
            {
                for (int i = 1; i < mainLoop.Count - 1; i++)
                {
                    try
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
                                        spraySystem.ValveDic.Add(tpt, true);
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        
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
                    try
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
                                    foreach(var tpt in termPts)
                                    {
                                        spraySystem.ValveDic.Add(tpt, true);

                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        
                    }
                }
            }
        }
    }
}
