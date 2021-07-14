using AcHelper.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using Linq2Acad;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.EditorInput;
using AcHelper;
using ThCADExtension;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Command
{
    class ThFireHydrantCmd : IAcadCommand, IDisposable
    {
        public ThFireHydrantCmd()
        {
        }
        public void Dispose()
        {
        }

        public void Execute()
        {
            var fireHydrantSysIn = new FireHydrantSystemIn();//输入参数

            GetInput.GetFireHydrantSysInput(ref fireHydrantSysIn);//提取输入参数
            
            var mainPathList = new List<List<Point3dEx>>();//主环路最终路径
            var extraNodes = new List<Point3dEx>();//主环路连通阀点集
            var visited = new HashSet<Point3dEx>();//访问标志

            foreach (var markLine in fireHydrantSysIn.markLineList)
            {
                var startPt = new Point3dEx(new Point3d());//主环起始点
                if (fireHydrantSysIn.ptDic[new Point3dEx(markLine[0].StartPoint)].Count == 1)
                {
                    startPt = new Point3dEx(markLine[0].StartPoint);
                }
                else
                {
                    startPt = new Point3dEx(markLine[0].EndPoint);
                }
                var targetPt = new Point3dEx(markLine[1].EndPoint);//主环终止点
                if (fireHydrantSysIn.ptDic[new Point3dEx(markLine[1].StartPoint)].Count == 1)
                {
                    targetPt = new Point3dEx(markLine[1].StartPoint);
                }
                else
                {
                    targetPt = new Point3dEx(markLine[1].EndPoint);
                }
                var tempPath = new List<Point3dEx>();//主环路临时路径
                visited.Add(startPt);
                tempPath.Add(startPt);

                //主环路深度搜索
                DepthFirstSearch.dfsMainLoop(startPt, tempPath, visited, ref mainPathList, targetPt, fireHydrantSysIn, ref extraNodes);
                ThPointCountService.SetPointType(ref fireHydrantSysIn, mainPathList);
            }

            using (var acadDatabase = AcadDatabase.Active())
            {
                foreach (var ptls in mainPathList)
                {
                    for (int i = 0; i < ptls.Count - 1; i++)
                    {
                        var pt1 = new Point3d(ptls[i]._pt.X, ptls[i]._pt.Y, 0);
                        var pt2 = new Point3d(ptls[i + 1]._pt.X, ptls[i + 1]._pt.Y, 0);
                        acadDatabase.CurrentSpace.Add(new Line(pt1, pt2));
                    }
                }
            }


            var subPathList = new List<List<Point3dEx>>();//次环路最终路径 List
            foreach (var nd in fireHydrantSysIn.nodeList)
            {
                var subStartPt = nd[0];//次环起始点
                var subTargetPt = nd[1];//次环终止点
                if (!fireHydrantSysIn.ptDic.ContainsKey(subStartPt))
                {
                    continue;
                }
                var subTempPath = new List<Point3dEx>();//次环路临时路径
                var subRstPath = new List<Point3dEx>();//次环路临时路径

                visited.Add(subStartPt);
                subTempPath.Add(subStartPt);

                //次环路深度搜索
                DepthFirstSearch.dfsSubLoop(subStartPt, subTempPath, visited, ref subPathList, subTargetPt, fireHydrantSysIn);
                visited.Remove(visited.Last());//删除占用的点，避免干扰其他次环的遍历
            }
            ThPointCountService.SetPointType(ref fireHydrantSysIn, subPathList);
            foreach (var ptls in mainPathList)
            {
                foreach (var pt in ptls)
                {
                    visited.Add(pt);
                }
            }
            foreach (var ptls in subPathList)
            {
                foreach (var pt in ptls)
                {
                    visited.Add(pt);
                }
            }


            //using (var acadDatabase = AcadDatabase.Active())
            //{
            //    foreach (var ptls in subPathList)
            //    {
            //        for (int i = 0; i < ptls.Count - 1; i++)
            //        {
            //            var pt1 = new Point3d(ptls[i]._pt.X, ptls[i]._pt.Y, 0);
            //            var pt2 = new Point3d(ptls[i + 1]._pt.X, ptls[i + 1]._pt.Y, 0);
            //            acadDatabase.CurrentSpace.Add(new Line(pt1, pt2));
            //        }
            //    }
            //}


            var branchDic = new Dictionary<Point3dEx, List<List<Point3dEx>>>();//支点 + 支点的支路列表
            var lonelyPt = new List<Point3dEx>();
            foreach (var rstPath in mainPathList)
            {
                foreach (var pt in rstPath)//遍历主环路的点
                {
                    if (fireHydrantSysIn.ptTypeDic[pt].Equals("Branch"))//是支点
                    {
                        var branchPath = new List<List<Point3dEx>>();
                        DepthFirstSearch.BranchSearch(pt, visited, ref branchPath, rstPath, fireHydrantSysIn, extraNodes);//支路遍历
                        
                        if (branchPath.Count != 0)
                        {
                            branchDic.Add(pt, branchPath);
                        }
                        else
                        {
                            ;
                            lonelyPt.Add(pt);

                        }

                    }
                }
            }

            foreach (var ptls in subPathList)
            {
                foreach (var pt in ptls)
                {
                    if (fireHydrantSysIn.ptTypeDic[pt].Equals("Branch"))//是支点
                    {
                        var branchPath = new List<List<Point3dEx>>();
                        DepthFirstSearch.BranchSearch(pt, visited, ref branchPath, ptls, fireHydrantSysIn, extraNodes);//支路遍历
                        if (branchDic.ContainsKey(pt))
                        {
                            continue;
                        }
                        if (branchPath.Count != 0)
                        {
                            branchDic.Add(pt, branchPath);
                        }
                        else
                        {
                            ;
                            lonelyPt.Add(pt);

                        }
                    }
                }
            }

            
            var fireHydrantSysOut = new FireHydrantSystemOut();
            GetFireHydrantPipe.GetMainLoop(ref fireHydrantSysOut, mainPathList, fireHydrantSysIn);//主环路获取
            GetFireHydrantPipe.GetSubLoop(ref fireHydrantSysOut, subPathList, fireHydrantSysIn);//次环路获取
            GetFireHydrantPipe.GetBranch(ref fireHydrantSysOut, branchDic, fireHydrantSysIn);//支路获取

            fireHydrantSysOut.Draw();//绘制系统图
            
        }
        
    }
}
