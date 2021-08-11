using System;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.Stair
{
    public class ThStairEngine : IDisposable
    {
        public void Dispose()
        {
            //
        }

        public ObjectId Insert(string name, Scale3d scale)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                return acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                    "0",
                    name,
                    Point3d.Origin,
                    scale,
                    0.0 );
            }
        }

        public void Displacement(ObjectId blkRef, List<Point3d> points)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blkRef.Database))
            {
                var blockReference = acadDatabase.Element<BlockReference>(blkRef, true);
                var targetBlockData = new ThBlockReferenceData(blkRef);
                var targetMCS2WCS = targetBlockData.BlockTransform.PreMultiplyBy(targetBlockData.OwnerSpace2WCS);
                var targetBlockDataPosition = Point3d.Origin.TransformBy(targetMCS2WCS);

                // 平台中心布置
                var position = new Vector3d();
                if (targetBlockData.EffectiveName == "E-BL302")
                {
                    position = (points[0].GetAsVector() + points[1].GetAsVector()
                                + points[2].GetAsVector() + points[3].GetAsVector()) / points.Count();
                }
                // 平台靠近下行方向1/4位置中心布置
                else if (targetBlockData.EffectiveName == "E-BFEL800")
                {
                    position = points[0].GetAsVector() + GetVector(points[0], points[1]) / 4
                               + GetVector(points[0], points[3]) / 2;
                }
                // 平台靠近上行方向1/4位置中心布置
                else if (targetBlockData.EffectiveName == "E-BFAS110")
                {
                    position = points[0].GetAsVector() + GetVector(points[0], points[1]) * 3 / 4
                               + GetVector(points[0], points[3]) / 2;
                }
                var offset = position - targetBlockDataPosition.GetAsVector();
                blockReference.TransformBy(Matrix3d.Displacement(offset));
            }
        }

        private Vector3d GetVector(Point3d head, Point3d tail)
        {
            return (tail.GetAsVector() - head.GetAsVector());
        }
    }
}
