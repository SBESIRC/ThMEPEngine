using System;
using Linq2Acad;
using DotNetARX;
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
                var block = acadDatabase.Element<Entity>(blkRef);

                // 图层
                block.LayerId = ThBConvertDbUtils.BlockLayer(ThBConvertCommon.LAYER_BLOCK_WEAKCURRENT, 3);
            }
        }

        public override void TransformBy(ObjectId blkRef, ThBlockReferenceData srcBlockReference)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blockReference = acadDatabase.Element<BlockReference>(blkRef, true);
                blockReference.TransformBy(srcBlockReference.BlockTransform);
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
            //
        }
    }
}
