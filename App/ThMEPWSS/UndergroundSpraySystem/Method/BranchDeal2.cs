using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundSpraySystem.Method
{
    public class BranchDeal2
    {
        public static void Get(ref HashSet<Point3dEx> visited, SprayIn sprayIn, SpraySystem spraySystem)
        {
            foreach(var branchLoop in spraySystem.MainLoops)
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
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ;
                }
                if (!indexOrder)
                {
                    branchLoop.Reverse();
                }

                for (int i = 1; i < branchLoop.Count - 1; i++)
                {
                    try
                    {
                        var pt = branchLoop[i];
                        if (sprayIn.PtTypeDic[pt].Contains("Alarm"))
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
                        ;
                        
                    }
                }
                foreach (var branchLoopPt in branchLoop)
                {
                    try
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
                    catch
                    {
                        ;
                    }
                }
            }
        }
    }
}



