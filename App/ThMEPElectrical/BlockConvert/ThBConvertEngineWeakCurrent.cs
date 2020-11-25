using System;
using Linq2Acad;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;

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
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Element<BlockReference>(blkRef, true).LayerId = ThBConvertDbUtils.BlockLayer();
            }
        }

        public override void SetDatbaseProperties(ObjectId blkRef, ThBlockReferenceData srcBlockReference)
        {
            throw new NotImplementedException();
        }

        public override void TransformBy(ObjectId blkRef, ThBlockReferenceData srcBlockReference)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blockReference = acadDatabase.Element<BlockReference>(blkRef, true);
                blockReference.TransformBy(srcBlockReference.BlockTransform);
                double rotation = srcBlockReference.Rotation;
                if ((rotation - Math.PI / 2) > ThBConvertCommon.radian_tolerance && 
                    (rotation - Math.PI * 3 / 2) <= ThBConvertCommon.radian_tolerance)
                {
                    blockReference.TransformBy(Matrix3d.Rotation(Math.PI, Vector3d.ZAxis, blockReference.Position));
                }
            }
        }
    }
}
