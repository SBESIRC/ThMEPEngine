using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Method
{
    public class SubLoop
    {
        public static List<List<Point3dEx>> Get(FireHydrantSystemIn fireHydrantSysIn, List<List<Point3dEx>> mainPathList)
        {
            var subPathList = new List<List<Point3dEx>>();//次环路最终路径 List
            var visited = new HashSet<Point3dEx>();//访问标志
            foreach (var nd in fireHydrantSysIn.NodeList)
            {
                if (!fireHydrantSysIn.PtDic.ContainsKey(nd[0]))
                {
                    continue;
                }
                if (!mainPathList.First().Contains(nd[0]) && !mainPathList.First().Contains(nd[1]))
                {
                    continue;
                }
                var subTempPath = new List<Point3dEx>();//次环路临时路径
                var subRstPath = new List<Point3dEx>();//次环路临时路径

                visited.Add(nd[0]);
                subTempPath.Add(nd[0]);
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                //次环路深度搜索
                DepthFirstSearch.DfsSubLoop(nd[0], subTempPath, visited, ref subPathList, nd[1], fireHydrantSysIn, stopwatch);
                stopwatch.Stop();
                visited.Remove(visited.Last());//删除占用的点，避免干扰其他次环的遍历
            }
            ThPointCountService.SetPointType(ref fireHydrantSysIn, subPathList);

            return subPathList;
        }
    }
}
