using System;
using System.Collections.Generic;

using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPElectrical.BlockConvert.Model;

namespace ThMEPElectrical.BlockConvert
{
    public class ThTCHBConvertEngine : IDisposable
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public ObjectId Insert(string name, Scale3d scale, ThTCHElementData tchElementData)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                return acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                    "0",
                    name,
                    Point3d.Origin,
                    scale,
                    0.0,
                    new Dictionary<string, string>());
            }
        }

        public void Rotate(ThBlockReferenceData targetBlockData, ThTCHElementData tchElementData, ThBlockConvertBlock convertRule)
        {
            using (var acadDatabase = AcadDatabase.Use(targetBlockData.Database))
            {
                var blockReference = acadDatabase.Element<BlockReference>(targetBlockData.ObjId, true);
                var position = tchElementData.Position;
                var rotation = tchElementData.Rotation;
                if (convertRule.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_ROTATION_CORRECT].Equals(false))
                {
                    blockReference.TransformBy(Matrix3d.Rotation(rotation, Vector3d.ZAxis, position));
                }
                else
                {
                    if (rotation > Math.PI / 2 && rotation < Math.PI * 3 / 2)
                    {
                        blockReference.TransformBy(Matrix3d.Rotation(rotation - Math.PI, Vector3d.ZAxis, position));
                    }
                    else
                    {
                        blockReference.TransformBy(Matrix3d.Rotation(rotation, Vector3d.ZAxis, position));
                    }
                }

                targetBlockData.Position = blockReference.Position;
                targetBlockData.Rotation = targetBlockData.ObjId.GetBlockRotation();
            }
        }

        public void Displacement(ThBlockReferenceData targetBlockData, ThTCHElementData tchElementData)
        {
            using (var acadDatabase = AcadDatabase.Use(targetBlockData.Database))
            {
                var blockReference = acadDatabase.Element<BlockReference>(targetBlockData.ObjId, true);
                var targetBlockDataPosition = targetBlockData.GetNewBasePoint(false);
                var srcBlockDataPosition = tchElementData.Position.TransformBy(tchElementData.OwnerSpace2WCS);
                var offset = targetBlockDataPosition.GetVectorTo(srcBlockDataPosition);
                blockReference.TransformBy(Matrix3d.Displacement(offset));

                // Z值归零
                blockReference.ProjectOntoXYPlane();
                targetBlockData.Position = blockReference.Position;
            }
        }

        public void SetDatabaseProperties(ThBlockReferenceData targetBlockData, ObjectId objId, string layer)
        {
            using (var acadDatabase = AcadDatabase.Use(targetBlockData.Database))
            {
                if (ThBConvertDbUtils.UpdateLayerSettings(layer))
                {
                    var block = acadDatabase.Element<Entity>(objId, true);
                    block.Layer = layer;
                }
            }
        }
    }
}
