using System;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using TianHua.Publics.BaseCode;
using TianHua.FanSelection.Function;
using ThMEPEngineCore.Service.Hvac;

namespace TianHua.FanSelection.UI.CAD
{
    public static class ThFanSelectionEngine
    {
        public static void InsertModels(FanDataModel dataModel)
        {
            var pr = Active.Editor.GetPoint("\n请输入插入点");
            if (pr.Status == PromptStatus.OK)
            {
                for (int i = 0; i < dataModel.VentQuan; i++)
                {
                    var number = dataModel.ListVentQuan[i];
                    // 以指定点作为起始点（UCS），沿着X轴方向间隔5000放置图块
                    var insertPt = pr.Value + Vector3d.XAxis * 5000 * i;
                    var position = insertPt.TransformBy(Active.Editor.UCS2WCS());
                    InsertModel(dataModel, number, position);
                }
            }
        }

        public static ObjectId InsertModel(FanDataModel dataModel, int number, Point3d pt)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blockName = dataModel.BlockName();
                var layerName = dataModel.BlockLayer();
                Active.Database.ImportModel(blockName, layerName);
                var objId = Active.Database.InsertModel(blockName, layerName, dataModel.Attributes());
                objId.SetModelIdentifier(dataModel.ID, FuncStr.NullToInt(number), dataModel.VentStyle, dataModel.Scenario);
                objId.SetModelNumber(dataModel.InstallFloor, FuncStr.NullToInt(number));
                objId.SetModelTextHeight();
                UpdateModelName(objId, dataModel);

                // 设置风机图块位置
                var blockRef = acadDatabase.Element<BlockReference>(objId, true);
                blockRef.TransformBy(Matrix3d.Displacement(pt - objId.GetModelBasePoint()));

