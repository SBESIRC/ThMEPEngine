using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundSpraySystem.Method
{
    public class SubLoopDeal
    {
        public static bool Get(ref HashSet<Point3dEx> visited, List<List<Point3dEx>> mainPathList, SprayIn sprayIn,
             SpraySystem spraySystem)
        {
            visited.Clear();
            var tempPath = new List<Point3dEx>();
            var subLoopPts = new List<List<Point3dEx>>();
            var sePts = new List<Point3dEx>();
            bool flag = true;
            foreach (var pt in mainPathList[0])
            {
                visited.Add(pt);
                if (!sprayIn.PtTypeDic.ContainsKey(pt))
                {
                    continue;
                }
                if (sprayIn.PtTypeDic[pt].Contains("SubLoop"))
                {
                    if (flag)
                    {
                        sePts.Add(pt);
                        flag = false;
                        continue;
                    }
                    else
                    {
                        sePts.Add(pt);
                        subLoopPts.Add(new List<Point3dEx>(sePts));
                        sePts.Clear();
                        flag = true;
                        continue;
                    }
                }
            }

            foreach (var sePt in subLoopPts)
            {
                tempPath.Clear();
                tempPath.Add(sePt[0]);
                visited.Add(sePt[0]);
                var subLoop = new List<Point3dEx>();
                DepthSearch.DfsSubLoop(sePt[0], sePt[1], tempPath, ref visited, ref subLoop, sprayIn);
                spraySystem.SubLoops.Add(subLoop);
                spraySystem.SubLoopBranchDic.Add(subLoop.First(), 0);
                spraySystem.SubLoopBranchDic.Add(subLoop.Last(), 0);
                spraySystem.SubLoopBranchPtDic.Add(subLoop.First(), new List<Point3dEx>());
                spraySystem.SubLoopBranchPtDic.Add(subLoop.Last(), new List<Point3dEx>());
            }
            return spraySystem.SubLoops.Count() > 0;
        }

        public static void SetType(SprayIn sprayIn, SpraySystem spraySystem)
        {
            foreach (var rstPath in spraySystem.SubLoops)
            {
                for (int i = 1; i < rstPath.Count - 1; i++)
                {
                    var pt = rstPath[i];
                    if (sprayIn.PtDic[pt].Count == 3)
                    {
                        if (sprayIn.PtTypeDic[pt].Contains("MainLoop"))
                        {
                            sprayIn.PtTypeDic[pt] = "Branch";
                            spraySystem.SubLoopBranchDic[rstPath[0]] += 1;
                            spraySystem.SubLoopBranchDic[rstPath.Last()] += 1;
                            spraySystem.SubLoopBranchPtDic[rstPath.First()].Add(pt);
                            spraySystem.SubLoopBranchPtDic[rstPath.Last()].Add(pt);
                        }
                    }
                }
            }
        }
    }
}
