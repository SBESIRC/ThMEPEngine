using System;
using Linq2Acad;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThParkingStallVisitor : ThDistributionElementExtractionVisitor
    {
        public Func<Entity, bool> CheckQualifiedLayer { get; set; }
        public Func<Entity, bool> CheckQualifiedBlockName { get; set; }
        public ThParkingStallVisitor()
        {
            CheckQualifiedLayer = base.CheckLayerValid;
            CheckQualifiedBlockName = (Entity entity) => true;
        }
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference br)
            {
                HandleBlockReference(elements, br, matrix);
            }
        }

        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj)
        {
            if (dbObj is BlockReference br)
            {
                HandleBlockReference(elements, br);
            }
        }

        public override void DoXClip(List<ThRawIfcDistributionElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !xclip.Contains(o.Geometry as Curve));
            }
        }

        private void HandleBlockReference(List<ThRawIfcDistributionElementData> elements,BlockReference br, Matrix3d matrix)
        {
            var texts = new List<DBText>();
            if (IsDistributionElement(br))
            {
                var rectangle = br.GeometricExtents.ToRectangle();
                rectangle.TransformBy(matrix);
                elements.Add(new ThRawIfcDistributionElementData()
                {
                    Data=br.GetEffectiveName(),
                    Geometry = rectangle
                });                
            }
        }
        private void HandleBlockReference(List<ThRawIfcDistributionElementData> elements, BlockReference br)
        {
            // 获取ModelSpace中BlockReference的OBB
            var texts = new List<DBText>();
            if (IsDistributionElement(br))
            {
                using (AcadDatabase acadDatabase = AcadDatabase.Use(br.Database))
                {
                    var btr = acadDatabase.Blocks.Element(br.BlockTableRecord);
                    var rectangle = btr.GeometricExtents().ToRectangle();
                    rectangle.TransformBy(br.BlockTransform);
                    elements.Add(new ThRawIfcDistributionElementData()
                    {
                        Data = br.GetEffectiveName(),
                        Geometry = rectangle
                    });
                }
            }
        }
        public override bool IsDistributionElement(Entity entity)
        {
            return CheckQualifiedBlockName(entity);
        }

        public override bool CheckLayerValid(Entity curve)
        {
            return CheckQualifiedLayer(curve);
        }
    }
}
