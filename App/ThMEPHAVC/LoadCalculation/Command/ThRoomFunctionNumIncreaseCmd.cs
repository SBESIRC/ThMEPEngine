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
            this.CommandName = "THFJBH";
            this.ActionName = "房间编号递增";
        }
        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            //初始化
            int StartingNo;
            if (!int.TryParse(ThLoadCalculationUIService.Instance.Parameter.StartingNum, out StartingNo))
            {
                return;
            }
            LogicService logicService = new LogicService();
            //初始化图纸(导入图层/图块/图层三板斧等)
            InsertBlockService.initialization();
            while (true)
            {
                PromptEntityOptions op = new PromptEntityOptions("\n逐个点选房间功能图块");
                op.Keywords.Add("批量修改", "A", "批量修改(A)");
                op.Keywords.Default = "批量修改";
                var ner = Active.Editor.GetEntity(op);
                if (ner.Status == PromptStatus.OK)
                {
                    using (var doclock = AcApp.DocumentManager.MdiActiveDocument.LockDocument())
                    using (var database = AcadDatabase.Active())
                    {
                        var blks = new List<BlockReference>();
                        var entity = database.Element<Entity>(ner.ObjectId);
                        if (entity is BlockReference blk && (blk.GetEffectiveName() == LoadCalculationParameterFromConfig.RoomFunctionBlockName || blk.GetEffectiveName() == LoadCalculationParameterFromConfig.RoomFunctionBlockName_New))
                        {
                            blks.Add(blk);
                        }
                        int index = logicService.ChangeRoonFunctionBlk(blks, ThLoadCalculationUIService.Instance.Parameter.HasPrefix, ThLoadCalculationUIService.Instance.Parameter.PerfixContent, StartingNo++);
                        ThLoadCalculationUIService.Instance.Parameter.StartingNum = index.ToString("00");
                    }
                }
                else if(ner.Status==PromptStatus.Keyword)
                {
                    if(ner.StringResult== "批量修改")
                    {
                        using (var doclock = AcApp.DocumentManager.MdiActiveDocument.LockDocument())
                        using (var database = AcadDatabase.Active())
                        {
                            // 获取房间框线
                            PromptSelectionOptions options2 = new PromptSelectionOptions()
                            {
                                AllowDuplicates = false,
                                MessageForAdding = "请批量选择房间功能图块",
                                RejectObjectsOnLockedLayers = true,
                            };
                            var dxfNames = new string[]
                            {
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
                            };
                            var filterlist = OpFilter.Bulid(o =>
                            (o.Dxf((int)DxfCode.BlockName) == LoadCalculationParameterFromConfig.RoomFunctionBlockName |
                            o.Dxf((int)DxfCode.BlockName) == LoadCalculationParameterFromConfig.RoomFunctionBlockName_New) &
                            o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames));
                            var result2 = Active.Editor.GetSelection(options2, filterlist);
                            if (result2.Status != PromptStatus.OK)
                            {
                                return;
                            }
                            var blks = new List<BlockReference>();
                            foreach (ObjectId objid in result2.Value.GetObjectIds())
                            {
                                blks.Add(database.Element<BlockReference>(objid));
                            }
                            int index = logicService.ChangeRoonFunctionBlk(blks, ThLoadCalculationUIService.Instance.Parameter.HasPrefix, ThLoadCalculationUIService.Instance.Parameter.PerfixContent, StartingNo);
                            ThLoadCalculationUIService.Instance.Parameter.StartingNum = index.ToString("00");
                        }
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }
}
