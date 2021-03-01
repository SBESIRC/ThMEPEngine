using System;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;


namespace ThMEPWSS.Pipe.Engine
{
 public  class ThWRoofRainPipeExtractionVisitor : ThDistributionElementExtractionVisitor
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
            if (entity is BlockReference blkref)
            {
                var name = blkref.GetEffectiveName();
                return ThRoofRainPipeLayerManager.IsRoofPipeBlockName(name);
            }
            return false;
        }

        private void HandleBlockReference(BlockReference blkref, Matrix3d matrix)
        {
            Results.Add(new ThRawIfcDistributionElementData()
            {
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
