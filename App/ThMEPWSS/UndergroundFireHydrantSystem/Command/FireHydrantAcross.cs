using Linq2Acad;
using System;
using System.Collections.Generic;
using ThMEPWSS.UndergroundFireHydrantSystem.Method;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Command
{
    public static class FireHydrantAcross
    {
        public static bool Cmd(AcadDatabase curDb, FireHydrantSystemIn fireHydrantSysIn, FireHydrantSystemOut fireHydrantSysOut,
            double pipeLen1, int subPathLsCnt)
        {
            var mainPathList = MainLoop.GetAcross(fireHydrantSysIn);
            if (mainPathList.Count == 0)
            {
                return false;
            }

            var subPathList = SubLoop.Get(fireHydrantSysIn, mainPathList);

            var visited = new HashSet<Point3dEx>();
            visited.AddVisit(mainPathList);
            visited.AddVisit(subPathList);

            var branchDic = new Dictionary<Point3dEx, List<Point3dEx>>();
            var ValveDic = new Dictionary<Point3dEx, List<Point3dEx>>();
            PtDic.CreateBranchDic(ref branchDic, ref ValveDic, mainPathList, fireHydrantSysIn, visited);
            PtDic.CreateBranchDic(ref branchDic, ref ValveDic, subPathList, fireHydrantSysIn, visited);

            var checkPipe = new CheckPipe(mainPathList, subPathList);
            checkPipe.DrawMainLoop(curDb);
            checkPipe.DrawSubLoop(curDb);
            checkPipe.DrawBranchLoop(curDb, fireHydrantSysIn, branchDic);

            double pepeLen2 = GetFireHydrantPipe.GetMainLoop(fireHydrantSysOut, mainPathList[0], fireHydrantSysIn, branchDic, true);
            GetFireHydrantPipe.GetSubLoop(fireHydrantSysOut, subPathList, fireHydrantSysIn, branchDic, true, subPathLsCnt);
            GetFireHydrantPipe.GetBranch(ref fireHydrantSysOut, branchDic, ValveDic, fireHydrantSysIn);

            double pipeLen = Math.Max(pipeLen1, pepeLen2) + 3000;
            UndergroundSpraySystem.Service.StoreyLine.Get(fireHydrantSysIn, fireHydrantSysOut, pipeLen);
            return true;
        }
    }
}
