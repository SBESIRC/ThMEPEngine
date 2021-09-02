using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Method
{
    class MainLoop
    {
        public static List<List<Point3dEx>> Get(ref FireHydrantSystemIn fireHydrantSysIn)
        {
            var mainPathList = new List<List<Point3dEx>>();//主环路最终路径
            var visited = new HashSet<Point3dEx>();//访问标志
            var extraNodes = new List<Point3dEx>();//主环路连通阀点集
            var tempPath = new List<Point3dEx>();//主环路临时路径
            visited.Add(fireHydrantSysIn.StartEndPts[0]);
            tempPath.Add(fireHydrantSysIn.StartEndPts[0]);
            //主环路深度搜索
            DepthFirstSearch.dfsMainLoop(fireHydrantSysIn.StartEndPts[0], fireHydrantSysIn.StartEndPts[1], tempPath, visited, 
                ref mainPathList, fireHydrantSysIn, ref extraNodes);
            ThPointCountService.SetPointType(ref fireHydrantSysIn, mainPathList);
            return mainPathList;
        }
    }
}
