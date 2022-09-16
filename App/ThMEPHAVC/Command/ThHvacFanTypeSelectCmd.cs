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
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var needFanCount = pFanModel.ListVentQuan.Count;
                var thisIdBlocks = eqpmDocument.GetDocumentFanBlocks(pFanModel);
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
                    foreach (ObjectId objId in thisIdBlocks)
                    {
                        objId.RemoveModel();
                    }
                    ThFanSelectionEngine.InsertModels(pFanModel, cFanModel);
                }
                else if (thisIdBlocks.Count == needFanCount)
                {
                    // 场景3：若检测到图纸中有对应的风机图块，且图块数量相同
                    var thisBlocks = thisIdBlocks
                        .OfType<ObjectId>()
                        .Select(o => acadDatabase.Element<BlockReference>(o))
                        .ToList();
                    var blockId = thisIdBlocks[0];
                    var allModels = eqpmDocument.DocumentFanToFanModels(thisIdBlocks);
                    var blockPModel = allModels.First();
                    if (FanSelectCheck.IsModelStyleChanged(blockPModel, pFanModel))
                    {
                        // 风机形式变化
                        // 删除已经生成的块，重新进入插块模式
                        foreach(ObjectId objId in thisIdBlocks)
                        {
                            objId.RemoveModel();
                        }
                        ThFanSelectionEngine.InsertModels(pFanModel, cFanModel);
                    }
                    else if (FanSelectCheck.IsModelBlockNameChanged(blockPModel, pFanModel))
                    {
                        // 风机图块变化
                        ThFanSelectionEngine.ReplaceModelsInplace(thisBlocks, pFanModel, cFanModel);
                    }
                    else
                    {
                        // 风机规格和型号变化
                        if (FanSelectCheck.IsModelNameChanged(blockId, pFanModel))
                        {
                            //需要更新xdata，将xdata中的风机型号名称改掉
                            ThFanSelectionEngine.ModifyModelNames(thisBlocks, pFanModel);
                        }

                        // 风机编号变化

                        var numbers = blockPModel.ListVentQuan;
                        if (!Enumerable.SequenceEqual(numbers.OrderBy(t => t), pFanModel.ListVentQuan.OrderBy(t => t)))
                        {
                            ThFanSelectionEngine.ModifyModelNumbers(pFanModel, cFanModel);
                        }

                        // 风机楼层变化
                        var storey = blockPModel.InstallFloor;
                        var modelNumber = blockId.GetModelNumber();
                        var storeyNumber = blockId.GetStoreyNumber();
                        if (storeyNumber != EQPMFanCommon.StoreyNumber(storey, modelNumber.ToString()))
                        {
                            ThFanSelectionEngine.ModifyModelNumbers(pFanModel, cFanModel);
                        }

                        // 参数变化
                        var blockReference = new ThBlockReferenceData(blockId);
                        var attributes = new Dictionary<string, string>(blockReference.Attributes);
                        if (FanSelectCheck.IsAttributeModified(pFanModel.Attributes(), attributes))
                        {
                            // Workaround:
                            //  若“服务区域”发生变化，我们需要通过调整“翻转状态1”使其文字对齐
                            var oldState = ThFanSelectionEngine.GetModelRotateState(thisBlocks);
                            ThFanSelectionEngine.ResetModelRotateState(thisBlocks);
                            ThFanSelectionEngine.SetModelRotateState(thisBlocks, oldState);
                            ThFanSelectionEngine.ModifyModels(thisBlocks, pFanModel.Attributes());
                        }
                    }

                    // 导航到图块
                    PickFirstModel(pFanModel);
                }
            }
        }

        private void PickFirstModel(FanDataModel dataModel)
        {
            var thisIdBlocks = eqpmDocument.GetDocumentFanBlocks(dataModel);
            if (thisIdBlocks.Count > 0)
            {
                Active.Editor.ZoomToObjects(thisIdBlocks, 2.0);
                Active.Editor.PickFirstObjects(thisIdBlocks.ToArray());
            }
        }

        public void LoadBlockLayerToDocument(Database database)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacModelDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(blockName), false);
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
