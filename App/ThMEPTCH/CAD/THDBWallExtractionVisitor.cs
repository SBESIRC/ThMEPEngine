using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.TCHArchDataConvert.THStructureEntity;

namespace ThMEPTCH.CAD
{
    public class THDBWallExtractionVisitor : ThBuildingElementExtractionVisitor
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
            return !e.Is95PercentStructureElement() && e is Polyline;
        }

        public override bool CheckLayerValid(Entity e)
        {
            return e.Layer == "S_WALL";
        }

        private List<ThRawIfcBuildingElementData> HandleElement(Entity entity, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(entity) && CheckLayerValid(entity))
            {
                results.Add(new ThRawIfcBuildingElementData()
                {
                    Geometry = entity.GetTransformedCopy(matrix),
                    Data = CreatStructureEntity(entity, matrix),
                });
            }
            return results;
        }

        private THStructureWall CreatStructureEntity(Entity entity, Matrix3d matrix)
        {
            Polyline polyline = entity.GetTransformedCopy(matrix) as Polyline;
            return new THStructureWall()
            {
                Outline = polyline,
                Uuid = polyline.Handle.Value,
            };
        }
    }
}
