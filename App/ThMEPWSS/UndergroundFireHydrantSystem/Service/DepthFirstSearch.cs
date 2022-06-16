using System.Linq;
using System.Collections.Generic;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using System.Diagnostics;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    class DepthFirstSearch
    {
        public static bool dfsMainLoop(Point3dEx cur, Point3dEx target, List<Point3dEx> tempPath, HashSet<Point3dEx> visited, 
            ref List<List<Point3dEx>> rstPaths, FireHydrantSystemIn fireHydrantSysIn, ref List<Point3dEx> extraNodes)
        {
            if (cur.Equals(target))//找到目标点，返回最终路径
            {
                var rstPath = new List<Point3dEx>(tempPath);
                var flag = true;
                if (rstPath.Count < 15)
                {
                    return false;
                }
                if (rstPaths.Count == 0)//主环数为0
                {
                    rstPaths.Add(rstPath);//把当前路径加入
                }
                else//存在主环
                {
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
                return true;
            }

            var neighbors = fireHydrantSysIn.PtDic[cur];//当前点的邻接点
            var subLoopPoint = false;//次环标志
            foreach (List<Point3dEx> nd in fireHydrantSysIn.NodeList)
            {
                if (nd.Contains(cur))
                { 
                    subLoopPoint = true;
                    break;
                }
            }
            foreach (Point3dEx p in neighbors)
            {
                var isOmit = IsOmitPt(p, cur, target, subLoopPoint, visited, fireHydrantSysIn);
                if (isOmit) continue;
                tempPath.Add(p);
                visited.Add(p);

                //递归搜索
                var flag = dfsMainLoop(p, target, tempPath, visited, ref rstPaths, fireHydrantSysIn, ref extraNodes);
                if (flag) return true;
                //删除不符合要求的点
                tempPath.RemoveAt(tempPath.Count - 1);
                visited.Remove(p);
            }

            return false;
        }

        //主环遍历时忽略掉的点
        private static bool IsOmitPt(Point3dEx p, Point3dEx cur, Point3dEx target, bool subLoopPoint, 
            HashSet<Point3dEx> visited, FireHydrantSystemIn fireHydrantSysIn)
        {
            if (fireHydrantSysIn.ThroughPt.Contains(p))
            {
                if (!p.Equals(target))
                {
                    return true;
                }
            }
            if (visited.Contains(p))//已经访问过
            {
                return true;
            }
            if (subLoopPoint)//次环点
            {
                if (PointCompute.IsSecondLoop(cur, p, fireHydrantSysIn.AngleList[cur]))
                {
                    return true;
                }
            }
            return false;
        }

        public static void DfsSubLoop(Point3dEx cur, List<Point3dEx> tempPath, HashSet<Point3dEx> visited, 
            ref List<List<Point3dEx>> rstPaths, Point3dEx target, FireHydrantSystemIn fireHydrantSysIn, Stopwatch stopwatch)
        {
            if(stopwatch.Elapsed.TotalSeconds > 20)//搜索了20s，可能死循环了
            {
                return;
            }
            if (cur._pt.DistanceTo(target._pt) < 5)
            {
                if(PointCompute.IsSecondLoop(cur, tempPath[tempPath.Count-2], fireHydrantSysIn.AngleList[target]))
                {
                    var rstPath = new List<Point3dEx>(tempPath);
                    var flag = true;
                    if (rstPaths.Count == 0)
                    {
                        rstPaths.Add(rstPath);
                    }
                    else
                    {
                        foreach (var tmpPath in rstPaths.ToArray())
                        {
                            if (tmpPath[0] == rstPath[0] && tmpPath.Last() == rstPath.Last())
                            {
                                if (tmpPath.Count < rstPath.Count)
                                {
                                    rstPaths.Remove(tmpPath);
                                    rstPaths.Add(rstPath);
                                    flag = false;
                                }
                                else
                                {
                                    flag = false;
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
            }
            var neighbors = fireHydrantSysIn.PtDic[cur];
            var subLoopPoint = false;
            var subStartPoint = false;
            foreach (var nd in fireHydrantSysIn.NodeList)
            {
                if (nd.Contains(cur))
                {
                    subLoopPoint = !nd.Contains(target);
                    subStartPoint = nd.Contains(target);
                    break;
                }
            }
            foreach (var p in neighbors)
            {
                if (visited.Contains(p))
                {
                    continue;
                }

                if (subLoopPoint)
                {
                    if (PointCompute.IsSecondLoop(cur, p, fireHydrantSysIn.AngleList[cur]))
                    {
                        continue;
                    }
                }
                if (subStartPoint)
                {
                    if (!PointCompute.IsSecondLoop(cur, p, fireHydrantSysIn.AngleList[cur]))
                    {
                        continue;
                    }
                }
                tempPath.Add(p);
                visited.Add(p);

                DfsSubLoop(p, tempPath, visited, ref rstPaths, target, fireHydrantSysIn, stopwatch);
                tempPath.RemoveAt(tempPath.Count - 1);
                visited.Remove(p);
            }
        }

        public static void dfsBranchLoop(Point3dEx cur, Point3dEx target, List<Point3dEx> tempPath, HashSet<Point3dEx> neverVisited, 
            HashSet<Point3dEx> visited, ref List<List<Point3dEx>> rstPaths, FireHydrantSystemIn fireHydrantSysIn)
        {
            if (cur.Equals(target))//找到目标点，返回最终路径
            {
                var rstPath = new List<Point3dEx>(tempPath);
                rstPaths.Add(rstPath);//把当前路径加入
                return;
            }

            var neighbors = fireHydrantSysIn.PtDic[cur];//当前点的邻接点
            foreach (Point3dEx p in neighbors)
            {
                if (neverVisited.Contains(p))//主环次环节点
                {
                    continue;
                }
                if (visited.Contains(p))//访问过
                {
                    continue;
                }
                tempPath.Add(p);
                visited.Add(p);

                //递归搜索
                dfsBranchLoop(p, target, tempPath, visited, neverVisited, ref rstPaths, fireHydrantSysIn);

                //删除不符合要求的点
                tempPath.RemoveAt(tempPath.Count - 1);
                visited.Remove(p);
            }
        }
    }
}
