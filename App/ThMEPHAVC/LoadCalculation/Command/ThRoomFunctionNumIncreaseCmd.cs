using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore.Command;
using ThMEPHVAC.LoadCalculation.Model;
using ThMEPHVAC.LoadCalculation.Service;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace ThMEPHVAC.LoadCalculation.Command
{
    public class ThRoomFunctionNumIncreaseCmd : ThMEPBaseCommand, IDisposable
    {
        public ThRoomFunctionNumIncreaseCmd()
        {
            this.CommandName = "THFJDZ";
            this.ActionName = "房间编号递增";
        }
        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            using (var doclock = AcApp.DocumentManager.MdiActiveDocument.LockDocument())
            using (var database = AcadDatabase.Active())
            {
                //初始化
                int StartingNo;
                if (!int.TryParse(ThLoadCalculationUIService.Instance.Parameter.StartingNum, out StartingNo))
                {
                    return;
                }

                // 获取房间框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择房间功能块",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
                };
                var filterlist = OpFilter.Bulid(o =>
                o.Dxf((int)DxfCode.BlockName) == LoadCalculationParameterFromConfig.RoomFunctionBlockName &
                o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames));
                var result = Active.Editor.GetSelection(options, filterlist);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var blks = new List<BlockReference>();
                foreach (ObjectId objid in result.Value.GetObjectIds())
                {
                    blks.Add(database.Element<BlockReference>(objid));
                }

                LogicService logicService = new LogicService();
                int index = logicService.ChangeRoonFunctionBlk(blks, ThLoadCalculationUIService.Instance.Parameter.HasPrefix, ThLoadCalculationUIService.Instance.Parameter.PerfixContent, StartingNo);
                ThLoadCalculationUIService.Instance.Parameter.StartingNum = index.ToString("00");
            }
        }
    }
}
