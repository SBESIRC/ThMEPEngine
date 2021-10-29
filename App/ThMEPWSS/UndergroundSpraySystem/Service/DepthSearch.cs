using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    public class DepthSearch
    {
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
                if (visited.Contains(p)) continue;
                if(neverVisited.Contains(p)) continue;
                if (sprayIn.PtTypeDic[p].Contains("PressureValves")) continue;
                tempPath.Add(p);
                visited.Add(p);

                DfsMainLoop(p, tempPath, visited, ref rstPaths, sprayIn, ref extraNodes, neverVisited);
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
            if(!sprayIn.PtDic.ContainsKey(cur))
            {
                ;
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

        public static void DfsBranchLoop(Point3dEx cur, Point3dEx targetPt, List<Point3dEx> tempPath, ref HashSet<Point3dEx> visited,
           ref List<Point3dEx> rstPath, SprayIn sprayIn, ref bool flag, List<Point3dEx> pts)
        {
            if (cur.Equals(targetPt))//找到目标点，返回最终路径
            {
                //tempPath.Add(cur);
                rstPath.AddRange(tempPath);
                return;
            }
            
            var neighbors = sprayIn.PtDic[cur];//当前点的邻接点
            foreach (Point3dEx p in neighbors)
            {
                if (!p.Equals(targetPt) && visited.Contains(p)) continue;
                if (pts.Contains(p) && !p.Equals(targetPt)) 
                    continue;
                if (!sprayIn.PtTypeDic.ContainsKey(p))
                {
                    continue;
                }
                if (sprayIn.PtTypeDic[p].Contains("AlarmValve"))
                {
                    flag = true;
                }
                
                tempPath.Add(p);
                visited.Add(p);
                DfsBranchLoop(p, targetPt, tempPath, ref visited, ref rstPath, sprayIn, ref flag, pts);
                tempPath.RemoveAt(tempPath.Count - 1);
                visited.Remove(p);
            }
        }
    }
}
