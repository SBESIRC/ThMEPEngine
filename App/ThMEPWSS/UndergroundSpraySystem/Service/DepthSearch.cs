using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    public class DepthSearch
    {
        public static void DfsMainLoopWithoutAlarmValve(Point3dEx cur, List<Point3dEx> tempPath, HashSet<Point3dEx> visited,
           ref List<Point3dEx> rstPath, SprayIn sprayIn, Stopwatch stopwatch)
        {
            if (stopwatch.Elapsed.TotalSeconds > 10)//
            {
                stopwatch.Stop();
                return;
            }
            if (cur.Equals(sprayIn.LoopEndPt))//找到目标点，返回最终路径
            {
                rstPath.AddRange(new List<Point3dEx>(tempPath));//把当前路径加入
                return;
            }
            var neighbors = sprayIn.PtDic[cur];//当前点的邻接点
            foreach (Point3dEx p in neighbors)
            {
                if (visited.Contains(p)) continue;
                if (sprayIn.PtTypeDic[p].Contains("PressureValves")) continue;
                if (sprayIn.PtTypeDic[p].Contains("AlarmValve")) 
                    continue;
                tempPath.Add(p);
                visited.Add(p);

                DfsMainLoopWithoutAlarmValve(p, tempPath, visited, ref rstPath, sprayIn, stopwatch);
                tempPath.RemoveAt(tempPath.Count - 1);
                visited.Remove(p);
            }
        }



        public static void DfsMainLoop(Point3dEx cur, List<Point3dEx> tempPath, HashSet<Point3dEx> visited, 
            ref List<List<Point3dEx>> rstPaths, SprayIn sprayIn, ref List<Point3dEx> extraNodes, HashSet<Point3dEx> neverVisited)
        {
            if (cur.Equals(sprayIn.LoopEndPt))//找到目标点，返回最终路径
            {
                var rstPath = new List<Point3dEx>(tempPath);
                var flag = true;
                foreach(var pt in rstPath)
                {
                    if(sprayIn.PtTypeDic[pt].Contains("AlarmValve"))
                    {
                        neverVisited.Add(pt);
                        break;
                    }
                }
                if (rstPaths.Count == 0)//主环数为0
                {
                    rstPaths.Add(rstPath);//把当前路径加入
                }
                else//存在主环
                {
                    if(rstPath.Count > 20)
                    {
                        rstPaths.Add(rstPath);
                        return;
                    }
                    foreach (var tmpPath in rstPaths.ToArray())//主环遍历
                    {
                        if (tmpPath[0] == rstPath[0] && tmpPath.Last() == rstPath.Last())//存在同起点和终点的主环
                        {
                            if (tmpPath.Count < rstPath.Count)//取点数多的
                            {
                                rstPaths.Remove(tmpPath);
                                rstPaths.Add(rstPath);
                                flag = false;
                                foreach (var pt in tmpPath)//额外的点加入列表
                                {
                                    if (!rstPath.Contains(pt))
                                    {
                                        extraNodes.Add(pt);
                                    }
                                }
                            }
                            else
                            {
                                flag = false;
                                foreach (var pt in rstPath)
                                {
                                    if (!tmpPath.Contains(pt))
                                    {
                                        extraNodes.Add(pt);
                                    }
                                }
                            }
                        }
                    }
                    if (flag)
                    {
                        rstPaths.Add(rstPath);
                    }
                }
                return;
            }
            var neighbors = sprayIn.PtDic[cur];//当前点的邻接点

            foreach (Point3dEx p in neighbors)
            {
                try
                {
                    if (visited.Contains(p)) continue;
                    if (neverVisited.Contains(p)) continue;
                    if (sprayIn.PtTypeDic[p].Contains("PressureValves")) continue;
                    tempPath.Add(p);
                    visited.Add(p);

                    DfsMainLoop(p, tempPath, visited, ref rstPaths, sprayIn, ref extraNodes, neverVisited);
                    tempPath.RemoveAt(tempPath.Count - 1);
                    visited.Remove(p);
                }
                catch(Exception ex)
                {
                    ;
                }
            }
        }

        public static void DfsMainLoopWithAcrossFloor(Point3dEx cur, List<Point3dEx> tempPath, ref HashSet<Point3dEx> visited,
            ref List<List<Point3dEx>> rstPaths, SprayIn sprayIn, ref List<Point3dEx> extraNodes)
        {
            if (cur.Equals(sprayIn.LoopEndPt))//找到目标点，返回最终路径
            {
                if(tempPath.Count > 10)
                {
                    var rstPath = new List<Point3dEx>(tempPath);
                    rstPaths.Add(rstPath);//把当前路径加入
                }
                
                return;
            }
            var neighbors = sprayIn.PtDic[cur];//当前点的邻接点
            foreach (Point3dEx p in neighbors)
            {
                if (visited.Contains(p)) continue;
                if(!sprayIn.PtTypeDic.ContainsKey(p))
                {
                    ;
                }
                else
                {
                    if (sprayIn.PtTypeDic[p].Contains("AlarmValve"))
                        continue;
                }
     
                tempPath.Add(p);
                visited.Add(p);

                DfsMainLoopWithAcrossFloor(p, tempPath, ref visited, ref rstPaths, sprayIn, ref extraNodes);
                tempPath.RemoveAt(tempPath.Count - 1);
                visited.Remove(p);
            }
        }

        public static void DfsMainLoopWithMainLoopAcrossFloor(Point3dEx cur, List<Point3dEx> tempPath, ref HashSet<Point3dEx> visited,
            ref List<List<Point3dEx>> rstPaths, SprayIn sprayIn, ref List<Point3dEx> extraNodes)
        {
            if (cur.Equals(sprayIn.LoopEndPt))//找到目标点，返回最终路径
            {
                var rstPath = new List<Point3dEx>(tempPath);
                rstPaths.Add(rstPath);//把当前路径加入
                return;
            }
            var neighbors = sprayIn.PtDic[cur];//当前点的邻接点
            foreach (Point3dEx p in neighbors)
            {
                if (visited.Contains(p)) continue;
                if (sprayIn.PtTypeDic[p].Contains("AlarmValve"))
                    continue;
                if (sprayIn.ThroughPt.Contains(p)) 
                    continue;
                tempPath.Add(p);
                visited.Add(p);

                DfsMainLoopWithMainLoopAcrossFloor(p, tempPath, ref visited, ref rstPaths, sprayIn, ref extraNodes);
                tempPath.RemoveAt(tempPath.Count - 1);
                visited.Remove(p);
            }
        }

      
        public static void DfsSubLoop(Point3dEx cur, Point3dEx targetPt, List<Point3dEx> tempPath, ref HashSet<Point3dEx> visited,
           ref List<Point3dEx> rstPath, SprayIn sprayIn)
        {
            if (cur.Equals(targetPt))//找到目标点，返回最终路径
            {
                if(tempPath.Last()._pt.DistanceTo(cur._pt) >1)
                {
                    tempPath.Add(cur);
                }
                rstPath.AddRange(tempPath);
                return;
            }

            var neighbors = sprayIn.PtDic[cur];//当前点的邻接点
            foreach (Point3dEx p in neighbors)
            {
                if (!p.Equals(targetPt) && visited.Contains(p)) continue;
                if(!sprayIn.PtTypeDic.ContainsKey(p))
                {
                    continue;
                }
                if (sprayIn.PtTypeDic[p].Contains("AlarmValve")) continue;
                tempPath.Add(p);
                visited.Add(p);
                DfsSubLoop(p, targetPt, tempPath, ref visited, ref rstPath, sprayIn);
                tempPath.RemoveAt(tempPath.Count - 1);
                visited.Remove(p);
            }
        }

        public static void DfsBranchLoop(Point3dEx cur, Point3dEx startPt, Point3dEx targetPt, List<Point3dEx> tempPath, ref HashSet<Point3dEx> visited,
           ref List<Point3dEx> rstPath, SprayIn sprayIn, ref bool flag, List<Point3dEx> pts, ref string floorNumber)
        {
            if (cur.Equals(targetPt))//找到目标点，返回最终路径
            {
                rstPath.AddRange(tempPath);
                return;
            }
            
            var neighbors = sprayIn.PtDic[cur];//当前点的邻接点
            foreach (Point3dEx p in neighbors)
            {
                if (cur.Equals(startPt) && p.Equals(targetPt)) continue;//起点和终点相邻
                if (!p.Equals(targetPt) && visited.Contains(p)) continue;
                if (pts.Contains(p) && !p.Equals(targetPt)) 
                    continue;
                if (!sprayIn.PtTypeDic.ContainsKey(p))
                {
                    continue;
                }
                if (sprayIn.PtTypeDic[p].Contains("AlarmValve"))
                {
                    floorNumber = p._pt.GetFloor(sprayIn.FloorRectDic);
                    flag = true;
                }
                
                tempPath.Add(p);
                visited.Add(p);
                DfsBranchLoop(p, startPt, targetPt, tempPath, ref visited, ref rstPath, sprayIn, ref flag, pts, ref floorNumber);
                tempPath.RemoveAt(tempPath.Count - 1);
                visited.Remove(p);
            }
        }

        /// <summary>
        /// 不跨层支环,深度优先查找本层支环
        /// </summary>
        /// <param name="cur"></param>
        /// <param name="startPt"></param>
        /// <param name="targetPt"></param>
        /// <param name="tempPath"></param>
        /// <param name="visited"></param>
        /// <param name="rstPath"></param>
        /// <param name="sprayIn"></param>
        /// <param name="flag"></param>
        /// <param name="pts"></param>
        /// <param name="floorNumber"></param>
        public static void DfsCurrentFloorBranchLoop(Point3dEx cur, Point3dEx startPt, Point3dEx targetPt, List<Point3dEx> tempPath, ref HashSet<Point3dEx> visited,
           ref List<Point3dEx> rstPath, SprayIn sprayIn, ref bool flag, List<Point3dEx> pts, ref string floorNumber)
        {
            if (cur.Equals(targetPt))//找到目标点，返回最终路径
            {
                rstPath.AddRange(tempPath);
                return;
            }

            var neighbors = sprayIn.PtDic[cur];//当前点的邻接点
            foreach (Point3dEx p in neighbors)
            {
                if (cur.Equals(startPt) && p.Equals(targetPt)) continue;//起点和终点相邻
                if (!p.Equals(targetPt) && visited.Contains(p)) continue;
                if (pts.Contains(p) && !p.Equals(targetPt))
                    continue;
                if (!sprayIn.PtTypeDic.ContainsKey(p))
                {
                    continue;
                }

                if(sprayIn.ThroughPt.Contains(p))//跳过跨层点
                {
                    continue;
                }

                if (sprayIn.PtTypeDic[p].Contains("AlarmValve"))
                {
                    floorNumber = p._pt.GetFloor(sprayIn.FloorRectDic);
                    flag = true;
                }

                tempPath.Add(p);
                visited.Add(p);
                DfsCurrentFloorBranchLoop(p, startPt, targetPt, tempPath, ref visited, ref rstPath, sprayIn, ref flag, pts, ref floorNumber);
                tempPath.RemoveAt(tempPath.Count - 1);
                visited.Remove(p);
            }
        }


        public static bool DfsBranchLoop2(Point3dEx cur, Point3dEx startPt, Point3dEx targetPt, List<Point3dEx> tempPath, ref HashSet<Point3dEx> visited,
           ref List<Point3dEx> rstPath, SprayIn sprayIn, ref bool flag, List<Point3dEx> pts, ref string floorNumber, ref int throughoutCnts)
        {
            if (cur.Equals(targetPt))//找到目标点，返回最终路径
            {
                rstPath.AddRange(tempPath);
                return true;
            }

            var neighbors = sprayIn.PtDic[cur];//当前点的邻接点
            if(neighbors.Count == 1)
            {
                ;
            }
            foreach (Point3dEx p in neighbors)
            {
                if (cur.Equals(startPt) && p.Equals(targetPt)) continue;//起点和终点相邻
                if (!p.Equals(targetPt) && visited.Contains(p)) continue;//该点已经遍历
                if (pts.Contains(p) && !p.Equals(targetPt)) continue;//找到了其他的终点
                if (sprayIn.PtTypeDic.ContainsKey(p))
                {
                    if (sprayIn.PtTypeDic[p].Contains("AlarmValve"))
                    {
                        floorNumber = p._pt.GetFloor(sprayIn.FloorRectDic);
                        flag = true;
                    }
                }

                if (sprayIn.ThroughPt.Contains(p))
                {
                    if (throughoutCnts < 4)
                    {
                        throughoutCnts++;
                    }
                    else
                    {
                        continue;
                    }
                }

                tempPath.Add(p);
                visited.Add(p);
                var rst = DfsBranchLoop2(p, startPt, targetPt, tempPath, ref visited, ref rstPath, sprayIn, ref flag, pts, ref floorNumber, ref throughoutCnts);
                if (rst) return true;
                tempPath.RemoveAt(tempPath.Count - 1);
                visited.Remove(p);
                if (sprayIn.ThroughPt.Contains(p))
                {
                    throughoutCnts--;
                }
            }
            return false;
        }

        /// <summary>
        /// 针对主环跨楼层的情况
        /// </summary>
        /// <param name="cur"></param>
        /// <param name="startPt"></param>
        /// <param name="targetPt"></param>
        /// <param name="tempPath"></param>
        /// <param name="visited"></param>
        /// <param name="rstPath"></param>
        /// <param name="sprayIn"></param>
        /// <param name="flag"></param>
        /// <param name="pts"></param>
        /// <param name="floorNumber"></param>
        public static void DfsBranchLoopInCurrentFloor(Point3dEx cur, Point3dEx startPt, Point3dEx targetPt, List<Point3dEx> tempPath, ref HashSet<Point3dEx> visited,
               ref List<Point3dEx> rstPath, SprayIn sprayIn, ref bool flag, List<Point3dEx> pts, ref string floorNumber)
        {
            if (cur.Equals(targetPt))//找到目标点，返回最终路径
            {
                rstPath.AddRange(tempPath);
                return;
            }

            var neighbors = sprayIn.PtDic[cur];//当前点的邻接点
            foreach (Point3dEx p in neighbors)
            {
                if (cur.Equals(startPt) && p.Equals(targetPt)) continue;//起点和终点相邻
                if (!p.Equals(targetPt) && visited.Contains(p)) continue;
                if (sprayIn.ThroughPt.Contains(p))
                    continue;
                if (pts.Contains(p) && !p.Equals(targetPt))
                    continue;
                if (!sprayIn.PtTypeDic.ContainsKey(p))
                {
                    continue;
                }
                if (sprayIn.PtTypeDic[p].Contains("AlarmValve"))
                {
                    floorNumber = p._pt.GetFloor(sprayIn.FloorRectDic);
                    flag = true;
                }

                tempPath.Add(p);
                visited.Add(p);
                DfsBranchLoopInCurrentFloor(p, startPt, targetPt, tempPath, ref visited, ref rstPath, sprayIn, ref flag, pts, ref floorNumber);
                tempPath.RemoveAt(tempPath.Count - 1);
                visited.Remove(p);
            }
        }


        /// <summary>
        /// 深度优先查找跨层的主环
        /// </summary>
        /// <param name="cur"></param>
        /// <param name="tempPath"></param>
        /// <param name="visited"></param>
        /// <param name="rstPaths"></param>
        /// <param name="sprayIn"></param>
        /// <param name="extraNodes"></param>
        public static bool DfsMainLoopInOtherFloor(Point3dEx cur, Point3dEx startPt, Point3dEx targetPt, List<Point3dEx> tempPath, ref HashSet<Point3dEx> visited,
               ref List<Point3dEx> rstPath, SprayIn sprayIn, ref bool flag, List<Point3dEx> pts, ref string floorNumber, ref int throughoutCnts)
        {
            if (cur.Equals(targetPt))//找到目标点，返回最终路径
            {
                rstPath.AddRange(tempPath);
                return true;
            }

            var neighbors = sprayIn.PtDic[cur];//当前点的邻接点
            foreach (Point3dEx p in neighbors)
            {
                if (cur.Equals(startPt) && p.Equals(targetPt)) continue;//起点和终点相邻
                if (!p.Equals(targetPt) && visited.Contains(p)) continue;
                if (pts.Contains(p) && !p.Equals(targetPt))  continue;

                if (sprayIn.PtTypeDic.ContainsKey(p))
                {
                    if (sprayIn.PtTypeDic[p].Contains("AlarmValve"))
                    {
                        continue;
                    }
                }
   
                if (sprayIn.ThroughPt.Contains(p))
                {
                    if (throughoutCnts < 4)
                    {
                        throughoutCnts++;
                    }
                    else
                    {
                        continue;
                    }
                }

                tempPath.Add(p);
                visited.Add(p);
                var rst = DfsMainLoopInOtherFloor(p, startPt, targetPt, tempPath, ref visited, ref rstPath, sprayIn, ref flag, pts, ref floorNumber, ref throughoutCnts);
                if (rst) return true;
                tempPath.RemoveAt(tempPath.Count - 1);
                visited.Remove(p);
                if (sprayIn.ThroughPt.Contains(p))
                {
                    throughoutCnts--;
                }
            }
            return false;
        }
    }
}
