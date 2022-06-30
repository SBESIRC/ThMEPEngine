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
using ThMEPEngineCore.Service.Hvac;

namespace ThMEPHVAC.EQPMFanSelect
{
    class ThFanSelectionEngine
    {
        public static void InsertModels(FanDataModel dataModel, FanDataModel cFanModel)
        {
            var pr = Active.Editor.GetPoint("\n请输入插入点");
            if (pr.Status == PromptStatus.OK)
            {
                var ucsXVector = Active.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d.Xaxis;
                var angle = Vector3d.XAxis.GetAngleTo(ucsXVector, Vector3d.ZAxis);
                for (int i = 0; i < dataModel.ListVentQuan.Count; i++)
                {
                    var number = dataModel.ListVentQuan[i];
                    // 以指定点作为起始点（UCS），沿着X轴方向间隔5000放置图块
                    var insertPt = pr.Value + Vector3d.XAxis * 5000 * i;
                    var position = insertPt.TransformBy(Active.Editor.UCS2WCS());
                    InsertModel(dataModel, cFanModel, number, position, angle);
                }
            }
        }
        public static ObjectId InsertModel(FanDataModel dataModel,FanDataModel cFanModel, int number, Point3d pt,double rotation)
        {
            string blockName = BlockLayerName(dataModel, out string layerName);
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var objId = Active.Database.InsertModel(blockName, layerName, dataModel.Attributes(), rotation);
                SetBlockValue(objId, dataModel, cFanModel, number);
                // 设置风机图块位置
                var blockRef = acadDatabase.Element<BlockReference>(objId, true);
                blockRef.TransformBy(Matrix3d.Displacement(pt - objId.GetModelBasePoint()));
                ChangeBlockTextAttrAngle(objId, rotation);
                // 返回风机图块
                return objId;
            }
        }

        private static void SetBlockValue(ObjectId objId,FanDataModel dataModel,FanDataModel cFanModel,int number) 
        {
            objId.SetModelIdentifier(dataModel.XDataValueList(number, cFanModel, objId.Handle.ToString()), ThHvacCommon.RegAppName_FanSelectionEx);
            SetModelNumber(objId, dataModel.InstallFloor, number);
            objId.SetModelTextHeight();
            UpdateModelName(objId, dataModel);
        }
        public static string BlockLayerName(FanDataModel fanData,out string layerName)
        {
            string blockName = "";
            if (EQPMFanCommon.IsHTFCModelStyle(fanData.VentStyle))
            {
                blockName = EQPMFanCommon.HTFCBlockName(fanData.VentStyle, fanData.IntakeForm, fanData.MountType);
            }
            else
            {
                blockName = EQPMFanCommon.AXIAL_BLOCK_NAME;
            }
            switch (fanData.Scenario)
            {
                case EQPMFanModelEnums.EnumScenario.FireAirSupplement:
                case EQPMFanModelEnums.EnumScenario.FireSmokeExhaust:
                case EQPMFanModelEnums.EnumScenario.FirePressurizedAirSupply:
                    layerName = EQPMFanCommon.BLOCK_LAYER_FIRE;
                    break;
                case EQPMFanModelEnums.EnumScenario.FireAirSupplementAndNormalAirSupply:
                case EQPMFanModelEnums.EnumScenario.FireSmokeExhaustAndNormalExhaust:
                    layerName = EQPMFanCommon.BLOCK_LAYER_DUAL;
                    break;
                default:
                    layerName = EQPMFanCommon.BLOCK_LAYER_EQUP;
                    break;
            }
            return blockName;
        }
       
