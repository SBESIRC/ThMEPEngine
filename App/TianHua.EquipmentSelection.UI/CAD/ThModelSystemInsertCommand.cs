﻿using System;
using AcHelper;
using Linq2Acad;
using ThCADExtension;
using AcHelper.Commands;
using System.Collections.Generic;
using TianHua.FanSelection.Function;
using ThMEPEngineCore.Service.Hvac;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThModelSystemInsertCommand : ThModelCommand, IDisposable 
    {
        public void Dispose()
        {
            //
        }

        public override void Execute()
        {
            using (Active.Document.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (ThHvacDbModelManager dbManager = new ThHvacDbModelManager(Active.Database))
            {
                // 切换焦点到CAD
                SetFocusToDwgView();

                // 获取风机参数
                var _FanDataModel = ThFanSelectionService.Instance.Model;
                if (_FanDataModel == null)
                {
                    return;
                }

                if (!dbManager.Contains(_FanDataModel.ID))
                {
                    // 场景1：若检测到图纸中没有对应的风机图块
                    //  插入新的图块
                    ThFanSelectionEngine.InsertModels(_FanDataModel);
                }
                else if (dbManager.Models[_FanDataModel.ID].Count != _FanDataModel.VentQuan)
                {
                    // 场景2：若检测到图纸中有对应的风机图块，但图块数量不同
                    ThFanSelectionEngine.RemoveModels(_FanDataModel);
                    ThFanSelectionEngine.InsertModels(_FanDataModel);
                }
                else if (dbManager.Models[_FanDataModel.ID].Count == _FanDataModel.VentQuan)
                {
                    // 场景3：若检测到图纸中有对应的风机图块，且图块数量相同
                    var models = dbManager.GetModels(_FanDataModel.ID);
                    // 所有风机图块由于相同的属性，这里随机选取一个风机图块作为这些图块的“代表”
                    var model = models[0];
                    // 风机形式变化
                    if (ThFanSelectionEngine.IsModelStyleChanged(model, _FanDataModel))
                    {
                        ThFanSelectionEngine.RemoveModels(_FanDataModel);
                        ThFanSelectionEngine.InsertModels(_FanDataModel);
                        return;
                    }

                    // 风机图块变化
                    if (ThFanSelectionEngine.IsModelBlockNameChanged(model, _FanDataModel))
                    {
                        ThFanSelectionEngine.ReplaceModelsInplace(_FanDataModel);
                        return;
                    }

                    // 风机规格和型号变化
                    bool bModified = false;
                    if (ThFanSelectionEngine.IsModelNameChanged(model, _FanDataModel))
                    {
                        bModified = true;
                        ThFanSelectionEngine.ModifyModelNames(_FanDataModel);
                    }

                    // 参数变化
                    var blockReference = new ThBlockReferenceData(model);
                    var attributes = new Dictionary<string, string>(blockReference.Attributes);
                    if (_FanDataModel.IsAttributeModified(attributes))
                    {
                        bModified = true;
                        ThFanSelectionEngine.ModifyModels(_FanDataModel);
                        ThFanSelectionEngine.ZoomToModels(_FanDataModel);
                    }

                    // 风机图块没有变化
                    if (!bModified)
                    {
                        ThFanSelectionEngine.ZoomToModels(_FanDataModel);
                    }
                }

                // 清除风机参数
                ThFanSelectionService.Instance.Model = null;
            }
        }
    }
}
