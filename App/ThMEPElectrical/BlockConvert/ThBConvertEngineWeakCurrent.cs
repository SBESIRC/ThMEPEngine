﻿using System;
using Linq2Acad;
using DotNetARX;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertEngineWeakCurrent : ThBConvertEngine
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

        public override void MatchProperties(ObjectId blkRef, ThBlockReferenceData srcBlockReference)
        {
            //
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
            //
        }

        public override void Displacement(ObjectId blkRef, ThBlockReferenceData srcBlockData)
        {
            if (srcBlockData.EffectiveName.Contains("自动扫描射水高空水炮") ||
                srcBlockData.EffectiveName.Contains("消防炮") ||
                srcBlockData.EffectiveName.Contains("大空间灭火装置") ||
                srcBlockData.EffectiveName.Contains("自动扫描射水喷头"))
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
                var targetCentriodPoint = targetBlockData.GetCentroidPoint().TransformBy(targetBlockData.OwnerSpace2WCS);
                var targetPoint = targetCentriodPoint.DistanceTo(targetBlockDataPosition) < 2000
                    ? targetBlockDataPosition : targetCentriodPoint;
                var srcMCS2WCS = srcBlockData.BlockTransform.PreMultiplyBy(srcBlockData.OwnerSpace2WCS);
                var srcBlockDataPosition = Point3d.Origin.TransformBy(srcMCS2WCS);
                var srcCentriodPoint = srcBlockData.GetCentroidPoint().TransformBy(srcBlockData.OwnerSpace2WCS);
                var srcPoint = srcCentriodPoint.DistanceTo(srcBlockDataPosition) < 2000
                    ? srcBlockDataPosition : srcCentriodPoint;
                var offset = targetPoint.GetVectorTo(srcPoint);
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
                if (srcBlockData.EffectiveName.Contains("室内消火栓平面"))
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
    }
}
