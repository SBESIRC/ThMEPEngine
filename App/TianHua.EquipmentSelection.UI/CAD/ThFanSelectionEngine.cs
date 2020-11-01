using AcHelper;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using TianHua.FanSelection.Function;

namespace TianHua.FanSelection.UI.CAD
{
    public static class ThFanSelectionEngine
    {
        private static string CurrentModel { get; set; }
        private static int CurrentModelNumber { get; set; }

        public static void InsertModels(FanDataModel dataModel)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 选取插入点
                PromptPointResult pr = Active.Editor.GetPoint("\n请输入插入点");
                if (pr.Status != PromptStatus.OK)
                    return;

                // 若检测到图纸中没有对应的风机图块，则在鼠标的点击处插入风机
                var blockName = BlockName(dataModel);
                var layerName = BlockLayer(dataModel);
                Active.Database.ImportModel(blockName, layerName);
                var objId = Active.Database.InsertModel(blockName, layerName, dataModel.Attributes());
                var blockRef = acadDatabase.Element<BlockReference>(objId);
                var position = pr.Value - objId.GetModelBasePoint();
                for (int i = 0; i < dataModel.VentQuan; i++)
                {
                    double deltaX = blockRef.GeometricExtents.Width() * 2 * i;
                    Vector3d delta = new Vector3d(deltaX, 0.0, 0.0);
                    Matrix3d displacement = Matrix3d.Displacement(position + delta);
                    var model = acadDatabase.ModelSpace.Add(blockRef.GetTransformedCopy(displacement));
                    model.SetModelIdentifier(dataModel.ID, dataModel.ListVentQuan[i], dataModel.VentStyle);
                    model.SetModelNumber(dataModel.InstallFloor, dataModel.ListVentQuan[i]);
                    model.SetModelTextHeight();
                    UpdateModelName(model, dataModel);
                }

