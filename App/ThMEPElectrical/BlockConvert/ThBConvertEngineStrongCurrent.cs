﻿using System;
using Linq2Acad;
using DotNetARX;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Service.Hvac;
using Autodesk.AutoCAD.DatabaseServices;
using System.Linq;
using NFox.Cad;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;

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
            if (source.EffectiveName.Contains("风机") ||
                source.EffectiveName.Contains("组合式空调器") ||
                source.EffectiveName.Contains("暖通其他设备标注") ||
                source.EffectiveName.Contains("风冷热泵") ||
                source.EffectiveName.Contains("冷水机组") ||
                source.EffectiveName.Contains("冷却塔"))
            {
                AdjustLoadLabel(target);
            }
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

            // 
        }

        private void AdjustLoadLabel(ThBlockReferenceData targetBlockData)
        {
            var entitiesClone = new DBObjectCollection();
            double textMaxWidth = double.MinValue;
            using (AcadDatabase acadDatabase = AcadDatabase.Use(targetBlockData.Database))
            {
                var entities = new DBObjectCollection();
                var blkref = acadDatabase.Element<BlockReference>(targetBlockData.ObjId);
                blkref.Explode(entities);
                entities = entities.Cast<Entity>()
                        .Where(e => e.Layer == "E-UNIV-NOTE")
                        .Where(e => e is DBText)
                        .ToCollection();
                var stringList = targetBlockData.Attributes.Values;
                for (int i = 0; i < entities.Count; i++) 
                {
                    foreach (string str in stringList)
                    {
                        var mText = new MText();
                        mText.Contents = str;
                        mText.TextStyleId = (entities[i] as DBText).TextStyleId;
                        mText.TextHeight = (entities[i] as DBText).Height;

                        var pt1 = new Point2d((entities[i] as DBText).Position.X, (entities[i] as DBText).Position.Y);
                        var pt2 = new Point2d(pt1.X + mText.ActualWidth, pt1.Y);
                        var pt3 = new Point2d(pt2.X, pt2.Y + mText.ActualHeight);
                        var pt4 = new Point2d(pt1.X, pt1.Y + mText.ActualHeight);
                        var pts = new Point2dCollection() { pt1, pt2, pt3, pt4 };
                        var obb = ThDrawTool.CreatePolyline(pts);
                        var mt = Matrix3d.Rotation((entities[i] as DBText).Rotation, (entities[i] as DBText).Normal, (entities[i] as DBText).Position);
                        obb.TransformBy(mt);
                        var width = GetWidth(obb);
                        if (width > textMaxWidth)
                        {
                            textMaxWidth = width;
                        }
                    }
                }
            }
            textMaxWidth = textMaxWidth > 1600 ? (textMaxWidth + 500) : 2100;
            targetBlockData.CustomProperties.SetValue("标注表格宽度", textMaxWidth);
        }

        private double GetWidth(Polyline textObb)
        {
            return textObb.ToLines().OrderByDescending(o => o.Length).First().Length;
        }
    }
}
