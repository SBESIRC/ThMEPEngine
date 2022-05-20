using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service.Hvac;

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
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blkRef.Database))
            {
                var target = acadDatabase.Element<BlockReference>(blkRef, true);
                var targetData = new ThBlockReferenceData(blkRef);
                FillProperties(targetData, source);
                blkRef.UpdateAttributesInBlock(new Dictionary<string, string>(targetData.Attributes));
                if (targetData.EffectiveName.Contains("负载标注"))
                {
                    ThBConvertBlockReferenceDataExtension.AdjustLoadLabel(target);
                }
            }

        }

        public override void SetDatabaseProperties(ObjectId blkRef, ThBlockReferenceData srcBlockReference, string layer)
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
                var br = blkRef.GetObject(OpenMode.ForRead) as BlockReference;
                if(br.IsDynamicBlock)
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
                TransformByFansCenter(blkRef, srcBlockData);
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

        public override void Displacement(ObjectId blkRef, ThBlockReferenceData srcBlockData, List<ThRawIfcDistributionElementData> list, Scale3d scale)
        {
            // 先做泵的常规处理
            TransformByPosition(blkRef, srcBlockData);

            using (AcadDatabase acadDatabase = AcadDatabase.Use(blkRef.Database))
            {
                var srcMCS2WCS = srcBlockData.BlockTransform.PreMultiplyBy(srcBlockData.OwnerSpace2WCS);
                var srcBlockDataPosition = Point3d.Origin.TransformBy(srcMCS2WCS);
                var number = srcBlockData.Attributes["编号"];
                foreach (var block in list)
                {
                    var blockAttributes = (block.Data as ThBlockReferenceData).Attributes;
                    var blockProperties = (block.Data as ThBlockReferenceData).CustomProperties;
                    if (blockAttributes.ContainsKey("集水井编号") && (string)blockAttributes["集水井编号"] == number)
                    {
                        var objId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                                    "0",
                                    "水泵标注",
                                    srcBlockDataPosition,
                                    scale,
                                    0.0,
                                    new Dictionary<string, string>(srcBlockData.Attributes));
                        var pumpLabel = acadDatabase.Element<BlockReference>(objId, true);
                        var pumpLabelData = new ThBlockReferenceData(objId);
                        var pumpAttributes = pumpLabelData.Attributes;
                        if (pumpAttributes.ContainsKey("水泵用途"))
                        {
                            pumpAttributes["水泵用途"] = ("潜水泵");
                        }
                        if (blockAttributes.ContainsKey("电量") && blockAttributes.ContainsKey("井内水泵台数") && pumpAttributes.ContainsKey("水泵电量"))
                        {
                            pumpAttributes["水泵电量"] = blockAttributes["井内水泵台数"] + "x" + blockAttributes["电量"] + "kW";
                        }
                        if (blockProperties.Contains("水泵配置") && pumpAttributes.ContainsKey("主备关系"))
                        {
                            pumpAttributes["主备关系"] = (string)blockProperties.GetValue("水泵配置");
                        }

                        objId.UpdateAttributesInBlock(new Dictionary<string, string>(pumpAttributes));
                        ThBConvertBlockReferenceDataExtension.AdjustLoadLabel(pumpLabel);
                        if (blockAttributes.ContainsKey("井内水泵台数") && blockProperties.Contains("水泵配置"))
                        {
                            var quantity = blockAttributes["井内水泵台数"];
                            var configuration = (string)blockProperties.GetValue("水泵配置");
                            var sum = 0;
                            foreach (var c in configuration)
                            {
                                var cClone = c;
                                if (c == '两')
                                {
                                    cClone = '二';
                                }
                                var num = ThStringTools.ChineseToNumber(cClone.ToString());
                                if (num > -1)
                                {
                                    sum += num;
                                }
                            }
                            if (quantity != sum.ToString())
                            {
                                var obb = pumpLabel.GetBlockOBB();
                                ThBConvertUtils.InsertRevcloud(obb);
                            }
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 过滤图层后，按几何中心调整位置，并设置标注位置
        /// </summary>
        /// <param name="blkRef"></param>
        /// <param name="srcBlockData"></param>
        private void TransformByFansCenter(ObjectId blkRef, ThBlockReferenceData srcBlockData)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blkRef.Database))
            {
                var blockReference = acadDatabase.Element<BlockReference>(blkRef, true);
                var targetBlockData = new ThBlockReferenceData(blkRef);
                var targetMCS2WCS = targetBlockData.BlockTransform.PreMultiplyBy(targetBlockData.OwnerSpace2WCS);
                var scrApproCentriod = srcBlockData.GetCentroidPoint().TransformBy(srcBlockData.OwnerSpace2WCS);
                if (!targetBlockData.EffectiveName.Contains("负载标注"))
                {
                    scrApproCentriod = srcBlockData.Position.TransformBy(srcBlockData.OwnerSpace2WCS);
                }
                var offset = Point3d.Origin.TransformBy(targetMCS2WCS).GetVectorTo(scrApproCentriod);
                blockReference.TransformBy(Matrix3d.Displacement(offset));

                var targetProperties = targetBlockData.CustomProperties;
                var srcProperties = srcBlockData.CustomProperties;
                double base_x = 0, base_y = 0;
                double label_x = 0, label_y = 0;
                if (srcProperties.Contains(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_X))
                {
                    base_x = (double)srcProperties.GetValue(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_X);
                }
                if (srcProperties.Contains(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_X))
                {
                    base_y = (double)srcProperties.GetValue(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_Y);
                }
                if (srcProperties.Contains("标注基点 X"))
                {
                    label_x = (double)srcProperties.GetValue("标注基点 X");
                }
                else if (srcProperties.Contains("位置1 X"))
                {
                    label_x = (double)srcProperties.GetValue("位置1 X");
                }
                if (srcProperties.Contains("标注基点 Y"))
                {
                    label_y = (double)srcProperties.GetValue("标注基点 Y");
                }
                else if (srcProperties.Contains("位置1 X"))
                {
                    label_y = (double)srcProperties.GetValue("位置1 Y");
                }

                double rotation = srcBlockData.Rotation;
                var labelPoint = new Vector3d(label_x - base_x, label_y - base_y, 0);
                if (targetProperties.Contains("位置1 X") && targetProperties.Contains("位置1 Y"))
                {
                    if (rotation > Math.PI / 2 && rotation - 10 * ThBConvertCommon.radian_tolerance <= Math.PI * 3 / 2)
                    {
                        targetProperties.SetValue("位置1 X", -labelPoint.X);
                        targetProperties.SetValue("位置1 Y", -labelPoint.Y);
                    }
                    else
                    {
                        targetProperties.SetValue("位置1 X", labelPoint.X);
                        targetProperties.SetValue("位置1 Y", labelPoint.Y);
                    }
                }
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

                var targetProperties = targetBlockData.CustomProperties;
                var srcProperties = srcBlockData.CustomProperties;
                if (targetProperties.Contains("距离") && srcProperties.Contains("距离"))
                {
                    targetProperties.SetValue("距离", srcProperties.GetValue("距离"));
                }
                if (targetProperties.Contains("距离1") && srcProperties.Contains("距离1"))
                {
                    targetProperties.SetValue("距离1", srcProperties.GetValue("距离1"));
                }
                if (targetProperties.Contains("距离2") && srcProperties.Contains("距离2"))
                {
                    targetProperties.SetValue("距离2", srcProperties.GetValue("距离2"));
                }
                if (targetProperties.Contains("角度") && srcProperties.Contains("角度"))
                {
                    targetProperties.SetValue("角度", srcProperties.GetValue("角度"));
                }
                if (targetProperties.Contains("角度1") && srcProperties.Contains("角度1"))
                {
                    targetProperties.SetValue("角度1", srcProperties.GetValue("角度1"));
                }
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

        public override void Rotate(ObjectId blkRef, ThBlockReferenceData srcBlockData)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blockReference = acadDatabase.Element<BlockReference>(blkRef, true);
                var position = srcBlockData.Position;
                double rotation = srcBlockData.Rotation;
                if (srcBlockData.Normal == new Vector3d(0, 0, -1))
                {
                    rotation = -rotation;
                }
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

        public override void Mirror(ObjectId blkRef, ThBlockReferenceData srcBlockData)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blockReference = acadDatabase.Element<BlockReference>(blkRef, true);
                var targetBlockData = new ThBlockReferenceData(blkRef);
                var targetScale = targetBlockData.ScaleFactors;
                var srcScale = srcBlockData.ScaleFactors;
                var scale = new Scale3d(targetScale.X * srcScale.X, targetScale.Y * srcScale.Y, targetScale.Z * srcScale.Z);
                var mirror = Matrix3d.Identity;
                if (scale.X < 0)
                {
                    if (scale.Y < 0)
                    {
                        if (scale.Z < 0)  //x<0,y<0,z<0
                        {
                            mirror = Matrix3d.Mirroring(Point3d.Origin);
                        }
                        else  //x<0,y<0,z>0
                        {
                            mirror = Matrix3d.Mirroring(new Line3d(Point3d.Origin, new Point3d(0, 0, 1)));
                        }
                    }
                    else
                    {
                        if (scale.Z < 0)  //x<0,y>0,z<0
                        {
                            mirror = Matrix3d.Mirroring(new Line3d(Point3d.Origin, new Point3d(0, 1, 0)));
                        }
                        else  //x<0,y>0,z>0
                        {
                            mirror = Matrix3d.Mirroring(new Plane(Point3d.Origin, new Vector3d(0, 1, 0), new Vector3d(0, 0, 1)));
                        }
                    }
                }
                else
                {
                    if (scale.Y < 0)
                    {
                        if (scale.Z < 0)  //x>0,y<0,z<0
                        {
                            mirror = Matrix3d.Mirroring(new Line3d(Point3d.Origin, new Point3d(1, 0, 0)));
                        }
                        else  //x>0,y<0,z>0
                        {
                            mirror = Matrix3d.Mirroring(new Plane(Point3d.Origin, new Vector3d(1, 0, 0), new Vector3d(0, 0, 1)));
                        }
                    }
                    else
                    {
                        if (scale.Z < 0)  //x>0,y>0,z<0
                        {
                            mirror = Matrix3d.Mirroring(new Plane(Point3d.Origin, new Vector3d(1, 0, 0), new Vector3d(0, 1, 0)));
                        }
                        else  //x>0,y>0,z>0
                        {
                            //
                        }
                    }
                }
                blockReference.TransformBy(mirror);
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

            // 由于翻转会造成文字居中显示异常，故暂不支持负载标注的翻转
            // 翻转状态
            //if (target.CustomProperties.Contains(ThBConvertCommon.PROPERTY_LOAD_FILP)
            //    && source.CustomProperties.Contains(ThBConvertCommon.PROPERTY_LOAD_FILP))
            //{
            //    target.CustomProperties.SetValue(ThBConvertCommon.PROPERTY_LOAD_FILP,
            //        source.CustomProperties.GetValue(ThBConvertCommon.PROPERTY_LOAD_FILP));
            //}
        }
    }
}