                // 删除初始图块
                blockRef.UpgradeOpen();
                blockRef.Erase();
            }
        }

        private static string BlockName(FanDataModel dataModel)
        {
            if (IsHTFCModel(dataModel))
            {
                return ThFanSelectionUtils.HTFCBlockName(
                    dataModel.VentStyle,
                    dataModel.IntakeForm,
                    dataModel.MountType);
            }
            else
            {
                return ThFanSelectionCommon.AXIAL_BLOCK_NAME;
            }
        }

        private static string BlockLayer(FanDataModel dataModel)
        {
            if (dataModel.Scenario == "消防排烟" || dataModel.Scenario == "消防加压送风" || dataModel.Scenario == "消防补风")
            {
                return ThFanSelectionCommon.BLOCK_LAYER_FIRE;
            }
            else if (dataModel.Scenario == "消防排烟兼平时排风" || dataModel.Scenario == "消防补风兼平时送风")
            {
                return ThFanSelectionCommon.BLOCK_LAYER_DUAL;
            }
            else
            {
                return ThFanSelectionCommon.BLOCK_LAYER_EQUP;
            }

        }

        public static void ReplaceModelsInplace(FanDataModel dataModel)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 导入新模型图块
                var blockName = BlockName(dataModel);
                var layerName = BlockLayer(dataModel);
                Active.Database.ImportModel(blockName, layerName);

                // 获取原模型对象
                var models = acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => o.ObjectId.IsModel(dataModel.ID))
                    .ToList();

                // 创建新模型
                foreach (var model in models)
                {
                    // 提取原属性
                    var block = new ThFSBlockReference(model.ObjectId);

                    // 插入新的图块
                    var objId = Active.Database.InsertModel(blockName, layerName, new Dictionary<string, string>(block.Attributes));
                    var blockRef = acadDatabase.Element<BlockReference>(objId, true);

                    // 写入原图元XData
                    objId.SetModelXDataFrom(model.ObjectId);

                    // 写入原图元属性
                    blockRef.SetPropertiesFrom(model);

                    // 写入原图元位置
                    blockRef.TransformBy(block.BlockTransform);

                    // 写入原图元自定义属性（动态属性）
                    objId.SetModelCustomPropertiesFrom(block.CustomProperties);

                    // 写入修改后的属性
                    objId.ModifyModelAttributes(dataModel.Attributes());

                    // 更新规格和型号
                    UpdateModelName(objId, dataModel);

                    // 删除原模型
                    model.UpgradeOpen();
                    model.Erase();
                }
            }
        }

        public static void RemoveModels(FanDataModel dataModel)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var models = acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => o.ObjectId.IsModel(dataModel.ID));
                foreach(var model in models)
                {
                    model.UpgradeOpen();
                    model.Erase();
                }
            }
        }

        public static void ModifyModels(FanDataModel dataModel)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var models = acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => o.ObjectId.IsModel(dataModel.ID));
                foreach (var model in models.Select((value, i) => new { i, value }))
                {
                    // 更新编号
                    int number = dataModel.ListVentQuan[model.i];
                    model.value.ObjectId.UpdateModelNumber(number);

                    // 更新属性值
                    model.value.ObjectId.ModifyModelAttributes(dataModel.Attributes());
                    model.value.ObjectId.SetModelNumber(dataModel.InstallFloor, number);
                }
            }
        }

        public static void ModifyModelNames(FanDataModel dataModel)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var models = acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => o.ObjectId.IsModel(dataModel.ID));
                foreach (var model in models.Select((value, i) => new { i, value }))
                {
                    // 更新风机型号
                    UpdateModelName(model.value.ObjectId, dataModel);
                }
            }
        }

        private static void UpdateModelName(ObjectId model, FanDataModel dataModel)
        {
            if (dataModel.VentStyle.Contains(ThFanSelectionCommon.AXIAL_TYPE_NAME))
            {
                model.SetModelName(ThFanSelectionUtils.AXIALModelName(dataModel.FanModelName, dataModel.MountType));
            }
            else
            {
                model.SetModelName(ThFanSelectionUtils.HTFCModelName(dataModel.VentStyle, dataModel.IntakeForm, dataModel.FanModelNum));
            }
        }

        public static bool IsHTFCModel(FanDataModel dataModel)
        {
            return ThFanSelectionUtils.IsHTFCModelStyle(dataModel.VentStyle);
        }

        public static bool IsModelStyleChanged(ObjectId model, FanDataModel dataModel)
        {
            return ThFanSelectionUtils.IsHTFCModelStyle(model.GetModelStyle()) ^ IsHTFCModel(dataModel);
        }

        public static bool IsModelNameChanged(ObjectId model, FanDataModel dataModel)
        {
            if (dataModel.VentStyle.Contains(ThFanSelectionCommon.AXIAL_TYPE_NAME))
            {
                return model.GetModelName() != ThFanSelectionUtils.AXIALModelName(dataModel.FanModelName, dataModel.MountType);
            }
            else
            {
                return model.GetModelName() != ThFanSelectionUtils.HTFCModelName(dataModel.VentStyle, dataModel.IntakeForm, dataModel.FanModelNum);
            }
        }

        public static bool IsModelBlockNameChanged(ObjectId model, FanDataModel dataModel)
        {
            return model.GetBlockName() != BlockName(dataModel);
        }

        public static void ZoomToModels(FanDataModel dataModel)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blockReferences = acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => o.ObjectId.IsModel(dataModel.ID));
                if (!blockReferences.Any())
                {
                    return;
                }
                if (CurrentModel == dataModel.ID)
                {
                    var models = blockReferences.Where(o => o.ObjectId.GetModelNumber() > CurrentModelNumber).ToList();
                    if (models.Count > 0)
                    {
                        // 找到第一个比当前编号大的图块
                        CurrentModelNumber = models[0].ObjectId.GetModelNumber();
                        Active.Editor.ZoomToModel(models[0].ObjectId, 3);
                        Active.Editor.PickFirstModel(models[0].ObjectId);
                    }
                    else
                    {
                        // 未找到一个比当前编号大的图块，回到第一个图块
                        CurrentModelNumber = blockReferences.First().ObjectId.GetModelNumber();
                        Active.Editor.ZoomToModel(blockReferences.First().ObjectId, 3);
                        Active.Editor.PickFirstModel(blockReferences.First().ObjectId);
                    }
                }
                else
                {
                    CurrentModel = dataModel.ID;
                    CurrentModelNumber = blockReferences.First().ObjectId.GetModelNumber();
                    Active.Editor.ZoomToModel(blockReferences.First().ObjectId, 3);
                    Active.Editor.PickFirstModel(blockReferences.First().ObjectId);
                }
            }
        }
    }
}
