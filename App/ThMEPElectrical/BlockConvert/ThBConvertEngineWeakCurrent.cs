using System;
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

        public override void SetDatbaseProperties(ObjectId blkRef, ThBlockReferenceData srcBlockReference)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var block = acadDatabase.Element<Entity>(blkRef, true);
                block.LayerId = ThBConvertDbUtils.BlockLayer(ThBConvertCommon.LAYER_BLOCK_WEAKCURRENT, 3);
            }
        }

        public override void SetVisibilityState(ObjectId blkRef, ThBlockReferenceData srcBlockReference)
        {
            //
        }

        public override void TransformBy(ObjectId blkRef, ThBlockReferenceData srcBlockData)
        {
            var tolerance = 800;
            if (srcBlockData.EffectiveName.Contains("防火阀"))
            {
                TransformByCenter_FireDamper(blkRef, srcBlockData);
            }
            else if (srcBlockData.EffectiveName.Contains("自动扫描射水高空水炮") ||
                     srcBlockData.EffectiveName.Contains("消防炮") ||
                     srcBlockData.EffectiveName.Contains("大空间灭火装置") ||
                     srcBlockData.EffectiveName.Contains("自动扫描射水喷头"))
            {
                TransformByCenter(blkRef, srcBlockData, tolerance);
            }
            else
            {
                tolerance = 100;
                TransformByCenter(blkRef, srcBlockData, tolerance);
            }
        }

        private void TransformByCenter(ObjectId blkRef, ThBlockReferenceData srcBlockData, int tolerance)
        {
            // 考虑几何中心点与位置点的偏差，如果偏差在允许范围内，则取位置点进行计算
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blkRef.Database))
            {
                var blockReference = acadDatabase.Element<BlockReference>(blkRef, true);
                var targetBlockData = new ThBlockReferenceData(blkRef);
                var targetPoint = targetBlockData.GetCentroidPoint().DistanceTo(targetBlockData.Position) < tolerance
                    ? targetBlockData.Position : targetBlockData.GetCentroidPoint();
                var srcBlockDataPosition = new Point3d().TransformBy(srcBlockData.MCS2WCS);
                var srcBlockDataPoint = srcBlockData.GetCentroidPoint().DistanceTo(srcBlockDataPosition) < tolerance
                    ? srcBlockDataPosition : srcBlockData.GetCentroidPoint();
                var offset = targetPoint.GetVectorTo(srcBlockDataPoint);
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
                double length = 0, width = 0;
                if (srcBlockData.CustomProperties.Contains("长度"))
                {
                    length = (double)dynamicProperties.GetValue("长度");
                }
                if (srcBlockData.CustomProperties.Contains("宽度或直径"))
                {
                    width = (double)dynamicProperties.GetValue("宽度或直径");
                }
                var targetPoint = targetBlockData.GetCentroidPoint().DistanceTo(targetBlockData.Position) < 100
                    ? targetBlockData.Position : targetBlockData.GetCentroidPoint();
                var srcBlockDataPosition = new Point3d(width / 2, -length / 2, 0).TransformBy(srcBlockData.MCS2WCS);
                var offset = targetPoint.GetVectorTo(srcBlockDataPosition);
                blockReference.TransformBy(Matrix3d.Displacement(offset));
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
                var targetPoint = targetBlockData.GetCentroidPoint().DistanceTo(targetBlockData.Position) < 100
                    ? targetBlockData.Position : targetBlockData.GetCentroidPoint();
                if (rotation > Math.PI / 2 && rotation - 10 * ThBConvertCommon.radian_tolerance <= Math.PI * 3 / 2)
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
}
