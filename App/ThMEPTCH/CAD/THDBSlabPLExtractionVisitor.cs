using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.TCHArchDataConvert.THStructureEntity;

namespace ThMEPTCH.CAD
{
    public class THDBSlabPLExtractionVisitor : ThBuildingElementExtractionVisitor
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
            return e is Polyline;
        }

        public override bool CheckLayerValid(Entity e)
        {
            return e.Layer == "S_PLAN_HACH" || e.Layer == "S_HOLE";
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

        private THStructureSlabPL CreatStructureEntity(Entity entity)
        {
            Polyline polyline = entity as Polyline;
            return new THStructureSlabPL()
            {
                Outline = polyline,
                Uuid = polyline.Handle.Value,
                slabPLType = entity.Layer == "S_PLAN_HACH" ? SlabType.Slab : SlabType.Hole,
            };
        }
    }
}
