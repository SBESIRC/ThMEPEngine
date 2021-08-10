using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThCADExtension;

namespace ThMEPEngineCore.Engine
{
    public class ThDB3StairExtractionVisitor : ThBuildingElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference br)
            {
                elements.AddRange(Handle(br, matrix));
            }
        }

        public override void DoXClip(List<ThRawIfcBuildingElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid && elements.Count != 0)
            {
                //xclip.TransformBy(matrix);
                //elements.RemoveAll(o => !xclip.Contains(o.Geometry as Curve));
            }
        }

        public override bool IsBuildElement(Entity entity)
        {
            return ThMEPDB3ComponentUtils.IsStair(entity);
        }

        public override bool IsBuildElementBlockReference(BlockReference blockReference)
        {
            return ThMEPDB3ComponentUtils.IsStair(blockReference);
        }

        public override bool CheckLayerValid(Entity curve)
        {
            var layer = curve.LayerId.GetObject(OpenMode.ForRead) as LayerTableRecord;
            return !layer.IsFrozen && !layer.IsOff && !layer.IsHidden;
        }

        private List<ThRawIfcBuildingElementData> Handle(BlockReference br, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (CheckLayerValid(br))
            {
                results.Add(new ThRawIfcBuildingElementData()
                {
                    Data = new ThBlockReferenceData(br.ObjectId, matrix),
                    Geometry = br.GetTransformedCopy(matrix),
                });
            }
            return results;
        }
    }
}
