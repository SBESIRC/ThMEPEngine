using System;
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
            Active.Editor.WriteMessage($"seconds: {_stopwatch.Elapsed.TotalSeconds} \n");
        }

        public void CreateFireHydrantSystem(AcadDatabase curDb)
        {
            var opt = new PromptPointOptions("请指定环管标记起点: \n");
            var propPtRes = Active.Editor.GetPoint(opt);
            if (propPtRes.Status != PromptStatus.OK)
            {
                return;
            }

            var loopStartPt = propPtRes.Value.TransformBy(Active.Editor.UCS2WCS());
            var selectArea = Common.Utils.SelectAreas();//生成候选区域
            var fireHydrantSysIn = new FireHydrantSystemIn(_UiConfigs.SetViewModel.FloorLineSpace);//输入参数
            var fireHydrantSysOut = new FireHydrantSystemOut();//输出参数

            GetInput.GetFireHydrantSysInput(curDb, ref fireHydrantSysIn, selectArea, loopStartPt);//提取输入参数

            var mainPathList = MainLoop.Get(ref fireHydrantSysIn);//主环提取
            if (mainPathList.Count == 0)
            {
                return;
            }
            //for (int i = 0; i < mainPathList[0].Count - 1; i++)
            //{
            //    curDb.CurrentSpace.Add(new Line(mainPathList[0][i]._pt, mainPathList[0][i + 1]._pt));
            //}
            var subPathList = SubLoop.Get(ref fireHydrantSysIn, mainPathList);//支环提取

            var visited = new HashSet<Point3dEx>();//访问标志
            visited.AddVisit(mainPathList);
            visited.AddVisit(subPathList);
            
            var branchDic = new Dictionary<Point3dEx, List<Point3dEx>>();//支点 + 端点
            var ValveDic = new Dictionary<Point3dEx, List<Point3dEx>>();//支点 + 阀门点
            PtDic.CreateBranchDic(ref branchDic, ref ValveDic, mainPathList, fireHydrantSysIn, visited);
            PtDic.CreateBranchDic(ref branchDic, ref ValveDic, subPathList, fireHydrantSysIn, visited);

            GetFireHydrantPipe.GetMainLoop(ref fireHydrantSysOut, mainPathList[0], fireHydrantSysIn, branchDic);//主环路获取
            GetFireHydrantPipe.GetSubLoop(ref fireHydrantSysOut, subPathList, fireHydrantSysIn, branchDic);//次环路获取
            GetFireHydrantPipe.GetBranch(ref fireHydrantSysOut, branchDic, ValveDic, fireHydrantSysIn);//支路获取
            fireHydrantSysOut.Draw();//绘制系统图
        }
    }
}
