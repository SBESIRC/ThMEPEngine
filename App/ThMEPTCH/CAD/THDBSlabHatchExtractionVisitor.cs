using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.TCHArchDataConvert.THStructureEntity;

namespace ThMEPTCH.CAD
{
    public class THDBSlabHatchExtractionVisitor : ThBuildingElementExtractionVisitor
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
            return e is Hatch;
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
                Hatch hatch = entity as Hatch;
                var pls = Dreambuild.AutoCAD.Algorithms.HatchToPline(hatch);
                if(pls.Count > 0 && pls[0].Area > 500000)
                {
                    results.Add(new ThRawIfcBuildingElementData()
                    {
                        Geometry = entity,
                        Data = CreatStructureEntity(hatch, pls[0]),
                    });
                }
            }
            return results;
        }

        private THStructureSlabHatch CreatStructureEntity(Hatch hatch, Polyline polyline)
        {
            return new THStructureSlabHatch()
            {
                Outline = polyline,
                slabPLType = hatch.Layer == "S_PLAN_HACH" ? SlabType.Slab : SlabType.Hole,
                PatternName= hatch.PatternName,
            };
        }
    }
}
