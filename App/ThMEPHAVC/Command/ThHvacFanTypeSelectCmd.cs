using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using GeometryExtensions;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThControlLibraryWPF.ControlUtils;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.EQPMFanSelect;
using ThMEPHVAC.ParameterService;

namespace ThMEPHVAC.Command
{
    public class ThHvacFanTypeSelectCmd : ThMEPBaseCommand, IDisposable
    {
        private string blockName = "";
        private string layerName = "";
        FanDataModel pFanModel;
        FanDataModel cFanModel;
        EQPMDocument eqpmDocument;
        public ThHvacFanTypeSelectCmd()
        {
            CommandName = "THFJXXCK";
            ActionName = "风机选型插入块";
            eqpmDocument = new EQPMDocument();
        }
        public override void SubExecute()
        {
            pFanModel = FanSelectTypeParameter.Instance.FanData;
            cFanModel = FanSelectTypeParameter.Instance.ChildFanData;
            using (Active.Document.LockDocument())
            using (AcadDatabase acdb = AcadDatabase.Active()) 
            {
                blockName = ThFanSelectionEngine.BlockLayerName(pFanModel, out layerName);
                LoadBlockLayerToDocument(acdb.Database);
            }
            var thisIdBlocks = eqpmDocument.GetDocumentFanBlocks(pFanModel);
            var needFanCount = pFanModel.ListVentQuan.Count;
            if (thisIdBlocks.Count == 0)
            {
                // 场景1：若检测到图纸中没有对应的风机图块
                //  插入新的图块
                ThFanSelectionEngine.InsertModels(pFanModel, cFanModel);
            }
            else if (thisIdBlocks.Count != needFanCount)
            {
                // 场景2：若检测到图纸中有对应的风机图块，但图块数量不同
                // 删除已经生成的块，重新进入插块模式
                using (AcadDatabase acadDatabase = AcadDatabase.Active())
                {
                    thisIdBlocks.ForEach(o =>
                    {
                        o.Id.RemoveModel();
                    });
                }
                ThFanSelectionEngine.InsertModels(pFanModel,cFanModel);
            }
            else if (thisIdBlocks.Count == needFanCount) 
            {
                // 场景3：若检测到图纸中有对应的风机图块，且图块数量相同
                var block = thisIdBlocks.First(); 
                var allModels = eqpmDocument.DocumentFanToFanModels(thisIdBlocks);
                var blockPModel = allModels.First();
                if (FanSelectCheck.IsModelStyleChanged(blockPModel, pFanModel))
                {
                    // 风机形式变化
                    // 删除已经生成的块，重新进入插块模式
                    using (AcadDatabase acadDatabase = AcadDatabase.Active())
                    {
                        thisIdBlocks.ForEach(o =>
                        {
                            o.Id.RemoveModel();
                        });
                    }
                    ThFanSelectionEngine.InsertModels(pFanModel, cFanModel);
                }
                else if (FanSelectCheck.IsModelBlockNameChanged(blockPModel, pFanModel))
                {
                    // 风机图块变化
                    ThFanSelectionEngine.ReplaceModelsInplace(thisIdBlocks,pFanModel,cFanModel);
                }
                else
                {
                    using (AcadDatabase acadDatabase = AcadDatabase.Active())
                    {
                        // 风机规格和型号变化
                        if (FanSelectCheck.IsModelNameChanged(block.Id, pFanModel))
                        {
                            //需要更新xdata，将xdata中的风机型号名称改掉
                            ThFanSelectionEngine.ModifyModelNames(thisIdBlocks, pFanModel);
                        }

                        // 风机编号变化

                        var numbers = blockPModel.ListVentQuan;
                        if (!Enumerable.SequenceEqual(numbers.OrderBy(t => t), pFanModel.ListVentQuan.OrderBy(t => t)))
                        {
                            ThFanSelectionEngine.ModifyModelNumbers(pFanModel, cFanModel);
                        }

                        // 风机楼层变化
                        var storey = blockPModel.InstallFloor;
                        var modelNumber = block.Id.GetModelNumber();
                        var storeyNumber = block.Id.GetStoreyNumber();
                        if (storeyNumber != EQPMFanCommon.StoreyNumber(storey, modelNumber.ToString()))
                        {
                            ThFanSelectionEngine.ModifyModelNumbers(pFanModel, cFanModel);
                        }

                        // 参数变化
                        var blockReference = new ThBlockReferenceData(block.Id);
                        var attributes = new Dictionary<string, string>(blockReference.Attributes);
                        if (FanSelectCheck.IsAttributeModified(pFanModel.Attributes(), attributes))
                        {
                            // Workaround:
                            //  若“服务区域”发生变化，我们需要通过调整“翻转状态1”使其文字对齐
                            var oldState = ThFanSelectionEngine.GetModelRotateState(thisIdBlocks);
                            ThFanSelectionEngine.ResetModelRotateState(thisIdBlocks);
                            ThFanSelectionEngine.SetModelRotateState(thisIdBlocks, oldState);
                            ThFanSelectionEngine.ModifyModels(thisIdBlocks, pFanModel.Attributes());
                        }
                    }
                    
                }
                thisIdBlocks = eqpmDocument.GetDocumentFanBlocks(pFanModel);
                // 导航到图块
                Active.Editor.ZoomToObjects(thisIdBlocks.ToArray(), 2.0);
                Active.Editor.PickFirstObjects(thisIdBlocks.Select(o => o.ObjectId).ToArray());
            }

        }
        public void LoadBlockLayerToDocument(Database database)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacModelDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(blockName), true);
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(layerName), true);
            }
        }
        
        private void SetModelNumber(ObjectId obj, string storey, int number)
        {
            obj.UpdateAttributesInBlock(new Dictionary<string, string>()
            {
                { EQPMFanCommon.BLOCK_ATTRIBUTE_STOREY_AND_NUMBER, EQPMFanCommon.StoreyNumber(storey, number.ToString()) }
            });
        }
        public void Dispose()
        {
        }
    }
}
