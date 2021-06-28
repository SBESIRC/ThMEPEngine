using AcHelper.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using AcHelper;

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

        public void Execute1()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var tuplePoint = Common.Utils.SelectPoints();//范围框定
                var selectArea = ThFireHydrantSelectArea.CreateArea(tuplePoint);//生成候选区域
                var fireHydrantSysIn = new FireHydrantSystemIn();//输入参数
                var lineList = new List<Line>();//管段列表
                var pointList = new List<Point3dEx>();//点集
                var pipeEngine = new ThExtractHYDTPipeService();//提取供水管
                var dbObjs = pipeEngine.Extract(acadDatabase.Database, selectArea);
                var markEngine = new ThExtractPipeMark();//提取消火栓环管标记
                var mark = markEngine.Extract(acadDatabase.Database, selectArea);
                var pipeMarkSite = markEngine.GetPipeMarkPoisition();

               

                PipeLine.AddPipeLine(dbObjs, ref fireHydrantSysIn, ref pointList, ref lineList);
                foreach (var pms in pipeMarkSite)
                {
               
                    fireHydrantSysIn.markLine.Add(PointCompute.PointInLine(pms, lineList));
                }
                var opt = new PromptPointOptions("指定消火栓系统图插入点");
                var InsertPoint = Active.Editor.GetPoint(opt).Value;
                var visited = new HashSet<Point3dEx>();//访问标志
                ;
                var startPt = new Point3dEx(fireHydrantSysIn.markLine[0].StartPoint);//主环起始点
                var targetPt = new Point3dEx(fireHydrantSysIn.markLine[1].EndPoint);//主环终止点
                var tempPath = new List<Point3dEx>();//主环路临时路径
                var rstPath = new List<Point3dEx>();//主环路最终路径
                visited.Add(startPt);
                tempPath.Add(startPt);
               
                //主环路深度搜索
                DepthFirstSearch.dfsMainLoop(startPt, tempPath, visited, ref rstPath, targetPt, fireHydrantSysIn);
                //ThPointCountService.SetPointType(ref fireHydrantSysIn, rstPath);

                for (int i = 0; i < rstPath.Count - 1; i++)
                {
                    var spt = rstPath[i]._pt;
                    var ept = rstPath[i + 1]._pt;
                    var line1 = new Line(new Point3d(spt.X + InsertPoint.X, spt.Y + InsertPoint.Y, 0),
                        new Point3d(ept.X + InsertPoint.X, ept.Y + InsertPoint.Y, 0));
                    line1.LayerId = DbHelper.GetLayerId("W-FRPT-HYDT-PIPE");
                    acadDatabase.CurrentSpace.Add(line1);
                }
            }
        }
        public void Execute()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var fireHydrantSysIn = new FireHydrantSystemIn();//输入参数

                GetInput.GetFireHydrantSysInput(ref fireHydrantSysIn);//提取输入参数

                var visited = new HashSet<Point3dEx>();//访问标志

                var startPt = new Point3dEx(fireHydrantSysIn.markLine[0].StartPoint);//主环起始点
                var targetPt = new Point3dEx(fireHydrantSysIn.markLine[1].EndPoint);//主环终止点
                var tempPath = new List<Point3dEx>();//主环路临时路径
                var rstPath = new List<Point3dEx>();//主环路最终路径
                visited.Add(startPt);
                tempPath.Add(startPt);

                //主环路深度搜索
                DepthFirstSearch.dfsMainLoop(startPt, tempPath, visited, ref rstPath, targetPt, fireHydrantSysIn);
                ThPointCountService.SetPointType(ref fireHydrantSysIn, rstPath);

                var subPathList = new List<List<Point3dEx>>();//次环路最终路径 List

                foreach (var nd in fireHydrantSysIn.nodeList)
                {
                    var subStartPt = nd[0];//次环起始点
                    var subTargetPt = nd[1];//次环终止点
                    var subTempPath = new List<Point3dEx>();//次环路临时路径
                    var subRstPath = new List<Point3dEx>();//次环路临时路径

                    visited.Add(subStartPt);
                    subTempPath.Add(subStartPt);

                    //次环路深度搜索
                    DepthFirstSearch.dfsSubLoop(subStartPt, subTempPath, visited, ref subRstPath, subTargetPt, fireHydrantSysIn);
                    ThPointCountService.SetPointType(ref fireHydrantSysIn, subRstPath);
                    visited.Remove(visited.Last());//删除占用的点，避免干扰其他次环的遍历

                    subPathList.Add(subRstPath);
                }

                var branchDic = new Dictionary<Point3dEx, List<List<Point3dEx>>>();//支点 + 支点的支路列表
                foreach (var pt in rstPath)//遍历主环路的点
                {
                    if (fireHydrantSysIn.ptTypeDic[pt].Equals("Branch"))//是支点
                    {
                        var branchPath = new List<List<Point3dEx>>();
                        DepthFirstSearch.BranchSearch(pt, visited, ref branchPath, rstPath, fireHydrantSysIn);//支路遍历
                        branchDic.Add(pt, branchPath);
                    }
                }
                foreach(var ptls in subPathList)
                {
                    foreach (var pt in ptls)
                    {
                        if (fireHydrantSysIn.ptTypeDic[pt].Equals("Branch"))//是支点
                        {
                            var branchPath = new List<List<Point3dEx>>();
                            DepthFirstSearch.BranchSearch(pt, visited, ref branchPath, ptls, fireHydrantSysIn);//支路遍历
                            branchDic.Add(pt, branchPath);
                        }
                    }
                }
                
                var fireHydrantSysOut = new FireHydrantSystemOut();
                GetFireHydrantPipe.GetMainLoop(ref fireHydrantSysOut, rstPath, fireHydrantSysIn);//主环路绘制
                GetFireHydrantPipe.GetSubLoop(ref fireHydrantSysOut, subPathList, fireHydrantSysIn);//次环路绘制
                GetFireHydrantPipe.GetBranch(ref fireHydrantSysOut, branchDic, fireHydrantSysIn);
                
                fireHydrantSysOut.Draw();//绘制系统图
            }
        }
    }
}
