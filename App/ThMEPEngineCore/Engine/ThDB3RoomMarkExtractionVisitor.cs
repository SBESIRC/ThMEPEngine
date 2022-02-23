using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThDB3RoomMarkExtractionVisitor : ThAnnotationElementExtractionVisitor
    {
        private ThAIRoomMarkExtractionVisitor Impl { get; set; }

        public ThDB3RoomMarkExtractionVisitor()
        {
            Impl = new ThAIRoomMarkExtractionVisitor();
        }

        public override void DoExtract(List<ThRawIfcAnnotationElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            Impl.LayerFilter = LayerFilter;
            Impl.DoExtract(elements, dbObj, matrix);
        }

        public override void DoExtract(List<ThRawIfcAnnotationElementData> elements, Entity dbObj)
        {
            Impl.LayerFilter = LayerFilter;
            Impl.DoExtract(elements, dbObj);
        }

        public override void DoXClip(List<ThRawIfcAnnotationElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            Impl.DoXClip(elements, blockReference, matrix);
        }
    }
}
