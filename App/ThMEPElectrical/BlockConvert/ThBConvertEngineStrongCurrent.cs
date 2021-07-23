using System;
using Linq2Acad;
using DotNetARX;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service.Hvac;
using ThCADCore.NTS;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertEngineStrongCurrent : ThBConvertEngine
    {
        public override ObjectId Insert(string name, Scale3d scale, ThBlockReferenceData srcBlockReference)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                return acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                    "0",
                    name,
                    Point3d.Origin,
                    scale,
                    0.0,
                    new Dictionary<string, string>(srcBlockReference.Attributes));
            }
        }

        public override void MatchProperties(ObjectId blkRef, ThBlockReferenceData source)
        {
            var target = new ThBlockReferenceData(blkRef);
            FillProperties(target, source);
            blkRef.UpdateAttributesInBlock(new Dictionary<string, string>(target.Attributes));
        }

        public override void SetDatbaseProperties(ObjectId blkRef, ThBlockReferenceData srcBlockReference, string layer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ThBConvertDbUtils.UpdateLayerSettings(layer);
                var block = acadDatabase.Element<Entity>(blkRef, true);
                block.Layer = layer;
            }
        }

        public override void SetVisibilityState(ObjectId blkRef, ThBlockReferenceData srcBlockReference)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                if (ThBConvertUtils.IsFirePower(srcBlockReference))
                {
                    blkRef.SetDynBlockValue("电源类别", ThBConvertCommon.PROPERTY_VALUE_FIRE_POWER);
                }
                else
                {
                    blkRef.SetDynBlockValue("电源类别", ThBConvertCommon.PROPERTY_VALUE_NON_FIRE_POWER);
                }
            }
        }

        public override void Displacement(ObjectId blkRef, ThBlockReferenceData srcBlockData)
        {
            var name = srcBlockData.EffectiveName;
            if (name.Contains("风机") ||
                name.Contains("组合式空调器") ||
                name.Contains("暖通其他设备标注") ||
                name.Contains("风冷热泵") ||
                name.Contains("冷水机组") ||
                name.Contains("冷却塔"))
            {
                TransformByBase(blkRef, srcBlockData);
            }
            else if (name.Contains("潜水泵"))
            {
                TransformByPosition(blkRef, srcBlockData);
            }
            else
            {
                TransformByCenter(blkRef, srcBlockData);
            }
        }

        /// <summary>
        /// 按几何中心调整位置
        /// </summary>
        /// <param name="blkRef"></param>
        /// <param name="srcBlockData"></param>
        private void TransformByCenter(ObjectId blkRef, ThBlockReferenceData srcBlockData)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blkRef.Database))
            {
                var blockReference = acadDatabase.Element<BlockReference>(blkRef, true);
                var targetBlockData = new ThBlockReferenceData(blkRef);
                var targetCentriodPoint = targetBlockData.GetCentroidPoint().TransformBy(targetBlockData.OwnerSpace2WCS);
                var scrCentriodPoint = srcBlockData.GetCentroidPoint().TransformBy(srcBlockData.OwnerSpace2WCS);
                var offset = targetCentriodPoint.GetVectorTo(scrCentriodPoint);
                blockReference.TransformBy(Matrix3d.Displacement(offset));
            }
        }
        
        /// <summary>
        /// 按设备基点调整位置
        /// </summary>
        /// <param name="blkRef"></param>
        /// <param name="srcBlockData"></param>
        private void TransformByBase(ObjectId blkRef, ThBlockReferenceData srcBlockData)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blkRef.Database))
            {
                var blockReference = acadDatabase.Element<BlockReference>(blkRef, true);
                var targetBlockData = new ThBlockReferenceData(blkRef);
                var dynamicProperties = srcBlockData.CustomProperties;
                double base_x = 0, base_y = 0;
                if (dynamicProperties.Contains(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_X))
                {
                    base_x = (double)dynamicProperties.GetValue(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_X);
                }
                if (dynamicProperties.Contains(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_X))
                {
                    base_y = (double)dynamicProperties.GetValue(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_Y);
                }
                double rotation = srcBlockData.Rotation;
                var basePointAfterRotation = new Vector3d(base_x, base_y, 0).
                    TransformBy(Matrix3d.Rotation(rotation, Vector3d.ZAxis, Point3d.Origin).PreMultiplyBy(srcBlockData.OwnerSpace2WCS));
                var targetMCS2WCS = targetBlockData.BlockTransform.PreMultiplyBy(targetBlockData.OwnerSpace2WCS);
                var srcMCS2WCS = srcBlockData.BlockTransform.PreMultiplyBy(srcBlockData.OwnerSpace2WCS);
                var srcBlockDataPosition = Point3d.Origin.TransformBy(srcMCS2WCS);
                var srcBlockDataPoint = new Point3d(
                    srcBlockDataPosition.X + basePointAfterRotation.X,
                    srcBlockDataPosition.Y + basePointAfterRotation.Y,
                    srcBlockDataPosition.Z + basePointAfterRotation.Z);
                var offset = Point3d.Origin.TransformBy(targetMCS2WCS).GetVectorTo(srcBlockDataPoint);
                blockReference.TransformBy(Matrix3d.Displacement(offset));
            }
        }

        /// <summary>
        /// 按插入点调整位置
        /// </summary>
        /// <param name="blkRef"></param>
        /// <param name="srcBlockData"></param>
        private void TransformByPosition(ObjectId blkRef, ThBlockReferenceData srcBlockData)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blkRef.Database))
            {
                var blockReference = acadDatabase.Element<BlockReference>(blkRef, true);
                var targetBlockData = new ThBlockReferenceData(blkRef);
                var targetMCS2WCS = targetBlockData.BlockTransform.PreMultiplyBy(targetBlockData.OwnerSpace2WCS);
                var targetBlockDataPosition = Point3d.Origin.TransformBy(targetMCS2WCS);
                var srcMCS2WCS = srcBlockData.BlockTransform.PreMultiplyBy(srcBlockData.OwnerSpace2WCS);
                var srcBlockDataPosition = Point3d.Origin.TransformBy(srcMCS2WCS);
                var offset = targetBlockDataPosition.GetVectorTo(srcBlockDataPosition);
                blockReference.TransformBy(Matrix3d.Displacement(offset));

                if (targetBlockData.CustomProperties.Contains("距离") && srcBlockData.CustomProperties.Contains("距离"))
                {
                    targetBlockData.CustomProperties.SetValue("距离", srcBlockData.CustomProperties.GetValue("距离"));
                }
                if (targetBlockData.CustomProperties.Contains("距离1") && srcBlockData.CustomProperties.Contains("距离1"))
                {
                    targetBlockData.CustomProperties.SetValue("距离1", srcBlockData.CustomProperties.GetValue("距离1"));
                }
                if (targetBlockData.CustomProperties.Contains("距离2") && srcBlockData.CustomProperties.Contains("距离2"))
                {
                    targetBlockData.CustomProperties.SetValue("距离2", srcBlockData.CustomProperties.GetValue("距离2"));
                }
                if (targetBlockData.CustomProperties.Contains("角度") && srcBlockData.CustomProperties.Contains("角度"))
                {
                    targetBlockData.CustomProperties.SetValue("角度", srcBlockData.CustomProperties.GetValue("角度"));
                }
                if (targetBlockData.CustomProperties.Contains("角度1") && srcBlockData.CustomProperties.Contains("角度1"))
                {
                    targetBlockData.CustomProperties.SetValue("角度1", srcBlockData.CustomProperties.GetValue("角度1"));
                }
            }
        }

        public override void Rotate(ObjectId blkRef, ThBlockReferenceData srcBlockData)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blockReference = acadDatabase.Element<BlockReference>(blkRef, true);
                var position = srcBlockData.Position;
                double rotation = srcBlockData.Rotation;
                if (srcBlockData.EffectiveName.Contains("潜水泵"))
                {
                    blockReference.TransformBy(Matrix3d.Rotation(rotation, Vector3d.ZAxis, position));
                }
                else
                {
                    if (rotation > Math.PI / 2 && rotation - 10 * ThBConvertCommon.radian_tolerance <= Math.PI * 3 / 2)
                    {
                        blockReference.TransformBy(Matrix3d.Rotation(rotation - Math.PI, Vector3d.ZAxis, position));
                    }
                    else
                    {
                        blockReference.TransformBy(Matrix3d.Rotation(rotation, Vector3d.ZAxis, position));
                    }
                }
            }
        }

        private void FillProperties(ThBlockReferenceData target, ThBlockReferenceData source)
        {
            // 负载编号：“设备符号"&"-"&"楼层-编号”
            if (target.Attributes.ContainsKey(ThBConvertCommon.PROPERTY_LOAD_NUMBER))
            {
                target.Attributes[ThBConvertCommon.PROPERTY_LOAD_NUMBER] = ThBConvertUtils.LoadSN(source);
            }

            // 电量：“电量”
            if (target.Attributes.ContainsKey(ThBConvertCommon.PROPERTY_POWER_QUANTITY))
            {
                target.Attributes[ThBConvertCommon.PROPERTY_POWER_QUANTITY] = ThBConvertUtils.LoadPowerFromTHModel(source);
            }

            // 负载用途：“负载用途”
            if (target.Attributes.ContainsKey(ThBConvertCommon.PROPERTY_LOAD_USAGE))
            {
                target.Attributes[ThBConvertCommon.PROPERTY_LOAD_USAGE] = ThBConvertUtils.LoadUsage(source);
            }

            // 
        }
    }
}
