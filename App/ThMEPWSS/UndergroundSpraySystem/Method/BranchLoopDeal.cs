using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundSpraySystem.Method
{
    public class BranchLoopDeal
    {
        public static void Get(ref HashSet<Point3dEx> visited, SprayIn sprayIn, SpraySystem spraySystem)
        {
            foreach (var subLoop in spraySystem.SubLoops)
            {
                
                var tempPath = new List<Point3dEx>();
                
                visited.Clear();
                var pts = new List<Point3dEx>();
                for (int i = 1; i < subLoop.Count - 1; i++)
                {
                    var pt = subLoop[i];
                    visited.Add(pt);
                    if (sprayIn.PtDic[pt].Count == 3)
                    {
                        pts.Add(pt);
                    }
                }

                var usedPtNUms = new List<int>();
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    if(usedPtNUms.Contains(i))
                    {
                        continue;
                    }
                    for (int j = i + 1; j < pts.Count; j++)
                    {
                        if(usedPtNUms.Contains(j))
                        {
                            continue;
                        }
                        tempPath.Clear();
                        tempPath.Add(pts[i]);
                        visited.Add(pts[i]);
                        var flag = false;
                        var branchLoop = new List<Point3dEx>();
                        DepthSearch.DfsBranchLoop(pts[i], pts[j], tempPath, ref visited, ref branchLoop, sprayIn, ref flag, pts);
                        if (branchLoop.Count > 5 && flag)
                        {
                            usedPtNUms.Add(i);
                            usedPtNUms.Add(j);
                            spraySystem.BranchLoops.Add(branchLoop);
                            sprayIn.PtTypeDic[branchLoop.First()] = "BranchLoop";
                            sprayIn.PtTypeDic[branchLoop.Last()] = "BranchLoop";
                            break;
                        }
                    }
                }
            }
        }
    }
}
