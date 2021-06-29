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
    public  class ThAutoFireAlarmSystemVisitor : ThDistributionElementExtractionVisitor
    {
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
            return !(entity as BlockReference).Name.Contains('$');
        }

        private void HandleBlockReference(List<ThRawIfcDistributionElementData> elements, BlockReference blkref, Matrix3d matrix)
        {
            if (IsDistributionElement(blkref) && CheckLayerValid(blkref) && IsDistributeElementBlock(blkref))
            {
                var dic = blkref.Id.GetAttributesInBlockReferenceEx();
                var info = new ElementInfo()
                {
                    Layer = blkref.Layer,
                    Name = blkref.Name,
                    AttNames = dic
                };
                elements.Add(new ThRawIfcDistributionElementData()
                {
                    Data = info,
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
            return curve.Layer.Contains("E-");
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
    public class ElementInfo
    {
        public string Layer { get; set; }
        public string Name { get; set; }
        public List<KeyValuePair<string, string>> AttNames { get; set; }
    }
}
