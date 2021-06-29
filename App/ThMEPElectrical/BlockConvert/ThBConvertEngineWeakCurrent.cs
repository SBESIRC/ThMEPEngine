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

        public override void TransformBy(ObjectId blkRef, ThBlockReferenceData srcBlockReference)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blockReference = acadDatabase.Element<BlockReference>(blkRef, true);
                blockReference.TransformBy(srcBlockReference.BlockTransformToHostDwg);
            }
        }

        public override void Adjust(ObjectId blkRef, ThBlockReferenceData srcBlockReference)
        {
            AdjustRotation(blkRef, srcBlockReference);
            AdjustPosition(blkRef, srcBlockReference);
        }

        private void AdjustRotation(ObjectId blkRef, ThBlockReferenceData srcBlockReference)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blockReference = acadDatabase.Element<BlockReference>(blkRef, true);
                double rotation = srcBlockReference.Rotation;
                if ((rotation - Math.PI / 2) > ThBConvertCommon.radian_tolerance &&
                    (rotation - Math.PI * 3 / 2) <= ThBConvertCommon.radian_tolerance)
                {
                    blockReference.TransformBy(Matrix3d.Rotation(Math.PI, Vector3d.ZAxis, blockReference.Position));
                }
            }
        }

        private void AdjustPosition(ObjectId blkRef, ThBlockReferenceData srcBlockReference)
        {
            // 当图块的基点与其中图元的OBB内部或边界上时，目标块基点与源块基点相同
            // 当图块的基点在其中图元的OBB外部时，目标块基点选择源块图元OBB的几何中心
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blkRef.Database))
            {
                var blockReference = acadDatabase.Element<BlockReference>(blkRef);
                var blockReferenceOBB = blockReference.GetBlockReferenceOBB(blockReference.BlockTransform);
                if (!blockReferenceOBB.ContainsPoint(blockReference.Position))
                {
                    var centroid = blockReferenceOBB.GetCentroidPoint();
                    var offset = centroid.GetVectorTo(blockReference.Position);
                    blockReference.TransformBy(Matrix3d.Displacement(offset));
                }
            }
        }
    }
}
