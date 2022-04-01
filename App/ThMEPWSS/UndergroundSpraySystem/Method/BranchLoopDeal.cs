using System.Collections.Generic;
using System.Linq;
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
                    if (usedPtNUms.Contains(i))
                    {
                        continue;
                    }
                    for (int j = i + 1; j < pts.Count; j++)
                    {
                        if (usedPtNUms.Contains(j))
                        {
                            continue;
                        }
                        tempPath.Clear();
                        tempPath.Add(pts[i]);
                        visited.Add(pts[i]);
                        var flag = false;
                        var branchLoop = new List<Point3dEx>();
                        string floorNumber = "";
                        DepthSearch.DfsBranchLoop(pts[i], pts[i], pts[j], tempPath, ref visited, ref branchLoop, sprayIn, ref flag, pts, ref floorNumber);
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

        /// <summary>
        /// 获取当前层的支环，不跨层
        /// </summary>
        /// <param name="visited"></param>
        /// <param name="sprayIn"></param>
        /// <param name="spraySystem"></param>
        public static void GetInCurrentFloor(ref HashSet<Point3dEx> visited, SprayIn sprayIn, SpraySystem spraySystem)
        {
            var mainLoop = spraySystem.MainLoop;

            var tempPath = new List<Point3dEx>();

            visited.Clear();
            var pts = new List<Point3dEx>();
            for (int i = 1; i < mainLoop.Count - 1; i++)
            {
                var pt = mainLoop[i];
                visited.Add(pt);
                if (sprayIn.PtDic[pt].Count == 3)
                {
                    pts.Add(pt);
                }
            }

            var usedPtNUms = new List<int>();
            for (int i = 0; i < pts.Count - 1; i++)
            {
                if (usedPtNUms.Contains(i))
                {
                    continue;
                }
                var pti = pts[i];

                for (int j = i + 1; j < pts.Count; j++)
                {
                    if (usedPtNUms.Contains(j))
                    {
                        continue;
                    }
                    tempPath.Clear();
                    tempPath.Add(pti);
                    visited.Add(pts[i]);
                    var flag = false;
                    var branchLoop = new List<Point3dEx>();
                    var floorNumber = "";
                    DepthSearch.DfsCurrentFloorBranchLoop(pti, pti, pts[j], tempPath, ref visited, ref branchLoop, sprayIn, ref flag, pts, ref floorNumber);
                    if (branchLoop.Count > 5 && flag)
                    {
                        usedPtNUms.Add(i);
                        usedPtNUms.Add(j);
                        spraySystem.BranchLoops.Add(branchLoop);
                        sprayIn.PtTypeDic[branchLoop.First()] = "BranchLoop" + floorNumber;
                        sprayIn.PtTypeDic[branchLoop.Last()] = "BranchLoop" + floorNumber;
                        break;
                    }
                }
            }
        }

        public static void GetWithAcrossFloor(ref HashSet<Point3dEx> visited, SprayIn sprayIn, SpraySystem spraySystem)
        {
            var mainLoop = spraySystem.MainLoop;

            var tempPath = new List<Point3dEx>();

            visited.Clear();
            var pts = new List<Point3dEx>();
            for (int i = 1; i < mainLoop.Count - 1; i++)
            {
                var pt = mainLoop[i];
                visited.Add(pt);
                if (sprayIn.PtDic[pt].Count == 3)
                {
                    pts.Add(pt);
                }
            }

            var usedPtNUms = new List<int>();
            for (int i = 0; i < pts.Count - 1; i++)
            {
                if (usedPtNUms.Contains(i))
                {
                    continue;
                }
                var pti = pts[i];
  
                for (int j = i + 1; j < pts.Count; j++)
                {
                    if (usedPtNUms.Contains(j))
                    {
                        continue;
                    }
                    tempPath.Clear();
                    tempPath.Add(pti);
                    visited.Add(pts[i]);
                    var flag = false;
                    var branchLoop = new List<Point3dEx>();
                    var floorNumber = "";
                    DepthSearch.DfsBranchLoop(pti, pti, pts[j], tempPath, ref visited, ref branchLoop, sprayIn, ref flag, pts, ref floorNumber);
                    if (branchLoop.Count > 5 && flag)
                    {
                        usedPtNUms.Add(i);
                        usedPtNUms.Add(j);
                        spraySystem.BranchLoops.Add(branchLoop);
                        sprayIn.PtTypeDic[branchLoop.First()] = "BranchLoop" + floorNumber;
                        sprayIn.PtTypeDic[branchLoop.Last()] = "BranchLoop" + floorNumber;
                        break;
                    }
                }
            }
        }


        /// <summary>
        /// 获取支环上的支环
        /// </summary>
        /// <param name="visited"></param>
        /// <param name="sprayIn"></param>
        /// <param name="spraySystem"></param>
        public static void GetWithAcrossFloor2(ref HashSet<Point3dEx> visited, SprayIn sprayIn, SpraySystem spraySystem)
        {
            foreach(var branchLoop in spraySystem.BranchLoops)
            {
                var pts = Get3NeighborPts(sprayIn, branchLoop, ref visited);
                BranchLoopGet(2, pts, visited, sprayIn, spraySystem);
            }
            if(spraySystem.BranchLoops2.Count == 0)
            {
                return;
            }
            foreach(var branchLoop2 in spraySystem.BranchLoops2)
            {
                var pts = Get3NeighborPts(sprayIn, branchLoop2, ref visited);
                BranchLoopGet(3, pts, visited, sprayIn, spraySystem);
            }
        }

        /// <summary>
        /// 提取支环上的除了报警阀的所有支路点
        /// </summary>
        /// <param name="sprayIn"></param>
        /// <param name="bLoop"></param>
        /// <param name="visited"></param>
        /// <returns></returns>
        private static List<Point3dEx> Get3NeighborPts(SprayIn sprayIn, List<Point3dEx> bLoop, ref HashSet<Point3dEx> visited)
        {
            visited.Clear();
            var pts = new List<Point3dEx>();
            for (int i = 1; i < bLoop.Count - 1; i++)
            {
                var pt = bLoop[i];
                visited.Add(pt);

                if (sprayIn.PtDic[pt].Count == 3)
                {
                    if (sprayIn.PtTypeDic.ContainsKey(pt))
                    {
                        if (!sprayIn.PtTypeDic[pt].Contains("AlarmValve"))
                        {
                            pts.Add(pt);
                        }
                    }
                    else
                    {
                        pts.Add(pt);
                    }
                }
            }

            return pts;
        }

        private static void BranchLoopGet(int branchLoopIndex, List<Point3dEx> pts, HashSet<Point3dEx> visited, SprayIn sprayIn, SpraySystem spraySystem)
        {
            var tempPath = new List<Point3dEx>();

            var usedPtNUms = new List<int>();
            for (int i = 0; i < pts.Count - 1; i++)
            {
                if (usedPtNUms.Contains(i))
                {
                    continue;
                }
                var pti = pts[i];

                for (int j = i + 1; j < pts.Count; j++)
                {
                    if (usedPtNUms.Contains(j))
                    {
                        continue;
                    }
                    tempPath.Clear();
                    tempPath.Add(pti);
                    visited.Add(pts[i]);
                    var flag = false;
                    var branchLoop = new List<Point3dEx>();
                    var floorNumber = "";
                    var throughoutCnts = 0;

                    DepthSearch.DfsBranchLoop2(pti, pti, pts[j], tempPath, ref visited, ref branchLoop, sprayIn, ref flag, pts, ref floorNumber, ref throughoutCnts);
                    if (branchLoop.Count > 5 && flag)
                    {
                        usedPtNUms.Add(i);
                        usedPtNUms.Add(j);
                        BranchLoopAdd(branchLoopIndex, branchLoop, floorNumber, sprayIn, spraySystem);
                        break;
                    }
                }
            }
        }

        private static void BranchLoopAdd(int branchLoopIndex, List<Point3dEx> branchLoop, string floorNumber, SprayIn sprayIn, SpraySystem spraySystem)
        {
            if(branchLoopIndex == 1)//报警阀环路，1级环路，搭在主/次环上
            {
                spraySystem.BranchLoops.Add(branchLoop);
                sprayIn.PtTypeDic[branchLoop.First()] = "BranchLoop" + floorNumber;
                sprayIn.PtTypeDic[branchLoop.Last()] = "BranchLoop" + floorNumber;
                return;
            }
            if(branchLoopIndex == 2)//报警阀环路上的环路，2级环路，搭在1级环路上
            {
                spraySystem.BranchLoops2.Add(branchLoop);
                sprayIn.PtTypeDic[branchLoop.First()] = "2BranchLoop" + floorNumber;
                sprayIn.PtTypeDic[branchLoop.Last()] = "2BranchLoop" + floorNumber;
                return;
            }
            if (branchLoopIndex == 3)//3级环路，搭在2级环路上 
            {
                spraySystem.BranchLoops3.Add(branchLoop);
                sprayIn.PtTypeDic[branchLoop.First()] = "3BranchLoop" + floorNumber;
                sprayIn.PtTypeDic[branchLoop.Last()] = "3BranchLoop" + floorNumber;
                return;
            }
        }


        /// <summary>
        /// 处理主环跨层
        /// </summary>
        /// <param name="visited"></param>
        /// <param name="sprayIn"></param>
        /// <param name="spraySystem"></param>
        public static void GetWithMainLoopAcrossFloor(ref HashSet<Point3dEx> visited, SprayIn sprayIn, SpraySystem spraySystem)
        {
            var  mainLoop = spraySystem.MainLoop;
            var tempPath = new List<Point3dEx>();

            visited.Clear();
            var pts = new List<Point3dEx>();
            for (int i = 1; i < mainLoop.Count - 1; i++)
            {
                var pt = mainLoop[i];
                visited.Add(pt);
                if (sprayIn.PtDic[pt].Count == 3)
                {
                    pts.Add(pt);
                }
            }

            var usedPtNUms = new List<int>();
            for (int i = 0; i < pts.Count - 1; i++)
            {
                if (usedPtNUms.Contains(i))
                {
                    continue;
                }
                var pti = pts[i];

                for (int j = i + 1; j < pts.Count; j++)
                {
                    if (usedPtNUms.Contains(j))
                    {
                        continue;
                    }
                    tempPath.Clear();
                    tempPath.Add(pti);
                    visited.Add(pts[i]);
                    var flag = false;
                    var branchLoop = new List<Point3dEx>();
                    var floorNumber = "";
                    DepthSearch.DfsBranchLoopInCurrentFloor(pti, pti, pts[j], tempPath, ref visited, ref branchLoop, sprayIn, ref flag, pts, ref floorNumber);
                    if (branchLoop.Count > 5 && flag)
                    {
                        usedPtNUms.Add(i);
                        usedPtNUms.Add(j);
                        spraySystem.BranchLoops.Add(branchLoop);
                        sprayIn.PtTypeDic[branchLoop.First()] = "BranchLoop" + floorNumber;
                        sprayIn.PtTypeDic[branchLoop.Last()] = "BranchLoop" + floorNumber;
                        break;
                    }
                    ;
                    branchLoop.Clear();
                    int throughoutCnts = 0;//穿越点数目
                    DepthSearch.DfsMainLoopInOtherFloor(pti, pti, pts[j], tempPath, ref visited, ref branchLoop, sprayIn, 
                        ref flag, pts, ref floorNumber, ref throughoutCnts);//提取跨层主环
                    if(branchLoop.Count > 20)
                    {
                        ;
                        var rstMainLoop = new List<Point3dEx>();
                        rstMainLoop.AddRange(branchLoop);
                        spraySystem.MainLoopsInOtherFloor.Add(rstMainLoop);
                        Draw.MainLoopsInOtherFloor(branchLoop);
                        usedPtNUms.Add(i);
                        usedPtNUms.Add(j);
                        //spraySystem.BranchLoops.Add(branchLoop);
                        sprayIn.PtTypeDic[branchLoop.First()] = "MainLoopAcross";
                        sprayIn.PtTypeDic[branchLoop.Last()] = "MainLoopAcross";
                        break;
                    }
                    ;
                }
            } 
        }
    }
}
