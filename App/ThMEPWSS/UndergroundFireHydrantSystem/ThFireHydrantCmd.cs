﻿using System;
using System.Collections.Generic;
using ThMEPWSS.ViewModel;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using Autodesk.AutoCAD.EditorInput;
using Linq2Acad;
using AcHelper;
using ThMEPEngineCore.Command;
using GeometryExtensions;
using ThMEPWSS.UndergroundFireHydrantSystem.Method;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPWSS.Hydrant.Engine;
using ThCADCore.NTS;
using ThMEPWSS.Uitl;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.UndergroundFireHydrantSystem.Extract;

namespace ThMEPWSS.Command
{
    public class ThFireHydrantCmd : ThMEPBaseCommand, IDisposable
    {
        readonly FireHydrantSystemViewModel _UiConfigs;
        public ThFireHydrantCmd(FireHydrantSystemViewModel uiConfigs)
        {
            _UiConfigs = uiConfigs;
            CommandName = "THDXXHSXTT";
            ActionName = "生成";
        }
        public void Dispose()
        {
        }
        public override void SubExecute()
        {
            try
            {
                Common.Utils.FocusMainWindow();
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

        public override void AfterExecute()
        {
            base.AfterExecute();
            Active.Editor.WriteMessage($"seconds: {_stopwatch.Elapsed.TotalSeconds} \n");
        }

        public Point3dCollection CreateFireHydrantSystem(AcadDatabase curDb, bool hasExtraSelection = false)
        {
            Autodesk.AutoCAD.Geometry.Point3d loopStartPt;
            {
                var opt = Active.Editor.GetPoint("\n请指定环管标记起点");
                if (opt.Status != PromptStatus.OK)
                {
                    return null;
                }
                var propPtRes = opt.Value;

                loopStartPt = propPtRes.TransformBy(Active.Editor.UCS2WCS());
            }

            var selectArea = Common.Utils.SelectAreas();//生成候选区域
            if (selectArea.Count == 0)
            {
                return null;
            }

            var fireHydrantSysIn = new FireHydrantSystemIn(_UiConfigs.SetViewModel.FloorLineSpace);//输入参数
            var fireHydrantSysOut = new FireHydrantSystemOut();//输出参数
            {
                var opt = Active.Editor.GetPoint("\n指定消火栓系统图插入点");

                if (opt.Status != PromptStatus.OK)
                {
                    return null;
                }
                fireHydrantSysOut.InsertPoint = opt.Value.TransformBy(Active.Editor.UCS2WCS());
            }
            if (hasExtraSelection)
            {
                Point3dCollection exArea = Common.Utils.SelectAreas();
                if (exArea.Count == 0) return null;
                return exArea;
            }
            GetInput.GetFireHydrantSysInput(curDb, ref fireHydrantSysIn, selectArea, loopStartPt);//提取输入参数

            var mainPathList = MainLoop.Get(ref fireHydrantSysIn);//主环提取
            if (mainPathList.Count == 0)
            {
                return null;
            }
            var subPathList = SubLoop.Get(ref fireHydrantSysIn, mainPathList);//支环提取

            var visited = new HashSet<Point3dEx>();//访问标志
            visited.AddVisit(mainPathList);
            visited.AddVisit(subPathList);

            var branchDic = new Dictionary<Point3dEx, List<Point3dEx>>();//支点 + 端点
            var ValveDic = new Dictionary<Point3dEx, List<Point3dEx>>();//支点 + 阀门点
            PtDic.CreateBranchDic(ref branchDic, ref ValveDic, mainPathList, fireHydrantSysIn, visited);
            PtDic.CreateBranchDic(ref branchDic, ref ValveDic, subPathList, fireHydrantSysIn, visited);

            var checkPipe = new CheckPipe(mainPathList, subPathList);
            checkPipe.DrawMainLoop(curDb);
            checkPipe.DrawSubLoop(curDb);
            checkPipe.DrawBranchLoop(curDb, fireHydrantSysIn, branchDic);

            GetFireHydrantPipe.GetMainLoop(ref fireHydrantSysOut, mainPathList[0], fireHydrantSysIn, branchDic);//主环路获取
            GetFireHydrantPipe.GetSubLoop(ref fireHydrantSysOut, subPathList, fireHydrantSysIn, branchDic);//次环路获取
            GetFireHydrantPipe.GetBranch(ref fireHydrantSysOut, branchDic, ValveDic, fireHydrantSysIn);//支路获取
            fireHydrantSysOut.Draw();//绘制系统图

            return null;
        }

        public void Test()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //var entOpt = new PromptEntityOptions("\nPick entity in block:");
                //var entityResult = Active.Editor.GetEntity(entOpt);

                //var entId = entityResult.ObjectId;
                //var dbObj = acadDatabase.Element<Entity>(entId);

                var selectArea = Common.Utils.SelectAreas();//生成候选区域
                if (selectArea.Count == 0)
                {
                    return;
                }
                var labelEngine = new ThExtractLabelLine();//提取消火栓标记线
                var labelDB = labelEngine.Extract(acadDatabase.Database, selectArea);
                var labelLine = labelEngine.CreateLabelLineList(labelDB);

            }

        }


       

    }
}
