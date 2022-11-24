using Linq2Acad;
using System.Collections.Generic;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.UndergroundSpraySystem.Method;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundSpraySystem.Service
{
    public class SpraySysWithMainLoopAcrossFloor//主环跨层
    {
        public static bool Processing(AcadDatabase acadDatabase, SprayIn sprayIn, SpraySystem spraySystem)
        {
            var mainPathList = new List<List<Point3dEx>>();//主环路最终路径
            var extraNodes = new List<Point3dEx>();//主环路连通阀点集
            var visited = new HashSet<Point3dEx>();//访问标志
            var tempPath = new List<Point3dEx>();//主环路临时路径

            visited.Add(sprayIn.LoopStartPt);
            tempPath.Add(sprayIn.LoopStartPt);
            //主环路提取
            Dfs.DfsMainLoopWithMainLoopAcrossFloor(sprayIn.LoopStartPt, tempPath, ref visited, ref mainPathList, sprayIn, ref extraNodes);
            DicTools.SetPointType(sprayIn, mainPathList, extraNodes);
            spraySystem.MainLoop.AddRange(mainPathList[0]);
            BranchLoopDeal.GetWithMainLoopAcrossFloor(ref visited, sprayIn, spraySystem);

            BranchDealWithAcorssFloor.Get(ref visited, sprayIn, spraySystem);
            BranchDeal.GetThrough(ref visited, sprayIn, spraySystem);

            return true;
        }

        public static bool ProcessingInOtherFloor(AcadDatabase acadDatabase, List<Point3dEx> acrossMainLoop, SprayIn sprayIn, SpraySystem spraySystem)
        {
            var visited = new HashSet<Point3dEx>();//访问标志
            acrossMainLoop.ForEach(p => visited.Add(p));
            DicTools.SetPointType(sprayIn, acrossMainLoop);
            spraySystem.MainLoop.Clear();
            spraySystem.BranchLoops.Clear();
            spraySystem.BranchDic.Clear();
            spraySystem.MainLoop.AddRange(acrossMainLoop);
            BranchLoopDeal.GetWithMainLoopAcrossFloor(ref visited, sprayIn, spraySystem);

            BranchDealWithAcorssFloor.Get(ref visited, sprayIn, spraySystem);
            BranchDeal.GetThrough(ref visited, sprayIn, spraySystem);

            return true;
        }


        public static void GetOutput(SprayIn sprayIn, SpraySystem spraySystem, SprayOut sprayOut)
        {
            StoreyLine.Get(sprayOut, spraySystem, sprayIn);
            MainLoopWithAcrossFloor.Get(sprayOut, spraySystem, sprayIn);
            BranchLoopAcrossFloor.Get(sprayOut, spraySystem, sprayIn);
            BranchAcrossFloor.Get(sprayOut, spraySystem, sprayIn);
            PipeLine.Split(sprayOut);
        }

        public static void GetOutputInOtherFloor(SprayIn sprayIn, SpraySystem spraySystem, SprayOut sprayOut, int number = 1)
        {
            MainLoopWithAcrossFloor.GetInOtherFloor(sprayOut, spraySystem, sprayIn, number);
            BranchLoopAcrossFloor.GetInOtherFloor(sprayOut, spraySystem, sprayIn);
            BranchAcrossFloor.GetInOtherFloor(sprayOut, spraySystem, sprayIn);
            PipeLine.Split(sprayOut);
        }
    }
}
