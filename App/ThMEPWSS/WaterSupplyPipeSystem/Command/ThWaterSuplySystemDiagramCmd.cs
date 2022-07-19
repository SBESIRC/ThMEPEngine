using AcHelper;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ThMEPEngineCore.Command;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.WaterSupplyPipeSystem;
using ThMEPWSS.WaterSupplyPipeSystem.model;
using ThMEPWSS.WaterSupplyPipeSystem.Method;
using ThMEPWSS.WaterSupplyPipeSystem.Data;
using ThMEPWSS.WaterSupplyPipeSystem.HalfFloorCase;
using ThMEPWSS.WaterSupplyPipeSystem.Command;

namespace ThMEPWSS.WaterSupplyPipeSystem.Command
{
    public class ThWaterSuplySystemDiagramCmd : ThMEPBaseCommand, IDisposable
    {
        readonly WaterSupplyVM _UiConfigs;
        Dictionary<string, List<string>> BlockConfig;

        public ThWaterSuplySystemDiagramCmd(WaterSupplyVM uiConfigs, Dictionary<string, List<string>> blockConfig)
        {
            _UiConfigs = uiConfigs;
            BlockConfig = blockConfig;
            CommandName = "THJSXTT";
            ActionName = "生成";
        }


        public void Dispose() { }


        public override void SubExecute()
        {
            try
            {
                if(_UiConfigs.SelectRadionButton.Content.IsNull())
                {
                    MessageBox.Show("不存在有效分组，请重新读取");
                    return;
                }
                if (_UiConfigs.SetViewModel.MeterType.Equals(WaterMeterLocation.RoofTank))
                {
                    WaterSupplyTankCmd.ExecuteTank(_UiConfigs, BlockConfig);
                }
                else
                {
                    WaterSupplyCmd.Execute(_UiConfigs, BlockConfig);
                }
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }
        }


        public override void AfterExecute()
        {
            Active.Editor.WriteLine($"seconds: {_stopwatch.Elapsed.TotalSeconds}");
            base.AfterExecute();
        }
    }
}