        private static void SetModelNumber(ObjectId obj, string storey, int number)
        {
            obj.UpdateAttributesInBlock(new Dictionary<string, string>()
            {
                { EQPMFanCommon.BLOCK_ATTRIBUTE_STOREY_AND_NUMBER, EQPMFanCommon.StoreyNumber(storey, number.ToString()) }
            });
        }
        public static void ReplaceModelsInplace(List<BlockReference> hisBlocks,FanDataModel dataModel,FanDataModel cFanModel)
        {
            string blockName = BlockLayerName(dataModel, out string layerName);
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 创建新模型
                var newModels = new ObjectIdCollection();
                foreach (var model in hisBlocks)
                {
                    // 提取原属性
                    var block = new ThBlockReferenceData(model.ObjectId);
                    var xData = model.ObjectId.ReadBlockFanXData(out FanBlockXDataBase xDataBase);
                    if (null == xData || xDataBase == null)
                        continue;
                    var number = xDataBase.Number;
                    // 插入新的图块
                    var objId = Active.Database.InsertModel(blockName, layerName, new Dictionary<string, string>(block.Attributes));
                    var blockRef = acadDatabase.Element<BlockReference>(objId, true);
                    newModels.Add(objId);

                    // 写入原图元XData
                    objId.SetModelIdentifier(dataModel.XDataValueList(number, cFanModel, objId.Handle.ToString()), ThHvacCommon.RegAppName_FanSelectionEx);

                    // 写入原图元属性
                    blockRef.SetPropertiesFrom(model);

                    // 写入原图元位置
                    blockRef.TransformBy(block.BlockTransform);

                    // 写入原图元自定义属性（动态属性）
                    objId.SetModelCustomPropertiesFrom(block.CustomProperties);

                    // 写入修改后的属性
                    objId.ModifyModelAttributes(dataModel.Attributes());
                    SetModelNumber(objId, dataModel.InstallFloor, number);

                    // 更新规格和型号
                    UpdateModelName(objId, dataModel);

                    // 删除原模型
                    model.UpgradeOpen();
                    model.Erase();
                }
            }
        }

        public static void ResetModelRotateState(List<BlockReference> hisBlocks)
        {
            // 更新模型
            foreach (var model in hisBlocks)
            {
                model.UpgradeOpen();
                model.ObjectId.SetModelRotateState((short)0);
                model.DowngradeOpen();
            }
        }

        public static void SetModelRotateState(List<BlockReference> hisBlocks, Dictionary<ObjectId, short> states)
        {
            // 获取原模型对象
            // 更新模型
            foreach (var model in hisBlocks)
            {
                model.UpgradeOpen();
                model.ObjectId.SetModelRotateState(states[model.ObjectId]);
                model.DowngradeOpen();
            }
        }

        public static Dictionary<ObjectId, short> GetModelRotateState(List<BlockReference> targetBlocks)
        {
            var states = new Dictionary<ObjectId, short>();
            // 获取数据
            foreach (var model in targetBlocks)
            {
                states.Add(model.ObjectId, model.ObjectId.GetModelRotateState());
            }
            // 返回数据
            return states;
        }
        public static void ModifyModels(List<BlockReference> blockReferences, Dictionary<string, string> Attributes)
        {
            blockReferences.ForEach(o => o.ObjectId.ModifyModelAttributes(Attributes));
        }

        public static void ModifyModelNumbers(FanDataModel dataModel,FanDataModel cDataModel)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var models = acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => !o.BlockTableRecord.IsNull)
                    .Where(o => o.ObjectId.IsModel(dataModel.ID, ThHvacCommon.RegAppName_FanSelectionEx))
                    .OrderBy(o => o.ObjectId.GetModelNumber()).ToList();
                var numbers = dataModel.ListVentQuan.OrderBy(o => o).ToList();
                for (int i = 0; i < models.Count; i++)
                {
                    var blockId = models[i].ObjectId;
                    var number = numbers[i];
                    SetModelNumber(blockId, dataModel.InstallFloor, number);
                    blockId.SetModelIdentifier(
                        dataModel.XDataValueList(number, cDataModel, blockId.Handle.ToString()), 
                        ThHvacCommon.RegAppName_FanSelectionEx
                        );
                }
            }
        }

        public static void ModifyModelNames(List<BlockReference> targetBlocks,FanDataModel dataModel)
        {
            foreach (var model in targetBlocks.Select((value, i) => new { i, value }))
            {
                // 更新风机型号
                UpdateModelName(model.value.ObjectId, dataModel);
                UpdateModelXData(model.value.ObjectId, dataModel);
            }
        }
        private static void UpdateModelName(ObjectId model, FanDataModel dataModel)
        {
            var strType = CommonUtil.GetEnumDescription(dataModel.VentStyle);
            var strIntakeForm = CommonUtil.GetEnumDescription(dataModel.IntakeForm);

            if (strType.Contains(EQPMFanCommon.AXIAL_TYPE_NAME))
            {
                var typeName = dataModel.FanModelTypeCalcModel.FanModelName;
                model.SetModelName(EQPMFanCommon.AXIALModelName(typeName, dataModel.MountType));
            }
            else
            {
                var typeName = dataModel.FanModelTypeCalcModel.FanModelNum;
                model.SetModelName(EQPMFanCommon.HTFCModelName(strType, strIntakeForm, typeName));
            }
        }
        private static void UpdateModelXData(ObjectId blockId, FanDataModel dataModel) 
        {
            var xData = blockId.ReadBlockFanXData(out FanBlockXDataBase xDataBase);
            if (null == xData || xDataBase == null)
                return;
            blockId.SetModelIdentifier(dataModel.XDataValueList(xDataBase.Number, dataModel, blockId.Handle.ToString()), ThHvacCommon.RegAppName_FanSelectionEx);
        }
        private static void ChangeBlockTextAttrAngle(ObjectId blockId,double angle)
        {
            var block = blockId.GetDBObject<BlockReference>();
            // 遍历块参照的属性
            foreach (ObjectId attId in block.AttributeCollection)
            {
                AttributeReference attRef = attId.GetDBObject<AttributeReference>();
                attRef.Rotation = angle;
            }
        }
    }
}
