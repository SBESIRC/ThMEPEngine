using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using System;
using System.Collections.Generic;
using ThMEPEngineCore.Command;
using ThMEPWSS.UndergroundFireHydrantSystem.Command;
using ThMEPWSS.UndergroundFireHydrantSystem.Extract;
using ThMEPWSS.UndergroundFireHydrantSystem.Method;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.ViewModel;

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
            Point3d loopStartPt;
            {
                var opt = Active.Editor.GetPoint("\n请指定环管标记起点");
                if (opt.Status != PromptStatus.OK)
                {
                    return null;
                }
                var propPtRes = opt.Value;

                loopStartPt = propPtRes.TransformBy(Active.Editor.UCS2WCS());
            }

            var selectArea = Assistant.DrawUtils.TrySelectRangeEx();
            if (selectArea is null)
            {
                return null;
            }
            var storeyRect = new StoreyRect();
            storeyRect.Extract(selectArea);

            var fireHydrantSysIn = new FireHydrantSystemIn(_UiConfigs.SetViewModel.FloorLineSpace, storeyRect);
            var fireHydrantSysOut = new FireHydrantSystemOut();
            {
                var opt = Active.Editor.GetPoint("\n指定消火栓系统图插入点");
                if (opt.Status != PromptStatus.OK)
                {
                    return null;
                }
                fireHydrantSysOut.InsertPoint = opt.Value;
            }

            if (hasExtraSelection)
            {
                Point3dCollection exArea = Common.Utils.SelectAreas();
                if (exArea.Count == 0) return null;
                return exArea;
            }

            var inputFlag = GetInput.GetFireHydrantSysInput(curDb, fireHydrantSysIn, selectArea, loopStartPt);
            DbHelper.EnsureLayerOn("W-辅助");
            if (!inputFlag)
            {
                return null;
            }

            var mainPathList = MainLoop.Get(fireHydrantSysIn);
            if (mainPathList.Count == 0)
            {
                Active.Editor.WriteMessage("没找到主环");
                return null;
            }

            var subPathList = SubLoop.Get(fireHydrantSysIn, mainPathList);//支环提取
            var subPathLsCnt = subPathList.Count;
            var visited = new HashSet<Point3dEx>();//访问标志
            visited.AddVisit(mainPathList);
            visited.AddVisit(subPathList);

            var branchDic = new Dictionary<Point3dEx, List<Point3dEx>>();//支点 + 端点
            var ValveDic = new Dictionary<Point3dEx, List<ValveCasing>>();//支点 + 阀门点
            PtDic.CreateBranchDic(branchDic, ValveDic, mainPathList, fireHydrantSysIn, visited);
            PtDic.CreateBranchDic(branchDic, ValveDic, subPathList, fireHydrantSysIn, visited);

            var checkPipe = new CheckPipe(mainPathList, subPathList);
            checkPipe.DrawMainLoop(curDb);
            checkPipe.DrawSubLoop(curDb);
            checkPipe.DrawBranchLoop(curDb, fireHydrantSysIn, branchDic);

            var pepeLen = GetFireHydrantPipe.GetMainLoop(fireHydrantSysOut, mainPathList[0], fireHydrantSysIn, branchDic,false, ValveDic);//主环路获取

            var across = FireHydrantAcross.Cmd(curDb, fireHydrantSysIn, fireHydrantSysOut, pepeLen, subPathLsCnt);

            GetFireHydrantPipe.GetSubLoop(fireHydrantSysOut, subPathList, fireHydrantSysIn, branchDic, across,0,ValveDic);//次环路获取
            GetFireHydrantPipe.GetBranch(fireHydrantSysOut, branchDic, ValveDic, fireHydrantSysIn);//支路获取

            fireHydrantSysOut.Draw();//绘制系统图

            return null;
        }
    }
}
