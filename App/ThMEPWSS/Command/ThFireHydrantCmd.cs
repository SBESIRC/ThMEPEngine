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

namespace ThMEPWSS.Command
{
    public class ThFireHydrantCmd : IAcadCommand, IDisposable
    {
        public ThFireHydrantCmd()
        {
        }
        public void Dispose()
        {
        }

        public void Execute()
        {
            var opt = new PromptPointOptions("请指定环管标记起点");
            var loopStartPt = Active.Editor.GetPoint(opt).Value;

            var tuplePoint = Common.Utils.SelectPoints();//范围框定
            var selectArea = ThFireHydrantSelectArea.CreateArea(tuplePoint);//生成候选区域

            var fireHydrantSysIn = new FireHydrantSystemIn();//输入参数
            var fireHydrantSysOut = new FireHydrantSystemOut();//输出参数

            GetInput.GetFireHydrantSysInput(ref fireHydrantSysIn, selectArea);//提取输入参数
            
            var mainPathList = new List<List<Point3dEx>>();//主环路最终路径
            var extraNodes = new List<Point3dEx>();//主环路连通阀点集
            var visited = new HashSet<Point3dEx>();//访问标志
            var subPtList = new List<Point3dEx>();//按照主环遍历顺序存放次环节点
            foreach (var markLine in fireHydrantSysIn.markLineList)
            {
                var startPt = new Point3dEx(new Point3d());
                var targetPt = new Point3dEx(new Point3d());
                if (PtOnLine.PtIsOnLine(loopStartPt, markLine[0]))
                {
                    startPt = PtSet.SetStartEndPt(fireHydrantSysIn, markLine).First();
                    targetPt = PtSet.SetStartEndPt(fireHydrantSysIn, markLine).Last();
                }
                if(PtOnLine.PtIsOnLine(loopStartPt, markLine[1]))
                {
                    startPt = PtSet.SetStartEndPt(fireHydrantSysIn, markLine).Last();
                    targetPt = PtSet.SetStartEndPt(fireHydrantSysIn, markLine).First();
                }
                if(startPt.Equals(new Point3dEx(0, 0, 0)))
                {
                    continue;
                }
                var tempPath = new List<Point3dEx>();//主环路临时路径
                visited.Add(startPt);
                tempPath.Add(startPt);

                //主环路深度搜索
                DepthFirstSearch.dfsMainLoop(startPt, tempPath, visited, ref mainPathList, targetPt, fireHydrantSysIn, ref extraNodes);
                //ThPointCountService.SetPointType(ref fireHydrantSysIn, ref subPtList, mainPathList);
                ThPointCountService.SetPointType(ref fireHydrantSysIn, mainPathList);
            }
            
            if(mainPathList.Count == 0)
            {
                return;
            }

            var subPathList = new List<List<Point3dEx>>();//次环路最终路径 List
            
            foreach (var nd in fireHydrantSysIn.nodeList)
            {
                if (!fireHydrantSysIn.ptDic.ContainsKey(nd[0]))
                {
                    continue;
                }
                if(!mainPathList.First().Contains(nd[0]) && !mainPathList.First().Contains(nd[1]))
                {
                    continue;
                }
                var subTempPath = new List<Point3dEx>();//次环路临时路径
                var subRstPath = new List<Point3dEx>();//次环路临时路径

                visited.Add(nd[0]);
                subTempPath.Add(nd[0]);

                //次环路深度搜索
                DepthFirstSearch.dfsSubLoop(nd[0], subTempPath, visited, ref subPathList, nd[1], fireHydrantSysIn);
                visited.Remove(visited.Last());//删除占用的点，避免干扰其他次环的遍历
            }

            ThPointCountService.SetPointType(ref fireHydrantSysIn, subPathList);
            PtSet.AddVisit(ref visited, mainPathList);
            PtSet.AddVisit(ref visited, subPathList);

            var branchDic = new Dictionary<Point3dEx, List<List<Point3dEx>>>();//支点 + 支点的支路列表
            PtDic.CreateBranchDic(ref branchDic, mainPathList, fireHydrantSysIn, visited, extraNodes);
            PtDic.CreateBranchDic(ref branchDic, subPathList, fireHydrantSysIn, visited, extraNodes);

            
            GetFireHydrantPipe.GetMainLoop(ref fireHydrantSysOut, mainPathList, fireHydrantSysIn);//主环路获取
            GetFireHydrantPipe.GetSubLoop(ref fireHydrantSysOut, subPathList, fireHydrantSysIn);//次环路获取
            GetFireHydrantPipe.GetBranch(ref fireHydrantSysOut, branchDic, fireHydrantSysIn);//支路获取

            fireHydrantSysOut.Draw();//绘制系统图   
        }
    }
}
