﻿using Autodesk.AutoCAD.Geometry;
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
        public static void BranchDepthSearch(Point3dEx startPt, HashSet<Point3dEx> visited, List<Point3dEx> termPts,
            List<Point3dEx> tempPts, List<Point3dEx> valvePts, List<Point3dEx> loopPath, FireHydrantSystemIn fireHydrantSysIn)
        {
            var cur = startPt;
            var getNextPt = true;//拿到下个点置为false
            //foreach (var hp in fireHydrantSysIn.hydrantPosition)
            //{
                //if (hp._pt.DistanceTo(cur._pt) < DisToTerm)//找到终点
                //{
                //    termPts.Add(cur);
                //    foreach(var pt in tempPts)
                //    {
                //        BranchDepthSearch(startPt, ref visited, ref termPts, ref valvePts, loopPath, fireHydrantSysIn);
                //    }
                //    return;
                //}
            //}
            if(fireHydrantSysIn.ptDic[cur].Count == 1)
            {
                termPts.Add(cur);
                foreach (var pt in tempPts)
                {
                    BranchDepthSearch(pt, visited, termPts, tempPts, valvePts, loopPath, fireHydrantSysIn);

                }
                return;
            }
            foreach (var pt in fireHydrantSysIn.ptDic[cur])
            {
                if (loopPath.Contains(pt))//若起始点的临近点是环路点，pass
                {
                    continue;
                }
                if(visited.Contains(pt))//若该点被访问了
                {
                    continue;
                }
                if(getNextPt)
                {
                    cur = pt;
                    visited.Add(cur);
                    getNextPt = false;
                }
                else
                {
                    tempPts.Add(pt);
                }
            }
            BranchDepthSearch(cur, visited, termPts, tempPts, valvePts, loopPath, fireHydrantSysIn);
            return;
        }
        
        public static void dfsMainLoop(Point3dEx cur, List<Point3dEx> tempPath, HashSet<Point3dEx> visited, ref List<List<Point3dEx>> rstPaths, Point3dEx target,
             FireHydrantSystemIn fireHydrantSysIn, ref List<Point3dEx> extraNodes)
        {
            if(cur.Equals(new Point3dEx(0,0,0)))
            {
                return;
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
                if (nd.Contains(cur))
                { 
                    subLoopPoint = true;
                    break;
                }
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
