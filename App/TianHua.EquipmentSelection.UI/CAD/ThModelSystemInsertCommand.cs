using System;
using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
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
                    //  同步风机模型
                    SyncModelData();
                }
                else if (dbManager.Models[_FanDataModel.ID].Count != _FanDataModel.VentQuan)
                {
                    // 场景2：若检测到图纸中有对应的风机图块，但图块数量不同
                    ThFanSelectionEngine.ReplaceModels(_FanDataModel);
                    // 同步风机模型
                    SyncModelData();
                }
                else if (dbManager.Models[_FanDataModel.ID].Count == _FanDataModel.VentQuan)
                {
                    // 场景3：若检测到图纸中有对应的风机图块，且图块数量相同
                    var models = dbManager.GetModels(_FanDataModel.ID);
                    // 所有风机图块由于相同的属性，这里随机选取一个风机图块作为这些图块的“代表”
                    var model = models[0];
                    if (ThFanSelectionEngine.IsModelStyleChanged(model, _FanDataModel))
                    {
                        // 风机形式变化
                        ThFanSelectionEngine.ReplaceModels(_FanDataModel);
                    }
                    else if (ThFanSelectionEngine.IsModelBlockNameChanged(model, _FanDataModel))
                    {
                        // 风机图块变化
                        ThFanSelectionEngine.ReplaceModelsInplace(_FanDataModel);
                    }
                    else
                    {
                        // 风机规格和型号变化
                        if (ThFanSelectionEngine.IsModelNameChanged(model, _FanDataModel))
                        {
                            ThFanSelectionEngine.ModifyModelNames(_FanDataModel);
                        }

                        // 风机编号变化
                        var numbers = dbManager.GetModelNumbers(_FanDataModel.ID);
                        if (!Enumerable.SequenceEqual(numbers.OrderBy(t => t), _FanDataModel.ListVentQuan.OrderBy(t => t)))
                        {
                            ThFanSelectionEngine.ModifyModelNumbers(_FanDataModel);
                        }

                        // 风机楼层变化
                        var storey = _FanDataModel.InstallFloor;
                        var modelNumber = model.GetModelNumber();
                        var storeyNumber = model.GetStoreyNumber();
                        if (storeyNumber != ThFanSelectionUtils.StoreyNumber(storey, modelNumber.ToString()))
                        {
                            ThFanSelectionEngine.ModifyModelNumbers(_FanDataModel);
                        }

                        // 参数变化
                        var blockReference = new ThBlockReferenceData(model);
                        var attributes = new Dictionary<string, string>(blockReference.Attributes);
                        if (_FanDataModel.IsAttributeModified(attributes))
                        {
                            // Workaround:
                            //  若“服务区域”发生变化，我们需要通过调整“翻转状态1”使其文字对齐
                            using (var ov = new ThModelRoateStateOverride(_FanDataModel))
                            {
                                ThFanSelectionEngine.ModifyModels(_FanDataModel);
                            }
                        }
                    }

                    // 同步风机模型
                    SyncModelData();

                    // 导航到图块
                    ThFanSelectionEngine.ZoomToModels(_FanDataModel);
                }
            }
        }

        public void SyncModelData()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var _FanDataModel = ThFanSelectionService.Instance.Model;
                var _SubFanDataModel = ThFanSelectionService.Instance.SubModel;
                if (_SubFanDataModel == null)
                {
                    acadDatabase.Database.AppendModelData(_FanDataModel);
                }
                else
                {
                    acadDatabase.Database.AppendModelData(_FanDataModel, _SubFanDataModel);
                }
            }
        }
    }
}
