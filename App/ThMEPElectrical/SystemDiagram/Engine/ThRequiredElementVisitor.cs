using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPElectrical.SystemDiagram.Model;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using DotNetARX;
using Linq2Acad;

namespace ThMEPElectrical.SystemDiagram.Engine
{
    public class ThRequiredElementVisitor : ThDistributionElementExtractionVisitor
    {
        private List<string> BlockNameSet = new List<string>() { "E-BFAS011", "E-BFAS010", "E-BFAS520", "E-BDB004", "E-BFAS630-3", "E-BFAS540", "E-BFAS621-2", "E-BFAS550", "E-BFAS510", "E-BFAS510-3", "E-BFAS520-3", "E-BFAS621-3" , "E-BFAS732", "E-BFAS522" };
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference blkref)
            {
                HandleBlockReference(elements, blkref, matrix);
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
            BlockReference blkref = entity as BlockReference;
            return BlockNameSet.Contains(blkref.Name) ||
                blkref.Id.GetAttributesInBlockReferenceEx().Any(o => o.Value == "SI");
        }

        private void HandleBlockReference(List<ThRawIfcDistributionElementData> elements, BlockReference blkref, Matrix3d matrix)
        {
            if (IsDistributionElement(blkref) && CheckLayerValid(blkref) && IsDistributeElementBlock(blkref))
            {
                elements.Add(new ThRawIfcDistributionElementData()
                {
                    Data = "",
                    Geometry = blkref.GetTransformedCopy(matrix),
                });
            }
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
        public override bool CheckLayerValid(Entity curve)
        {
            return true;
        }
        
        private bool IsDistributeElementBlock(BlockReference blkref)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blkref.Database))
            {
                var blockTableRecord = acadDatabase.Blocks.Element(blkref.BlockTableRecord);
                return base.IsBuildElementBlock(blockTableRecord);
            }
        }
    }
}
