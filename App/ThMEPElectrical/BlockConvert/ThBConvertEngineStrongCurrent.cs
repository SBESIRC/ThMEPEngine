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

        public override void SetDatbaseProperties(ObjectId blkRef, ThBlockReferenceData srcBlockReference)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var block = acadDatabase.Element<Entity>(blkRef, true);
                block.LayerId = ThBConvertDbUtils.BlockLayer(ThBConvertCommon.LAYER_BLOCK_STRONGCURRENT, 2);
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

        public override void TransformBy(ObjectId blkRef, ThBlockReferenceData srcBlockData)
        {
            if (srcBlockData.EffectiveName.Contains("风机"))
            {
                TransformByBase(blkRef, srcBlockData);
            }
            else if (srcBlockData.EffectiveName.Contains("潜水泵"))
            {
                TransformByPosition(blkRef, srcBlockData);
            }
            else if (srcBlockData.EffectiveName.Contains("防火阀"))
            {
                TransformByCenter_FireDamper(blkRef, srcBlockData);
            }
            else
            {
                TransformByCenter(blkRef, srcBlockData);
            }
        }

        private void TransformByCenter(ObjectId blkRef, ThBlockReferenceData srcBlockData)
        {
            // 考虑几何中心
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blkRef.Database))
            {
                var blockReference = acadDatabase.Element<BlockReference>(blkRef, true);
                var targetBlockData = new ThBlockReferenceData(blkRef);
                var targetPoint = targetBlockData.GetCentroidPoint().DistanceTo(targetBlockData.Position) < 100
                    ? targetBlockData.Position : targetBlockData.GetCentroidPoint();
                var srcBlockDataPosition = new Point3d().TransformBy(srcBlockData.MCS2WCS);
                var srcBlockDataPoint = srcBlockData.GetCentroidPoint().DistanceTo(srcBlockDataPosition) < 100
                    ? srcBlockDataPosition : srcBlockData.GetCentroidPoint();
                var offset = targetPoint.GetVectorTo(srcBlockDataPoint);
                blockReference.TransformBy(Matrix3d.Displacement(offset));
            }
        }

        private void TransformByBase(ObjectId blkRef, ThBlockReferenceData srcBlockData)
        {
            // 考虑设备基点
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
                var offset = targetBlockData.Position.GetVectorTo(new Point3d(base_x, base_y, 0).TransformBy(srcBlockData.MCS2WCS));
                blockReference.TransformBy(Matrix3d.Displacement(offset));
            }
        }

        private void TransformByCenter_FireDamper(ObjectId blkRef, ThBlockReferenceData srcBlockData)
        {
            // 考虑防火阀的几何中心
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blkRef.Database))
            {
                var blockReference = acadDatabase.Element<BlockReference>(blkRef, true);
                var targetBlockData = new ThBlockReferenceData(blkRef);
                var dynamicProperties = srcBlockData.CustomProperties;
                double centroid_x = 0, centroid_y = 0;
                if (dynamicProperties.Contains("长度"))
                {
                    centroid_x = (double)dynamicProperties.GetValue("长度");
                }
                if (dynamicProperties.Contains("宽度"))
                {
                    centroid_y = (double)dynamicProperties.GetValue("宽度");
                }
                var offset = targetBlockData.Position.GetVectorTo(new Point3d(centroid_x / 2, centroid_y / 2, 0).TransformBy(srcBlockData.MCS2WCS));
                blockReference.TransformBy(Matrix3d.Displacement(offset));
            }
        }

        private void TransformByPosition(ObjectId blkRef, ThBlockReferenceData srcBlockData)
        {
            // 考虑位置点
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blkRef.Database))
            {
                var blockReference = acadDatabase.Element<BlockReference>(blkRef, true);
                var targetBlockData = new ThBlockReferenceData(blkRef);
                var targetPoint = targetBlockData.GetCentroidPoint().DistanceTo(targetBlockData.Position) < 2000
                    ? targetBlockData.Position : targetBlockData.GetCentroidPoint();
                var srcBlockDataPosition = new Point3d().TransformBy(srcBlockData.MCS2WCS);
                var srcBlockDataPoint = srcBlockData.GetCentroidPoint().DistanceTo(srcBlockDataPosition) < 2000
                    ? srcBlockDataPosition : srcBlockData.GetCentroidPoint();
                var offset = targetPoint.GetVectorTo(srcBlockDataPoint);
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

        public override void Adjust(ObjectId blkRef, ThBlockReferenceData srcBlockReference)
        {
            AdjustRotation(blkRef, srcBlockReference);
        }

        private void AdjustRotation(ObjectId blkRef, ThBlockReferenceData srcBlockReference)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blockReference = acadDatabase.Element<BlockReference>(blkRef, true);
                var targetBlockData = new ThBlockReferenceData(blkRef);
                double rotation = srcBlockReference.Rotation;
                if (srcBlockReference.EffectiveName.Contains("防火阀"))
                {
                    var srcBlockDataPosition = new Point3d().TransformBy(srcBlockReference.MCS2WCS);
                    var targetPoint = targetBlockData.GetCentroidPoint().DistanceTo(targetBlockData.Position) < 100
                    ? targetBlockData.Position : targetBlockData.GetCentroidPoint();
                    if ((rotation - Math.PI / 2) > ThBConvertCommon.radian_tolerance &&
                        (rotation - Math.PI * 3 / 2) <= ThBConvertCommon.radian_tolerance)
                    {
                        blockReference.TransformBy(Matrix3d.Rotation(rotation - Math.PI * 3 / 2, Vector3d.ZAxis, srcBlockDataPosition));
                        blockReference.TransformBy(Matrix3d.Rotation(Math.PI / 2, Vector3d.ZAxis, targetPoint));
                    }
                    else
                    {
                        blockReference.TransformBy(Matrix3d.Rotation(rotation - Math.PI / 2, Vector3d.ZAxis, srcBlockDataPosition));
                        blockReference.TransformBy(Matrix3d.Rotation(Math.PI / 2, Vector3d.ZAxis, targetPoint));
                    }
                }
                else
                {
                    var targetPoint = targetBlockData.GetCentroidPoint().DistanceTo(targetBlockData.Position) < 100
                    ? targetBlockData.Position : targetBlockData.GetCentroidPoint();
                    if ((rotation - Math.PI / 2) > ThBConvertCommon.radian_tolerance &&
                        (rotation - Math.PI * 3 / 2) <= ThBConvertCommon.radian_tolerance)
                    {
                        blockReference.TransformBy(Matrix3d.Rotation(rotation - Math.PI, Vector3d.ZAxis, targetPoint));
                    }
                    else
                    {
                        blockReference.TransformBy(Matrix3d.Rotation(rotation, Vector3d.ZAxis, targetPoint));
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
