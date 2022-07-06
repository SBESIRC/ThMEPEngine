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

            var subLoop = new List<Point3dEx>();
            if (branchLoopIndex == 1)
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

            foreach (var branchLoop in branchLoopsI)
            {
                BranchDeal.BfsBranch(out int alarmNums, out int fireAreaNums, branchLoop, visited, sprayIn, spraySystem);

                BranchDeal.BfsBranchAcross(branchLoop, sprayIn, spraySystem);

                BranchDeal.SubLoopAdd(spraySystem, subLoop, branchLoop, alarmNums, fireAreaNums);
                

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
