using System;
using System.Collections.Generic;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using Linq2Acad;
using ThMEPWSS.UndergroundFireHydrantSystem.Method;
using System.Linq;

namespace ThMEPWSS.UndergroundSpraySystem.Command
{
    public static class FireHydrantAcross
    {
        public static bool Cmd(AcadDatabase curDb, FireHydrantSystemIn fireHydrantSysIn, FireHydrantSystemOut fireHydrantSysOut,
            double pipeLen1, int subPathLsCnt)
        {
            var mainPathList = MainLoop.GetAcross(fireHydrantSysIn);//主环提取
            if (mainPathList.Count == 0)
            {
                return false;
            }

            var subPathList = SubLoop.Get(fireHydrantSysIn, mainPathList);//支环提取

            var visited = new HashSet<Point3dEx>();//访问标志
            visited.AddVisit(mainPathList);
            visited.AddVisit(subPathList);

            var branchDic = new Dictionary<Point3dEx, List<Point3dEx>>();//支点 + 端点
            var ValveDic = new Dictionary<Point3dEx, List<Point3dEx>>();//支点 + 阀门点
            PtDic.CreateBranchDic(ref branchDic, ref ValveDic, mainPathList, fireHydrantSysIn, visited);
            PtDic.CreateBranchDic(ref branchDic, ref ValveDic, subPathList, fireHydrantSysIn, visited);

            var checkPipe = new CheckPipe(mainPathList, subPathList);
            checkPipe.DrawMainLoop(curDb);
            checkPipe.DrawSubLoop(curDb);
            checkPipe.DrawBranchLoop(curDb, fireHydrantSysIn, branchDic);

            double pepeLen2 = GetFireHydrantPipe.GetMainLoop(fireHydrantSysOut, mainPathList[0], fireHydrantSysIn, branchDic,true);//主环路获取
            GetFireHydrantPipe.GetSubLoop(fireHydrantSysOut, subPathList, fireHydrantSysIn, branchDic, true,subPathLsCnt);//次环路获取
            GetFireHydrantPipe.GetBranch(ref fireHydrantSysOut, branchDic, ValveDic, fireHydrantSysIn);//支路获取

            double pipeLen = Math.Max(pipeLen1, pepeLen2) + 3000;
            Service.StoreyLine.Get(fireHydrantSysIn, fireHydrantSysOut, pipeLen);
            return true;
        }
    }
}
