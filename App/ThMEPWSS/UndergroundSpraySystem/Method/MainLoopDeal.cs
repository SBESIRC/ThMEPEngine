using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundSpraySystem.Method
{
    public class MainLoopDeal
    {
        public static bool MainLoopDfs(List<List<Point3dEx>> ptsls, SprayIn sprayIn)
        {
            var ptls = new List<List<Point3dEx>>();
            var usedPts = new HashSet<Point3dEx>();
            foreach(var pts in ptsls)
            {
                if (pts.Count < 2) continue;
                for(int i = 0; i < pts.Count - 1; i++)
                {
                    var pti = pts[i];
                    if (usedPts.Contains(pti)) continue;
                    for(int j =i+1; j < pts.Count;j++)
                    {
                        var ptj = pts[j];
                        if (usedPts.Contains(ptj)) continue;
                        var tempPath = new List<Point3dEx> { pti };
                        var visited = new HashSet<Point3dEx> { pti };
                        var rstPath = new List<Point3dEx>();
                        var stopwatch = new Stopwatch();

                        DfsMainLoopInOtherFloor(pti, ptj, tempPath, ref visited, ref rstPath, sprayIn, ref stopwatch);
                        if (rstPath.Count > 10)
                        {
                            ptls.Add(rstPath);
                            usedPts.Add(rstPath.First());
                            usedPts.Add(rstPath.Last());
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static void DfsMainLoopInOtherFloor(Point3dEx cur, Point3dEx target, List<Point3dEx> tempPath, ref HashSet<Point3dEx> visited,
            ref List<Point3dEx> rstPath, SprayIn sprayIn, ref Stopwatch stopwatch)
        {
            if(stopwatch.Elapsed.TotalSeconds > 10)
            {
                stopwatch.Stop();
                return;
            }
            if (cur.Equals(target))//找到目标点，返回最终路径
            {
                rstPath = new List<Point3dEx>(tempPath);
                return;
            }
            var neighbors = sprayIn.PtDic[cur];//当前点的邻接点
            foreach (Point3dEx p in neighbors)
            {
                if (visited.Contains(p)) continue;
                if (sprayIn.ThroughPt.Contains(p) && !target.Equals(p)) continue;
                if (sprayIn.PtTypeDic[p].Contains("AlarmValve")) continue;
                tempPath.Add(p);
                visited.Add(p);

                DfsMainLoopInOtherFloor(p, target,tempPath, ref visited, ref rstPath, sprayIn, ref stopwatch);
                tempPath.RemoveAt(tempPath.Count - 1);
                visited.Remove(p);
            }
        }
    }
}
