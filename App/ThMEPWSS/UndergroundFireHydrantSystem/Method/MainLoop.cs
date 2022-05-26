using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.General;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Method
{
    public class MainLoop
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
            var mainLoopFlag = DepthFirstSearch.dfsMainLoop(fireHydrantSysIn.StartEndPts[0], fireHydrantSysIn.StartEndPts[1], tempPath, visited,
                ref mainPathList, fireHydrantSysIn, ref extraNodes);
            ThPointCountService.SetPointType(ref fireHydrantSysIn, mainPathList);

            return mainPathList;
        }

        public static List<List<Point3dEx>> GetAcross(FireHydrantSystemIn fireHydrantSysIn)
        {
            var mainPathList = new List<List<Point3dEx>>();//主环路最终路径
            var visited = new HashSet<Point3dEx>();//访问标志
            var extraNodes = new List<Point3dEx>();//主环路连通阀点集
            var tempPath = new List<Point3dEx>();//主环路临时路径
            var throughPts = GetThroughPts(fireHydrantSysIn);
            if(throughPts.Count > 1)
            {
                var startPt = throughPts[0];
                var endPt = throughPts[1];
                visited.Add(startPt);
                tempPath.Add(startPt);
                //主环路深度搜索
                var mainLoopFlag = DepthFirstSearch.dfsMainLoop(startPt, endPt, tempPath, visited,
                    ref mainPathList, fireHydrantSysIn, ref extraNodes);
                ThPointCountService.SetPointType(ref fireHydrantSysIn, mainPathList);
            }

            return mainPathList;
        }

        private static List<Point3dEx> GetThroughPts(FireHydrantSystemIn fireHydrantSysIn)
        {
            var curFloor = fireHydrantSysIn.StartEndPts[0]._pt.GetFloorInt(fireHydrantSysIn.FloorRect);//获取环管点的楼层号
            var throughPts = new List<Point3dEx>();
            foreach (var pt in fireHydrantSysIn.ThroughPt)
            {
                var ptFloor = pt._pt.GetFloorInt(fireHydrantSysIn.FloorRect);//获取当前点的楼层号
                if(curFloor!=ptFloor)
                {
                    throughPts.Add(pt);
                }
            }
            return throughPts;
        }
    }
}
