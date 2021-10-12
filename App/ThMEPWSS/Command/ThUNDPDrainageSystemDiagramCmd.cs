using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Diagnostics;
using ThMEPEngineCore.Command;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.PressureDrainage.Model;
using ThMEPWSS.PressureDrainageSystem.Model;
using ThMEPWSS.PressureDrainageSystem.Service;

namespace ThMEPWSS.Command
{
    public class ThUNDPDrainageSystemDiagramCmd : ThMEPBaseCommand, IDisposable
    {
        readonly PressureDrainageSystemDiagramVieModel _pressureDrainageViewModel;
        public Point3d InsertPt { get; set; }
        public ThUNDPDrainageSystemDiagramCmd(PressureDrainageSystemDiagramVieModel pressureDrainageViewModel = null)
        {
            _pressureDrainageViewModel = pressureDrainageViewModel;
            this.CommandName = "THDXPSXTT";
            this.ActionName = "生成";
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
                    PressureDrainageModelData modeldata = ModelReader.GetPressureDrainageModelData(_pressureDrainageViewModel);
                    if (modeldata == null)
                    {
                        return;
                    }

                    //导入外部模块数据
                    ImportService blockImportService = new ImportService();
                    blockImportService.Import();

                    //生成系统图           
                    ThPressureDrainageSystemDiagram diagram = new ThPressureDrainageSystemDiagram(modeldata);
                    diagram.Init();
                    diagram.Draw(this.InsertPt);

                    sw.Stop();
                    Active.Editor.WriteMessage($"系统图绘制完成，用时{sw.Elapsed.TotalSeconds.ToString("0.00")}秒\n");
                }
            }
            catch (Exception ex)
            {           
                Active.Editor.WriteMessage(ex.Message + "\n");
            }
        }
    }
}