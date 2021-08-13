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
using ThMEPEngineCore.Command;

namespace ThMEPWSS.Command
{
    public class ThFireHydrantCmd : ThMEPBaseCommand, IDisposable
    {
        public ThFireHydrantCmd()
        {
            CommandName = "THDXXHSXTT";
        }
        public void Dispose()
        {
        }
        override public void SubExecute()
        {
            try
            {
                using (var docLock = Active.Document.LockDocument())
                using (AcadDatabase currentDb = AcadDatabase.Active())
                {
                    CreateFireHydrantSystem(currentDb);
                }
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }
        }
        public void CreateFireHydrantSystem(AcadDatabase curDb)
        {
            var opt = new PromptPointOptions("请指定环管标记起点: \n");
            var loopStartPt = Active.Editor.GetPoint(opt).Value;
            var selectArea = Common.Utils.SelectAreas();//生成候选区域
            var fireHydrantSysIn = new FireHydrantSystemIn();//输入参数
            var fireHydrantSysOut = new FireHydrantSystemOut();//输出参数

            GetInput.GetFireHydrantSysInput(curDb, ref fireHydrantSysIn, selectArea);//提取输入参数
            
            var mainPathList = new List<List<Point3dEx>>();//主环路最终路径
            var extraNodes = new List<Point3dEx>();//主环路连通阀点集
            var visited = new HashSet<Point3dEx>();//访问标志
            //var subPtList = new List<Point3dEx>();//按照主环遍历顺序存放次环节点
            foreach (var markLine in fireHydrantSysIn.markLineList)
            {
                var startPt = new Point3dEx(new Point3d());
                var targetPt = new Point3dEx(new Point3d());
                if (PtOnLine.PtIsOnLine(loopStartPt, markLine[0], 100))
                {
                    startPt = PtSet.SetStartEndPt(fireHydrantSysIn, markLine).First();
                    targetPt = PtSet.SetStartEndPt(fireHydrantSysIn, markLine).Last();
                }
                if(PtOnLine.PtIsOnLine(loopStartPt, markLine[1], 100))
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
            visited.Clear();
            PtSet.AddVisit(ref visited, mainPathList);
            PtSet.AddVisit(ref visited, subPathList);
            //using (AcadDatabase currentDb = AcadDatabase.Active())
            //{
            //    DrawPipe(currentDb, mainPathList);
            //    DrawPipe(currentDb, subPathList);
            //}
            
            var branchDic = new Dictionary<Point3dEx, List<Point3dEx>>();//支点 + 端点
            var ValveDic = new Dictionary<Point3dEx, List<Point3dEx>>();//支点 + 阀门点
            PtDic.CreateBranchDic(ref branchDic, ref ValveDic, mainPathList, fireHydrantSysIn, visited, extraNodes);
            PtDic.CreateBranchDic(ref branchDic, ref ValveDic, subPathList, fireHydrantSysIn, visited, extraNodes);

            GetFireHydrantPipe.GetMainLoop(ref fireHydrantSysOut, mainPathList, fireHydrantSysIn, branchDic);//主环路获取
            GetFireHydrantPipe.GetSubLoop(ref fireHydrantSysOut, subPathList, fireHydrantSysIn, branchDic);//次环路获取
            GetFireHydrantPipe.GetBranch(ref fireHydrantSysOut, branchDic, ValveDic, fireHydrantSysIn);//支路获取
            fireHydrantSysOut.Draw();//绘制系统图
        }

        public static void DrawPipe(AcadDatabase acadDatabase, List<List<Point3dEx>> pathList)
        {
            foreach (var path in pathList)
            {
                for (int i = 0; i < path.Count - 1; i++)
                {
                    var line = new Line(path[i]._pt, path[i + 1]._pt);
                    line.LayerId = DbHelper.GetLayerId("X-SHET-LOGK");
                    acadDatabase.CurrentSpace.Add(line);
                }
            }
        }
    }
}
