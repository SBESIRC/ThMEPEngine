using System.Collections.Generic;
using ThMEPWSS.UndergroundSpraySystem.Model;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThCADCore.NTS;
using System.Diagnostics;

namespace ThMEPWSS.UndergroundSpraySystem.Method
{
    public class BranchDeal2
    {
        public static void Get(ref HashSet<Point3dEx> visited, SprayIn sprayIn, SpraySystem spraySystem)
        {
            for (int j = 0; j < spraySystem.MainLoops.Count; j++)
            {
                var branchLoop = new List<Point3dEx>();
                branchLoop.AddRange(spraySystem.MainLoops[j]);
                var alarmNums = 0;//支路数
                var fireAreaNums = 0;//防火分区数
                for (int i = branchLoop.Count - 1; i >= 0; i--)
                {
                    var pt = branchLoop[i];

                    if (!sprayIn.PtTypeDic[pt].Contains("AlarmValve"))
                    {
                        branchLoop.Remove(pt);
                    }
                    else
                    {
                        if (!sprayIn.AlarmTextDic.ContainsKey(pt))
                        {
                            foreach (var apt in sprayIn.AlarmTextDic.Keys)
                            {
                                if (apt._pt.DistanceTo(pt._pt) < 150)
                                {
                                    sprayIn.AlarmTextDic.Add(pt, sprayIn.AlarmTextDic[apt]);
                                    break;
                                }
                            }
                        }
                    }
                }

                for (int i = 0; i < branchLoop.Count; i++)
                {
                    var pt = branchLoop[i];
                    if (sprayIn.PtTypeDic[pt].Contains("Alarm"))
                    {
                        alarmNums += 1;
                        var termPts = new List<Point3dEx>();
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
                                if (spraySystem.MainLoops[j].Contains(adj))
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
                }
                foreach (var spt in branchLoop)//每个支路起点
                {
                    foreach (var ept in spraySystem.BranchDic[spt])//每个支路终点
                    {
                        var tempPath = new List<Point3dEx>();
                        var visited2 = new HashSet<Point3dEx>();
                        bool hasValve = false;
                        bool hasFlow = false;
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();
                        bool flag = true;
                        DfsBranch(spt, spt, ept, spraySystem.MainLoops[j], tempPath, visited2, sprayIn, ref hasValve, ref hasFlow, stopwatch, flag);
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
                foreach (var branchLoopPt in branchLoop)
                {
                    if (!spraySystem.SubLoopAlarmsDic.ContainsKey(branchLoopPt))
                    {
                        spraySystem.SubLoopAlarmsDic.Add(branchLoopPt, new List<int>() { alarmNums });
                        spraySystem.SubLoopFireAreasDic.Add(branchLoopPt, new List<int>() { fireAreaNums });
                    }
                    else
                    {
                        spraySystem.SubLoopAlarmsDic[branchLoopPt].Add(alarmNums);
                        spraySystem.SubLoopFireAreasDic[branchLoopPt].Add(fireAreaNums);
                    }
                }
                spraySystem.MainLoops[j] = branchLoop;
            }
        }

        public static void DfsBranch(Point3dEx start, Point3dEx cur, Point3dEx target, List<Point3dEx> branchLoop, List<Point3dEx> tempPath, HashSet<Point3dEx> visited,
    SprayIn sprayIn, ref bool hasValve, ref bool hasFlow, Stopwatch stopwatch, bool flag)
        {
            if (!flag) return;
            if(stopwatch.Elapsed.TotalSeconds > 5)
            {
                stopwatch.Stop();
                flag = false;
                return;
            }
            if (cur.Equals(target))//找到目标点，返回最终路径
            {
                for (int i = 1; i < tempPath.Count; i++)
                {
                    var pt = tempPath[i];
                    if (!sprayIn.PtTypeDic.ContainsKey(pt))
                    {
                        continue;
                    }
                    if (sprayIn.PtTypeDic[pt].Contains("Valve"))
                    {
                        hasValve = true;
                    }

                    var spatialIndex = new ThCADCoreNTSSpatialIndex(sprayIn.FlowBlocks);
                    var rec = pt._pt.GetRect(50);
                    var qureys = spatialIndex.SelectCrossingPolygon(rec);
                    if (qureys.Count > 0)
                    {
                        hasFlow = true;
                    }
                }
                flag = true;
                return;
            }
            var neighbors = sprayIn.PtDic[cur];//当前点的邻接点
            foreach (Point3dEx p in neighbors)
            {
                if (visited.Contains(p)) continue;
                if (branchLoop.Contains(p)) continue;
                tempPath.Add(p);
                visited.Add(p);
                DfsBranch(start, p, target, branchLoop, tempPath, visited, sprayIn, ref hasValve, ref hasFlow, stopwatch, flag);
                tempPath.RemoveAt(tempPath.Count - 1);
                visited.Remove(p);
            }
         }
    }
}



