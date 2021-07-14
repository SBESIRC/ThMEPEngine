using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    class DepthFirstSearch
    {
        public static void BranchSearch(Point3dEx startPt, HashSet<Point3dEx> visited, ref List<List<Point3dEx>> branchPath,
            List<Point3dEx> loopPath, FireHydrantSystemIn fireHydrantSysIn, List<Point3dEx> extraNodes)
        {
            const double DisToTerm = 150;
            var cur = startPt;
            
            var tempPath1 = new List<Point3dEx>();//存储第一条支链
            var tempPath2 = new List<Point3dEx>();//存储第2条支链
            var samePtLs = new List<Point3dEx>();
            var flag = false;
            var nextFlag = false;
            var breakFlag1 = false;
            while (true)
            {
                if(tempPath1.Count != 0)
                {
                    if (tempPath1.Last().Equals(cur))
                    {
                        break;
                    }
                }
                
                tempPath1.Add(cur);//路径添加当前点
                visited.Add(cur);//访问列表添加当前点
                
                foreach (var hp in fireHydrantSysIn.hydrantPosition)
                {
                    if (hp._pt.DistanceTo(cur._pt) < DisToTerm)//找到终点
                    {
                        branchPath.Add(tempPath1);
                        breakFlag1 = true;
                        break;
                    }
                    
                }
                if (breakFlag1)//路径1遍历结束
                {
                    break;
                }
                if (fireHydrantSysIn.ptTypeDic[cur].Equals("MainLoop") && fireHydrantSysIn.ptDic[cur].Count == 1)
                {
                    //是支路的边缘点且邻接点数目为1
                    branchPath.Add(tempPath1);
                    breakFlag1 = true;
                    break;
                }
                if(extraNodes.Contains(cur))
                {
                    //针对连通阀的点
                    breakFlag1 = true;
                    break;
                }
                
                if (cur.Equals(startPt))//当前点是起始点
                {
                    foreach (var pt in fireHydrantSysIn.ptDic[cur])
                    {
                        if(loopPath.Contains(pt))//若起始点的临近点是环路点，pass
                        {
                            continue;
                        }
                        if(visited.Contains(pt))//当前点已经遍历过
                        {
                            continue;
                        }
                        cur = pt;
                        nextFlag = true;//存在下一个点
                    }
                }
                else
                {
                    foreach (var pt in fireHydrantSysIn.ptDic[cur])
                    {
                        if (visited.Contains(pt))
                        {
                            continue;
                        }
                        cur = pt;
                        //visited.Add(pt);
                        break;
                    }
                }
                if (!cur.Equals(startPt) && fireHydrantSysIn.ptDic[cur].Count == 3)//支路上的分支点，分别保存
                {
                    foreach(var pt in tempPath1)
                    {
                        samePtLs.Add(pt);
                    }
                    flag = true;
                    samePtLs.Add(cur);
                }

                if(!nextFlag)
                {
                    return;
                }
            }

            if (samePtLs.Count != 0)
            {
                foreach (var pt in samePtLs)
                {
                    visited.Remove(pt);
                }
            }

            if (flag)
            {
                cur = startPt;
                while (true)
                {
                    if (tempPath2.Count != 0)
                    {
                        if (tempPath2.Last().Equals(cur))
                        {
                            break;
                        }
                    }
                    tempPath2.Add(cur);
                    visited.Add(cur);
                    
                    foreach (var hp in fireHydrantSysIn.hydrantPosition)
                    {
                        if (hp._pt.DistanceTo(cur._pt) < DisToTerm)
                        {
                            branchPath.Add(tempPath2);
                            return;
                        }
                    }
                    if (fireHydrantSysIn.ptTypeDic[cur].Equals("MainLoop") && fireHydrantSysIn.ptDic[cur].Count == 1)
                    {
                        branchPath.Add(tempPath2);
                        return;
                    }
                    if (extraNodes.Contains(cur))
                    {
                        return;
                    }

                    if (cur.Equals(startPt))
                    {
                        foreach (var pt in fireHydrantSysIn.ptDic[cur])
                        {
                            if (loopPath.Contains(pt))
                            {
                                continue;
                            }
                            if (visited.Contains(pt))
                            {
                                continue;
                            }
                            cur = pt;
                        }
                    }
                    else
                    {
                        foreach (var pt in fireHydrantSysIn.ptDic[cur])
                        {
                            if (visited.Contains(pt))
                            {
                                continue;
                            }
                            cur = pt;
                        }
                    }
                    
                }
            }
            return;
        }


        public static void dfsMainLoop(Point3dEx cur, List<Point3dEx> tempPath, HashSet<Point3dEx> visited, ref List<List<Point3dEx>> rstPaths, Point3dEx target,
             FireHydrantSystemIn fireHydrantSysIn, ref List<Point3dEx> extraNodes)
        {
            if(tempPath.Count==82)
            {
                ;
            }
            if (cur.Equals(target))//找到目标点，返回最终路径
            {
                var rstPath = new List<Point3dEx>(tempPath);
                var flag = true;
                if(rstPaths.Count == 0)//主环数为0
                {
                    rstPaths.Add(rstPath);//把当前路径加入
                }
                else//存在主环
                {
                    foreach(var tmpPath in rstPaths.ToArray())//主环遍历
                    {
                        if(tmpPath[0] == rstPath[0] && tmpPath.Last() == rstPath.Last())//存在同起点和终点的主环
                        {
                            if(tmpPath.Count < rstPath.Count)//取点数多的
                            {
                                rstPaths.Remove(tmpPath);
                                rstPaths.Add(rstPath);
                                flag = false;
                                foreach(var pt in tmpPath)//额外的点加入列表
                                {
                                    if(!rstPath.Contains(pt))
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
                    if(flag)
                    {
                        rstPaths.Add(rstPath);
                    }
                }
                return;
            }

            var neighbors = fireHydrantSysIn.ptDic[cur];//当前点的邻接点
            var subLoopPoint = false;//次环标志
            foreach (List<Point3dEx> nd in fireHydrantSysIn.nodeList)
            {
                ;
                if (nd.Contains(cur))
                { 
                    subLoopPoint = true;
                    break;
                }
                ;
            }
            foreach (Point3dEx p in neighbors)
            {
                if (visited.Contains(p))//已经访问过
                {
                    continue;
                }

                if (subLoopPoint)//次环点
                {
                    if (PointCompute.IsSecondLoop(cur, p, fireHydrantSysIn.angleList[cur]))
                    {
                        continue;
                    }
                }

                tempPath.Add(p);
                visited.Add(p);

                //递归搜索
                dfsMainLoop(p, tempPath, visited, ref rstPaths, target, fireHydrantSysIn, ref extraNodes);

                //删除不符合要求的点
                tempPath.RemoveAt(tempPath.Count - 1);
                visited.Remove(p);
            }
        }


        public static void dfsSubLoop(Point3dEx cur, List<Point3dEx> tempPath, HashSet<Point3dEx> visited, ref List<List<Point3dEx>> rstPaths, Point3dEx target,
            FireHydrantSystemIn fireHydrantSysIn)
        {
            if (cur.Equals(target))
            {
                if(PointCompute.IsSecondLoop(cur, tempPath[tempPath.Count-2], fireHydrantSysIn.angleList[cur]))
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
            var neighbors = fireHydrantSysIn.ptDic[cur];
            var subLoopPoint = false;
            var subStartPoint = false;
            foreach (var nd in fireHydrantSysIn.nodeList)
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
                    if (PointCompute.IsSecondLoop(cur, p, fireHydrantSysIn.angleList[cur]))
                    {
                        continue;
                    }
                }

                if (subStartPoint)
                {
                    if (!PointCompute.IsSecondLoop(cur, p, fireHydrantSysIn.angleList[cur]))
                    {
                        continue;
                    }
                }

                tempPath.Add(p);
                visited.Add(p);

                dfsSubLoop(p, tempPath, visited, ref rstPaths, target, fireHydrantSysIn);

                tempPath.RemoveAt(tempPath.Count - 1);
                visited.Remove(p);
            }
        }
    }
}
