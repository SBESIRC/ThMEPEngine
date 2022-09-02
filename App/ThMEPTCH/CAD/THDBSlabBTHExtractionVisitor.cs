using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.TCHArchDataConvert.THStructureEntity;
using ThCADExtension;

namespace ThMEPTCH.CAD
{
    public class THDBSlabBTHExtractionVisitor : ThBuildingElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            elements.AddRange(HandleElement(dbObj, matrix));
        }

        public override void DoXClip(List<ThRawIfcBuildingElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            //throw new NotImplementedException();
        }

        public override bool IsBuildElement(Entity e)
        {
            return !e.Is95PercentStructureElement() && e is BlockReference;
        }

        public override bool CheckLayerValid(Entity e)
        {
            return (e as BlockReference).Name == "B-th";
        }

        private List<ThRawIfcBuildingElementData> HandleElement(Entity entity, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(entity) && CheckLayerValid(entity))
            {
                results.Add(new ThRawIfcBuildingElementData()
                {
                    Geometry = entity,
                    Data = CreatStructureEntity(entity),
                });
            }
            return results;
        }

        private THStructureSlabBTH CreatStructureEntity(Entity entity)
        {
            BlockReference blk = entity as BlockReference;
            return new THStructureSlabBTH()
            {
                Uuid = blk.Handle.Value,
                Point = blk.Position,
                Height = double.Parse(blk.ObjectId.GetAttributesInBlockReferenceEx()[0].Value),
            };
        }
    }
}
