using Linq2Acad;
using AcHelper;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPWSS.UndergroundSpraySystem.Model;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.Method;
using Autodesk.AutoCAD.EditorInput;
using GeometryExtensions;
using System.Diagnostics;

namespace ThMEPWSS.UndergroundSpraySystem.Service
{
    class SpraySys
    {
        public static List<Point3dEx> GetStartPts()
        {
            var startPts = new List<Point3dEx>();
            Common.Utils.FocusMainWindow();
            using (Active.Document.LockDocument())
            {
                while (true)
                {
                    var opt = new PromptPointOptions("\n请指定报警阀后系统图起点");
                    opt.AllowNone = true;
                    var ptRst = Active.Editor.GetPoint(opt);
                    if(ptRst.Status == PromptStatus.None)
                    {
                        return startPts;
                    }
                    if (ptRst.Status == PromptStatus.Cancel)
                    {
                        return new List<Point3dEx>();
                    }
                    
                    var pt = ptRst.Value.TransformBy(Active.Editor.UCS2WCS());
                    startPts.Add(new Point3dEx(pt));
                    Active.Editor.WriteMessage("找到1个，总共" + startPts.Count.ToString() + "个");
                }
            }

        }

        public static bool GetPipeMarkPt(AcadDatabase acadDatabase, out Point3d insertPt)
        {
            insertPt = new Point3d();
            var database = acadDatabase.Database;
            var opt = new PromptPointOptions("\n请指定环管标记起点");
            var propRes = Active.Editor.GetPoint(opt);
            if (propRes.Status == PromptStatus.OK)
            {
                insertPt = propRes.Value.Point3dZ0().TransformBy(Active.Editor.UCS2WCS());
                var loopMarkPt = new LoopMarkPt();//环管标记点
                if (loopMarkPt.Extract(database, insertPt))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool GetInsertPoint(out Point3d insertPt)
        {
            insertPt = new Point3d();
            var opt = new PromptPointOptions("\n指定喷淋系统图插入点");
            var propPtRes = Active.Editor.GetPoint(opt);
            if (propPtRes.Status == PromptStatus.OK)
            {
                insertPt = propPtRes.Value;
                return true;
            }
            return false;
        }

        public static int Processing(AcadDatabase acadDatabase, SprayIn sprayIn, SpraySystem spraySystem)
        {
            var mainPathList = new List<List<Point3dEx>>();//主环路最终路径
            var extraNodes = new List<Point3dEx>();//主环路连通阀点集
            var visited = new HashSet<Point3dEx>();//访问标志
            var neverVisited = new HashSet<Point3dEx>();//访问标志
            var tempPath = new List<Point3dEx>();//主环路临时路径

            visited.Add(sprayIn.LoopStartPt);
            tempPath.Add(sprayIn.LoopStartPt);
            //主环路提取
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var mainPath = new List<Point3dEx>();
            Dfs.DfsMainLoopWithoutAlarmValve(sprayIn.LoopStartPt, tempPath, visited, mainPath, sprayIn);
            if(mainPath.Count == 0)
            {
                Dfs.DfsMainLoop(sprayIn.LoopStartPt, tempPath, visited, mainPathList, sprayIn, extraNodes, neverVisited);
                DicTools.SetPointType(sprayIn, mainPathList, extraNodes);
                spraySystem.MainLoop.AddRange(mainPathList[0]);

                if (LoopCheck.IsSingleLoop(spraySystem, sprayIn))//主环上存在报警阀
                {
                    foreach (var path in mainPathList)
                    {
                        var ls = new List<Point3dEx>();
                        foreach(var pt in path)
                        {
                            ls.Add(pt);
                        }
                        spraySystem.MainLoops.Add(ls);
                    }
                    BranchDeal2.Get(ref visited, sprayIn, spraySystem);

                    BranchDeal.GetThrough(ref visited, sprayIn, spraySystem);
                    return 2;
                }
            }
            mainPathList.Add(mainPath);
            spraySystem.MainLoop.AddRange(mainPathList[0]);
            DicTools.SetPointType(sprayIn, mainPathList);
            var hasSubLoop = SubLoopDeal.Get(ref visited, mainPathList, sprayIn, spraySystem);
            if(hasSubLoop)
            {
                BranchLoopDeal.Get(ref visited, sprayIn, spraySystem);
                SubLoopDeal.SetType(sprayIn, spraySystem);
                BranchDeal.Get(ref visited, sprayIn, spraySystem);
                BranchDeal.GetThrough(ref visited, sprayIn, spraySystem);
                return 1;
            }
            else
            {
                BranchLoopDeal.GetOnMainLoop(visited, sprayIn, spraySystem);
                BranchDeal.Get(ref visited, sprayIn, spraySystem);
                BranchDeal.GetThrough(ref visited, sprayIn, spraySystem);
                return 3;
            }
        }


        //主环-次环-支环-支路
        public static void GetOutput(SprayIn sprayIn, SpraySystem spraySystem, SprayOut sprayOut)
        {
            StoreyLine.Get(sprayOut, spraySystem, sprayIn);
            MainLoop.Get(sprayOut, spraySystem, sprayIn);
            SubLoop.Get(sprayOut, spraySystem, sprayIn);
            BranchLoop.Get(sprayOut, spraySystem, sprayIn);
            Branch.Get(sprayOut, spraySystem, sprayIn);
            PipeLine.Split(sprayOut);
        }

        //主环上面直接连支路
        public static void GetOutput2(SprayIn sprayIn, SpraySystem spraySystem, SprayOut sprayOut)
        {
            StoreyLine.Get(sprayOut, spraySystem, sprayIn);
            MainLoop2.Get(sprayOut, spraySystem, sprayIn);
            Branch.Get(sprayOut, spraySystem, sprayIn);
            StoreyLine.Get2(sprayOut, spraySystem, sprayIn);
            PipeLine.Split(sprayOut);
        }

        //无次环
        public static void GetOutput3(SprayIn sprayIn, SpraySystem spraySystem, SprayOut sprayOut)
        {
            StoreyLine.Get(sprayOut, spraySystem, sprayIn);
            MainLoop3.Get(sprayOut, spraySystem, sprayIn);
            BranchLoop.Get(sprayOut, spraySystem, sprayIn);
            Branch.Get(sprayOut, spraySystem, sprayIn);
            PipeLine.Split(sprayOut);
        }
    }
}
