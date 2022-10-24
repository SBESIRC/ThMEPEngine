using System;
using AcHelper;
using Linq2Acad;
using ThMEPEngineCore.Command;
using Autodesk.AutoCAD.Geometry;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.PressureDrainage.Model;
using ThMEPWSS.PressureDrainageSystem.Model;
using ThMEPWSS.PressureDrainageSystem.Service;
using System.Diagnostics;

namespace ThMEPWSS.Command
{
    public class ThUNDPDrainageSystemDiagramCmd : ThMEPBaseCommand, IDisposable
    {
        private readonly PressureDrainageSystemDiagramVieModel _vm;
        public Point3d InsertPt { get; set; }
        public bool Debug = false;
        public ThUNDPDrainageSystemDiagramCmd(PressureDrainageSystemDiagramVieModel pressureDrainageViewModel = null)
        {
            ActionName = "生成";
            CommandName = "THDXPSXTT";
            _vm = pressureDrainageViewModel;
        }
        public void Dispose()
        {
        }

        public override void SubExecute()
        {
            try
            {
                using (var Doclock = Active.Document.LockDocument())
                using (var curDb = AcadDatabase.Active())
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    //读取模型数据
                    PressureDrainageDataReader ModelReader = new PressureDrainageDataReader();
                    PressureDrainageModelData modeldata = ModelReader.GetPressureDrainageModelData(_vm);
                    modeldata.ConvertToWCS();
                    if (modeldata == null)
                    {
                        return;
                    }

                    //导入外部模块数据
                    ImportService blockImportService = new ImportService();
                    blockImportService.Import();

                    //生成系统图           
                    ThPressureDrainageSystemDiagram diagram = new ThPressureDrainageSystemDiagram(modeldata, Debug);
                    diagram.Init();
                    diagram.Draw(this.InsertPt);
                    sw.Stop();
                    Active.Editor.WriteLine($"计算完成，用时{sw.ElapsedMilliseconds / 1000}秒");
                }
            }
            catch (Exception ex)
            {
                Active.Editor.WriteLine(ex.Message);
            }
        }

        public override void AfterExecute()
        {
            base.AfterExecute();
            Active.Editor.WriteLine($"系统图绘制完成，用时{_stopwatch.Elapsed.TotalSeconds.ToString("0.00")}秒");
        }
    }
}