                // 返回风机图块
                return objId;
            }
        }

        public static void EraseModels(FanDataModel dataModel, bool erasing = true)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (ThHvacDbModelManager dbManager = new ThHvacDbModelManager(Active.Database, true))
            {
                dbManager.EraseModels(dataModel.ID, erasing);
            }
        }

        public static void RemoveModels(FanDataModel dataModel)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (ThHvacDbModelManager dbManager = new ThHvacDbModelManager(Active.Database))
            {
                dbManager.RemoveModels(dataModel.ID);
            }
        }

        public static void ReplaceModels(FanDataModel dataModel)
        {
            RemoveModels(dataModel);
            InsertModels(dataModel);
        }

        public static void CloneModels(FanDataModel targetDataModel, FanDataModel srcDataModel)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (ThHvacDbModelManager dbManager = new ThHvacDbModelManager(Active.Database, true))
            {
                // 计算源风机系统和目标风机系统的偏移量
                var offset = new Vector3d(0,0,0);
                for (int i = 0; i < srcDataModel.VentQuan; i++)
                {
                    var number = srcDataModel.ListVentQuan[i];
                    var srcObjId = dbManager.GetModel(srcDataModel.ID, number);
                    var targetObjId = dbManager.GetModel(targetDataModel.ID, number);
                    if (srcObjId.IsValid && targetObjId.IsValid)
                    {
                        var p1 = acadDatabase.Element<BlockReference>(srcObjId).Position;
                        var p2 = acadDatabase.Element<BlockReference>(targetObjId).Position;
                        offset = p2 - p1;
                        break;
                    }
                }

                // 更新目标风机系统内已经存在的风机
                EditModelsInplace(targetDataModel);

                // 复制源风机系统内其他风机
                for (int i = 0; i < targetDataModel.VentQuan; i++)
                {
                    var number = targetDataModel.ListVentQuan[i];
                    var srcObjId = dbManager.GetModel(srcDataModel.ID, number);
                    var targetObjId = dbManager.GetModel(targetDataModel.ID, number);
                    if (srcObjId.IsValid && targetObjId.IsNull)
                    {
                        var model = acadDatabase.Element<BlockReference>(srcObjId);
                        var pt = model.Position + srcObjId.GetModelBasePoint().GetAsVector();
                        targetObjId = InsertModel(targetDataModel, number, pt + offset);
                    }
                    if (srcObjId.IsValid && targetObjId.IsValid)
                    {
                        // 写入原图元属性
                        var srcModel = acadDatabase.Element<BlockReference>(srcObjId);
                        var targetModel = acadDatabase.Element<BlockReference>(targetObjId);
                        targetModel.SetPropertiesFrom(srcModel);

                        // 写入原图元自定义属性（动态属性）
                        var block = new ThBlockReferenceData(srcObjId);
                        targetObjId.SetModelCustomPropertiesFrom(block.CustomProperties);
                    }
                }
            }
        }

        public static void ReplaceModelsInplace(FanDataModel dataModel)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 导入新模型图块
                var blockName = dataModel.BlockName();
                var layerName = dataModel.BlockLayer(); 
                Active.Database.ImportModel(blockName, layerName);

                // 获取原模型对象
                var identifier = dataModel.ID;
                var models = acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => !o.BlockTableRecord.IsNull)
                    .Where(o => o.ObjectId.IsModel(identifier, ThHvacCommon.RegAppName_FanSelection))
                    .ToList();

                // 创建新模型
                var newModels = new ObjectIdCollection();
                foreach (var model in models)
                {
                    // 提取原属性
                    var block = new ThBlockReferenceData(model.ObjectId);

                    // 插入新的图块
                    var objId = Active.Database.InsertModel(blockName, layerName, new Dictionary<string, string>(block.Attributes));
                    var blockRef = acadDatabase.Element<BlockReference>(objId, true);
                    newModels.Add(objId);

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
                    objId.SetModelNumber(dataModel.InstallFloor, model.ObjectId.GetModelNumber());

                    // 更新规格和型号
                    UpdateModelName(objId, dataModel);

                    // 删除原模型
                    model.UpgradeOpen();
                    model.Erase();
                }
            }
        }

        public static void ResetModelRotateState(FanDataModel dataModel)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 获取原模型对象
                var identifier = dataModel.ID;
                var models = acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => !o.BlockTableRecord.IsNull)
                    .Where(o => o.ObjectId.IsModel(identifier, ThHvacCommon.RegAppName_FanSelection))
                    .ToList();

                // 更新模型
                foreach (var model in models)
                {
                    model.UpgradeOpen();
                    model.ObjectId.SetModelRotateState((short)0);
                    model.DowngradeOpen();
                }
            }
        }

        public static void SetModelRotateState(FanDataModel dataModel, Dictionary<ObjectId, short> states)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 获取原模型对象
                var identifier = dataModel.ID;
                var models = acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => !o.BlockTableRecord.IsNull)
                    .Where(o => o.ObjectId.IsModel(identifier, ThHvacCommon.RegAppName_FanSelection))
                    .ToList();

                // 更新模型
                foreach (var model in models)
                {
                    model.UpgradeOpen();
                    model.ObjectId.SetModelRotateState(states[model.ObjectId]);
                    model.DowngradeOpen();
                }
            }
        }

        public static Dictionary<ObjectId, short> GetModelRotateState(FanDataModel dataModel)
        {
            var states = new Dictionary<ObjectId, short>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 获取原模型对象
                var identifier = dataModel.ID;
                var models = acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => !o.BlockTableRecord.IsNull)
                    .Where(o => o.ObjectId.IsModel(identifier, ThHvacCommon.RegAppName_FanSelection))
                    .ToList();

                // 获取数据
                foreach (var model in models)
                {
                    states.Add(model.ObjectId, model.ObjectId.GetModelRotateState());
                }

                // 返回数据
                return states;
            }
        }

        public static void EditModelsInplace(FanDataModel dataModel)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 获取原模型对象
                var models = acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => !o.BlockTableRecord.IsNull)
                    .Where(o => o.ObjectId.IsModel(dataModel.ID, ThHvacCommon.RegAppName_FanSelection))
                    .ToList();

                // 更新模型
                foreach (var model in models)
                {
                    var number = model.ObjectId.GetModelNumber();
                    if (dataModel.ListVentQuan.Contains(number))
                    {
                        // 写入修改后的属性
                        model.ObjectId.ModifyModelAttributes(dataModel.Attributes());
                        model.ObjectId.SetModelNumber(dataModel.InstallFloor, number);

                        // 更新规格和型号
                        UpdateModelName(model.ObjectId, dataModel);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
            }
        }

        public static void ModifyModels(FanDataModel dataModel)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => !o.BlockTableRecord.IsNull)
                    .Where(o => o.ObjectId.IsModel(dataModel.ID, ThHvacCommon.RegAppName_FanSelection))
                    .ForEach(o => o.ObjectId.ModifyModelAttributes(dataModel.Attributes()));
            }
        }

        public static void ModifyModelNumbers(FanDataModel dataModel)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var models = acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => !o.BlockTableRecord.IsNull)
                    .Where(o => o.ObjectId.IsModel(dataModel.ID, ThHvacCommon.RegAppName_FanSelection))
                    .OrderBy(o => o.ObjectId.GetModelNumber()).ToList();
                var numbers = dataModel.ListVentQuan.OrderBy(o => o).ToList();
                for (int i = 0; i < models.Count; i++)
                {
                    models[i].ObjectId.SetModelNumber(dataModel.InstallFloor, numbers[i]);

                }
            }
        }

        private static void SetModelNumber(this ObjectId obj, string storey, int number)
        {
            obj.UpdateAttributesInBlock(new Dictionary<string, string>()
            {
                { ThFanSelectionCommon.BLOCK_ATTRIBUTE_STOREY_AND_NUMBER, ThFanSelectionUtils.StoreyNumber(storey, number.ToString()) }
            });
        }

        public static void ModifyModelNames(FanDataModel dataModel)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var models = acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => !o.BlockTableRecord.IsNull)
                    .Where(o => o.ObjectId.IsModel(dataModel.ID, ThHvacCommon.RegAppName_FanSelection));
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

        public static bool IsModelStyleChanged(ObjectId model, FanDataModel dataModel)
        {
            return ThFanSelectionUtils.IsHTFCModelStyle(model.GetModelStyle()) ^ dataModel.IsHTFCModel();
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
            return model.GetBlockName() != dataModel.BlockName();
        }

        public static void ZoomToModels(FanDataModel dataModel)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blockReferences = acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => !o.BlockTableRecord.IsNull)
                    .Where(o => o.ObjectId.IsModel(dataModel.ID, ThHvacCommon.RegAppName_FanSelection));
                if (blockReferences.Any())
                {
                    Active.Editor.ZoomToObjects(blockReferences.ToArray(), 2.0);
                    Active.Editor.PickFirstObjects(blockReferences.Select(o => o.ObjectId).ToArray());
                }
            }
        }
    }
}
