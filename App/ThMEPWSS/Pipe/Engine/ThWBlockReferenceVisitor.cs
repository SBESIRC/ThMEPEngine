using System;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Service;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWBlockReferenceVisitor : ThDistributionElementExtractionVisitor
    {
        public override void DoExtract(Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference blkref)
            {
                HandleBlockReference(blkref, matrix);
            }
        }

        public override void DoXClip(BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                Results.RemoveAll(o => !IsContain(xclip, o.Geometry));
            }
        }

        public override bool IsDistributionElement(Entity entity)
        {
            if (entity is BlockReference reference)
            {
                var name = reference.GetEffectiveName();
                if (ThRainPipeLayerManager.IsRainPipeBlockName(name))
                {
                    return true;
                }
                if (ThRoofRainPipeLayerManager.IsRoofPipeBlockName(name))
                {
                    return true;
                }
                if (ThCondensePipeLayerManager.IsCondensePipeBlockName(name))
                {
                    return true;
                }
                if (ThWashMachineLayerManager.IsWashmachineBlockName(name))
                {
                    return true;
                }
                if (ThBasintoolLayerManager.IsBasintoolBlockName(name))
                {
                    return true;
                }
                if (ThFloorDrainLayerManager.IsToiletFloorDrainBlockName(name))
                {
                    return true;
                }
                if (ThFloorDrainLayerManager.IsBalconyFloorDrainBlockName(name))
                {
                    return true;
                }
                if (ThClosestoolLayerManager.IsClosetoolBlockName(name))
                {
                    return true;
                }         
            }
            return false;
        }

        public override bool CheckLayerValid(Entity curve)
        {
            return true;
        }

        private void HandleBlockReference(BlockReference blkref, Matrix3d matrix)
        {
            Results.Add(new ThRawIfcDistributionElementData()
            {
                Data = blkref.GetEffectiveName(),
                Geometry = blkref.GetTransformedCopy(matrix),
            });
        }

        private bool IsContain(ThMEPXClipInfo xclip, Entity ent)
        {
            if (ent is BlockReference br)
            {
                //TODO: 获取块的OBB
                return xclip.Contains(br.GeometricExtents.ToRectangle());
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
