using System;
using AcHelper;
using Linq2Acad;
using ThMEPWSS.UndergroundSpraySystem.Model;
using ThMEPWSS.UndergroundSpraySystem.Service;
using ThMEPWSS.UndergroundSpraySystem.ViewModel;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Command;

namespace ThMEPWSS.UndergroundSpraySystem.Command
{
    public class ThSpraySystemCmd : ThMEPBaseCommand, IDisposable
    {
        readonly SprayVM _UiConfigs;
        public ThSpraySystemCmd(SprayVM viewModel)
        {
            _UiConfigs = viewModel;
            CommandName = "THDXPLXTT";
            ActionName = "生成";
        }
        public void Dispose()
        {
        }
        public override void SubExecute()
        {
            try
            {
                using (var docLock = Active.Document.LockDocument())
                using (AcadDatabase currentDb = AcadDatabase.Active())
                {
                    CreateSpraySystem(currentDb);
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
        public void CreateSpraySystem(AcadDatabase curDb)
        {
            var startPt = SpraySys.GetInsertPt();//环管起始点
            if(startPt.Equals(new Point3d()))
            {
                return;
            }
            var selectArea = _UiConfigs.SelectedArea;//生成候选区域
            var sprayOut = new SprayOut();//输出参数
            var sprayIn = new SprayIn(_UiConfigs);//输入参数
            var spraySystem = new SpraySystem();

            if(!SpraySys.GetInput(curDb, sprayIn, selectArea, startPt))//提取输入参数
            {
                return;
            }
            SpraySys.Processing(curDb, sprayIn, spraySystem);
            SpraySys.GetOutput(sprayIn, spraySystem, sprayOut);
            sprayOut.Draw(curDb);
            ;
        }
    }
}
