using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundSpraySystem.General;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    public static class DepthFirstSearch
    {
        public static void DfsMainLoop(Point3dEx cur, Point3dEx target, List<Point3dEx> tempPath, HashSet<Point3dEx> visited, 
            ref List<List<Point3dEx>> rstPaths, FireHydrantSystemIn fireHydrantSysIn, ref List<Point3dEx> extraNodes)
        {
            if (cur.Equals(target))
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
                                extraNodes.AddItems(tmpPath, rstPath);
                            }
                            else
                            {
                                extraNodes.AddItems(rstPath, tmpPath);
                            }
                            flag = false;
                            break;
                        }
                    }
                    if (flag)
                    {
                        rstPaths.Add(rstPath);
                    }
                }
                return;
            }
            var neighbors = fireHydrantSysIn.PtDic[cur];

            foreach (Point3dEx p in neighbors)
            {
                if (IsOmitPt(p, cur, target, visited, fireHydrantSysIn)) continue;
                tempPath.Add(p);
                visited.Add(p);

                using (AcadDatabase currentDb = AcadDatabase.Active())
                {
                    var line = new Line(p._pt, cur._pt)
                    {
                        Layer = "W-辅助"
                    };
                    currentDb.CurrentSpace.Add(line);
                }

                DfsMainLoop(p, target, tempPath, visited, ref rstPaths, fireHydrantSysIn, ref extraNodes);
                tempPath.RemoveAt(tempPath.Count - 1);
                visited.Remove(p);
            }
        }

        private static bool IsSubLoopPt(this Point3dEx cur,FireHydrantSystemIn fireHydrantSysIn)
        {
            foreach (List<Point3dEx> nd in fireHydrantSysIn.NodeList)
            {
                if (nd.Contains(cur))
                {
                    return true;
                }
            }
            return false;
        }

        //主环遍历时忽略掉的点
        private static bool IsOmitPt(Point3dEx p, Point3dEx cur, Point3dEx target,  
            HashSet<Point3dEx> visited, FireHydrantSystemIn fireHydrantSysIn)
        {
            if (fireHydrantSysIn.ThroughPt.Contains(p))
            {
                if (!p.Equals(target))
                {
                    return true;
                }
            }
            if (visited.Contains(p)) return true;
            
            if (cur.IsSubLoopPt(fireHydrantSysIn))//次环点
            {
                if (PointCompute.IsSecondLoop(cur, p, fireHydrantSysIn.AngleList[cur]))
                {
                    return true;
                }
            }
            else
            {
                if (fireHydrantSysIn.AngleList.ContainsKey(cur))//不是次环点
                {
                    if (!PointCompute.IsSecondLoop(cur, p, fireHydrantSysIn.AngleList[cur]))//不是主环方向
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static void DfsSubLoop(Point3dEx cur, List<Point3dEx> tempPath, HashSet<Point3dEx> visited, 
            ref List<List<Point3dEx>> rstPaths, Point3dEx target, FireHydrantSystemIn fireHydrantSysIn)
        {
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
                                flag = false;
                                if (tmpPath.Count < rstPath.Count)
                                {
                                    rstPaths.Remove(tmpPath);
                                    rstPaths.Add(rstPath);
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
                if (visited.Contains(p)) continue;
                bool isSecondLoop = false;
                if (fireHydrantSysIn.AngleList.ContainsKey(cur))
                    isSecondLoop = PointCompute.IsSecondLoop(cur, p, fireHydrantSysIn.AngleList[cur]);

                if (subLoopPoint&& isSecondLoop) continue;
                if (subStartPoint && !isSecondLoop) continue;

                tempPath.Add(p);
                visited.Add(p);

                DfsSubLoop(p, tempPath, visited, ref rstPaths, target, fireHydrantSysIn);
                tempPath.RemoveAt(tempPath.Count - 1);
                visited.Remove(p);
            }
        }

        public static void DfsBranchLoop(Point3dEx cur, Point3dEx target, List<Point3dEx> tempPath, HashSet<Point3dEx> neverVisited, 
            HashSet<Point3dEx> visited, ref List<List<Point3dEx>> rstPaths, FireHydrantSystemIn fireHydrantSysIn)
        {
            if (cur.Equals(target))
            {
                var rstPath = new List<Point3dEx>(tempPath);
                rstPaths.Add(rstPath);
                return;
            }

            var neighbors = fireHydrantSysIn.PtDic[cur];
            foreach (Point3dEx p in neighbors)
            {
                if (neverVisited.Contains(p)) continue;
                if (visited.Contains(p)) continue;
                
                tempPath.Add(p);
                visited.Add(p);

                DfsBranchLoop(p, target, tempPath, visited, neverVisited, ref rstPaths, fireHydrantSysIn);

                tempPath.RemoveAt(tempPath.Count - 1);
                visited.Remove(p);
            }
        }
    }
}
