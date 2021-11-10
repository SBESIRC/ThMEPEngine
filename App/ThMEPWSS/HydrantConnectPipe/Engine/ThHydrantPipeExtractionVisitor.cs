using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;

namespace ThMEPWSS.HydrantConnectPipe.Engine
{
    public class ThHydrantPipeExtractionVisitor : ThDistributionElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference blkref)
            {
                HandleBlockReference(elements, blkref, matrix);
            }
            else if(dbObj is Circle circle)
            {
                HandleCircle(elements, circle, matrix);
            }
        }

        public override void DoXClip(List<ThRawIfcDistributionElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !IsContain(xclip, o.Geometry));
            }
        }

        public override bool IsDistributionElement(Entity entity)
        {
            if (entity is BlockReference reference)
            {
                if (reference.GetEffectiveName().Contains("带定位立管"))
                {
                    return true;
                }
            }
            else if(entity is Circle)
            {
                if(entity.Layer == "W-FRPT-HYDT-EQPM" || entity.Layer == "W-FRPT-HYDT" || entity.Layer == "W-FRPT-EXTG")
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

        private void HandleBlockReference(List<ThRawIfcDistributionElementData> elements, BlockReference blkref, Matrix3d matrix)
        {
            if(blkref.Bounds.HasValue)
            {
                var outline = blkref.ToOBB(blkref.BlockTransform.PreMultiplyBy(matrix));
                elements.Add(new ThRawIfcDistributionElementData()
                {
                    Data = outline,
                    Geometry = blkref.GetTransformedCopy(matrix),
                });
            }            
        }

        private void HandleCircle(List<ThRawIfcDistributionElementData> elements, Circle circle, Matrix3d matrix)
        {
            var clone = circle.GetTransformedCopy(matrix) as Circle;
            elements.Add(new ThRawIfcDistributionElementData()
            {
                Geometry = clone,
                Data = clone.ToRectangle()
        }) ;
        }

        private bool IsContain(ThMEPXClipInfo xclip, Entity ent)
        {
            if (ent is BlockReference br)
            {
                //TODO: 获取块的OBB
                return xclip.Contains(br.GeometricExtents.ToRectangle());
            }
            else if (ent is Circle circle)
            {
                return xclip.Contains(circle.ToRectangle());
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            // 忽略图纸空间和匿名块
            if (blockTableRecord.IsLayout)
            {
                return false;
            }

            // 忽略不可“炸开”的块
            if (!blockTableRecord.Explodable)
            {
                return false;
            }

            return true;
        }
    }
}
