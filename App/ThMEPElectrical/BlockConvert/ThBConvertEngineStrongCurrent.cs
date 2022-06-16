﻿using System;
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
            using (var acadDatabase = AcadDatabase.Active())
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

        public override void MatchProperties(ThBlockReferenceData targetBlockData, ThBlockReferenceData source)
        {
            using (var acadDatabase = AcadDatabase.Use(targetBlockData.Database))
            {
                var target = acadDatabase.Element<BlockReference>(targetBlockData.ObjId, true);
                FillProperties(targetBlockData, source);
                targetBlockData.ObjId.UpdateAttributesInBlock(new Dictionary<string, string>(targetBlockData.Attributes));
                if (targetBlockData.EffectiveName.Contains(ThBConvertCommon.BLOCK_LOAD_DIMENSION))
                {
                    ThBConvertBlockReferenceDataExtension.AdjustLoadLabel(target);
                }
            }
        }

        public override void SetDatabaseProperties(ThBlockReferenceData targetBlockData, ObjectId objId, string layer)
        {
            using (var acadDatabase = AcadDatabase.Use(targetBlockData.Database))
            {
                ThBConvertDbUtils.UpdateLayerSettings(layer);
                var block = acadDatabase.Element<Entity>(objId, true);
                block.Layer = layer;
            }
        }

        public override void SetVisibilityState(ThBlockReferenceData targetBlockData, ThBlockReferenceData srcBlockReference)
        {
            using (var acadDatabase = AcadDatabase.Use(targetBlockData.Database))
            {
                if (!targetBlockData.CustomProperties.IsNull())
                {
                    if (ThBConvertUtils.IsFirePower(srcBlockReference))
                    {
                        targetBlockData.ObjId.SetDynBlockValue("电源类别", ThBConvertCommon.PROPERTY_VALUE_FIRE_POWER);
                    }
                    else
                    {
                        targetBlockData.ObjId.SetDynBlockValue("电源类别", ThBConvertCommon.PROPERTY_VALUE_NON_FIRE_POWER);
                    }
                }
            }
        }

        public override void Displacement(ThBlockReferenceData targetBlockData, ThBlockReferenceData srcBlockData, List<ThRawIfcDistributionElementData> list, Scale3d scale)
        {
            // 先做泵的常规处理
            Displacement(targetBlockData, srcBlockData);

            using (var acadDatabase = AcadDatabase.Use(targetBlockData.Database))
            {
                foreach (var block in list)
                {
                    var blockAttributes = (block.Data as ThBlockReferenceData).Attributes;
                    var blockProperties = (block.Data as ThBlockReferenceData).CustomProperties;
                    if (blockAttributes.ContainsKey("集水井编号") && (string)blockAttributes["集水井编号"] == srcBlockData.Attributes["编号"])
                    {
                        // 插入水泵标注，并获得其id
                        var objId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                                    "0",
                                    "水泵标注",
                                    srcBlockData.Position,
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
        /// 分别计算源块与目标块的插入点，并移动
        /// </summary>
        /// <param name="blkRef"></param>
        /// <param name="srcBlockData"></param>
        public override void Displacement(ThBlockReferenceData targetBlockData, ThBlockReferenceData srcBlockData)
        {
            using (var acadDatabase = AcadDatabase.Use(targetBlockData.Database))
            {
                var blockReference = acadDatabase.Element<BlockReference>(targetBlockData.ObjId, true);
                var targetBlockDataPosition = targetBlockData.GetNewBasePoint(false);
                var srcBlockDataPosition = srcBlockData.GetNewBasePoint(true).TransformBy(srcBlockData.OwnerSpace2WCS);
                var offset = targetBlockDataPosition.GetVectorTo(srcBlockDataPosition);
                blockReference.TransformBy(Matrix3d.Displacement(offset));
                targetBlockData.Position = blockReference.Position;
            }
        }

        public override void SpecialTreatment(ThBlockReferenceData targetBlockData, ThBlockReferenceData srcBlockData)
        {
            using (var acadDatabase = AcadDatabase.Use(targetBlockData.Database))
            {
                var targetProperties = targetBlockData.CustomProperties;
                var srcProperties = srcBlockData.CustomProperties;
                if (targetProperties.IsNull() || srcProperties.IsNull())
                {
                    return;
                }

                // 电动机及负载标注 & 负载标注
                if (targetBlockData.EffectiveName.Contains(ThBConvertCommon.BLOCK_LOAD_DIMENSION))
                {
                    double label_x = 0, label_y = 0;
                    var srcPosition = srcBlockData.Position.TransformBy(srcBlockData.OwnerSpace2WCS);
                    if (srcProperties.Contains(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_ANNOTATION_BASE_POINT_X))
                    {
                        label_x = (double)srcProperties.GetValue(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_ANNOTATION_BASE_POINT_X);
                    }
                    else if (srcProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_POSITION1_X))
                    {
                        label_x = (double)srcProperties.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_POSITION1_X);
                    }
                    if (srcProperties.Contains(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_ANNOTATION_BASE_POINT_Y))
                    {
                        label_y = (double)srcProperties.GetValue(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_ANNOTATION_BASE_POINT_Y);
                    }
                    else if (srcProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_POSITION1_Y))
                    {
                        label_y = (double)srcProperties.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_POSITION1_Y);
                    }

                    var labelPoint = new Point3d(label_x, label_y, 0);
                    if (srcPosition.DistanceTo(targetBlockData.Position) > 1000.0)
                    {
                        labelPoint = new Point3d(label_x + (srcPosition.X - targetBlockData.Position.X) * srcBlockData.ScaleFactors.X,
                            label_y + (srcPosition.Y - targetBlockData.Position.Y) * srcBlockData.ScaleFactors.Y, 0);
                    }
                    if (targetProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_POSITION1_X)
                        && targetProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_POSITION1_Y))
                    {
                        targetProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_POSITION1_X, labelPoint.X);
                        targetProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_POSITION1_Y, labelPoint.Y);
                    }
                }
                // 部分风机
                else if (targetBlockData.EffectiveName.Equals("E-BFAN010")
                    || targetBlockData.EffectiveName.Equals("E-BFAN011")
                    || targetBlockData.EffectiveName.Equals("E-BFAN012"))
                {
                    if (srcProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_FAN_ANGLE)
                        && targetProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_FAN_ANGLE))
                    {
                        targetProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_FAN_ANGLE,
                            srcProperties.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_FAN_ANGLE));
                    }
                    if (srcProperties.Contains(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_ANNOTATION_POSITION_POINT_X)
                        && targetProperties.Contains(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_ANNOTATION_POSITION_POINT_X))
                    {
                        targetProperties.SetValue(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_ANNOTATION_POSITION_POINT_X,
                            srcProperties.GetValue(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_ANNOTATION_POSITION_POINT_X));
                    }
                    if (srcProperties.Contains(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_ANNOTATION_POSITION_POINT_Y)
                        && targetProperties.Contains(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_ANNOTATION_POSITION_POINT_Y))
                    {
                        targetProperties.SetValue(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_ANNOTATION_POSITION_POINT_Y,
                            srcProperties.GetValue(ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_ANNOTATION_POSITION_POINT_Y));
                    }
                    if (srcProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_ROTATE1)
                        && targetProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_ROTATE1))
                    {
                        targetProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_ROTATE1,
                            srcProperties.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_ROTATE1));
                    }
                }
                else if (srcBlockData.EffectiveName.Contains(ThBConvertCommon.BLOCK_SUBMERSIBLE_PUMP))
                {
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
        }

        /// <summary>
        /// 按插入点调整位置
        /// </summary>
        /// <param name="blkRef"></param>
        /// <param name="srcBlockData"></param>
        private void TransformByBasePoint(ObjectId blkRef, ThBlockReferenceData srcBlockData)
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

        public override void Rotate(ThBlockReferenceData targetBlockData, ThBlockReferenceData srcBlockData, ThBlockConvertBlock convertRule)
        {
            using (var acadDatabase = AcadDatabase.Use(targetBlockData.Database))
            {
                var blockReference = acadDatabase.Element<BlockReference>(targetBlockData.ObjId, true);
                var position = srcBlockData.Position;
                var rotation = srcBlockData.Rotation;
                if (srcBlockData.Normal == new Vector3d(0, 0, -1))
                {
                    rotation = -rotation;
                }
                if (convertRule.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_ROTATION_CORRECT].Equals(false))
                {
                    blockReference.TransformBy(Matrix3d.Rotation(rotation, Vector3d.ZAxis, position));
                    targetBlockData.Position = blockReference.Position;
                }
                else
                {
                    if (rotation > Math.PI / 2 && rotation < Math.PI * 3 / 2)
                    {
                        blockReference.TransformBy(Matrix3d.Rotation(rotation - Math.PI, Vector3d.ZAxis, position));
                        targetBlockData.Position = blockReference.Position;
                    }
                    else
                    {
                        blockReference.TransformBy(Matrix3d.Rotation(rotation, Vector3d.ZAxis, position));
                        targetBlockData.Position = blockReference.Position;
                    }
                }
            }
        }

        public override void Mirror(ThBlockReferenceData targetBlockData, ThBlockReferenceData srcBlockData)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(targetBlockData.Database))
            {
                var blockReference = acadDatabase.Element<BlockReference>(targetBlockData.ObjId, true);
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

            //// 电量：“电量”
            //if (target.Attributes.ContainsKey(ThBConvertCommon.PROPERTY_POWER_QUANTITY))
            //{
            //    target.Attributes[ThBConvertCommon.PROPERTY_POWER_QUANTITY] = ThBConvertUtils.LoadPowerFromTHModel(source);
            //}

            // 负载电量：“负载电量”
            if (target.Attributes.ContainsKey(ThBConvertCommon.PROPERTY_LOAD_POWER_QUANTITY))
            {
                target.Attributes[ThBConvertCommon.PROPERTY_LOAD_POWER_QUANTITY] = ThBConvertUtils.LoadPowerFromTHModel(source);
            }

            // 负载用途：“负载用途”
            if (target.Attributes.ContainsKey(ThBConvertCommon.PROPERTY_LOAD_USAGE))
            {
                target.Attributes[ThBConvertCommon.PROPERTY_LOAD_USAGE] = ThBConvertUtils.LoadUsage(source);
            }

            // 由于翻转会造成文字居中显示异常，故暂不支持负载标注的翻转
            // 翻转状态
            if (!target.CustomProperties.IsNull() && target.CustomProperties.Contains(ThBConvertCommon.PROPERTY_LOAD_FILP)
                && !source.CustomProperties.IsNull() && source.CustomProperties.Contains(ThBConvertCommon.PROPERTY_LOAD_FILP))
            {
                target.CustomProperties.SetValue(ThBConvertCommon.PROPERTY_LOAD_FILP,
                    source.CustomProperties.GetValue(ThBConvertCommon.PROPERTY_LOAD_FILP));
            }
        }
    }
}
