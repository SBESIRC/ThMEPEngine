using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThDB3RoomMarkExtractionVisitor : ThAnnotationElementExtractionVisitor
    {
        private ThRoomMarkExtractionVisitor Impl { get; set; }

        public ThDB3RoomMarkExtractionVisitor()
        {
            Impl = new ThRoomMarkExtractionVisitor();
        }

        public override void DoExtract(List<ThRawIfcAnnotationElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            Impl.DoExtract(elements, dbObj, matrix);
        }

        public override void DoExtract(List<ThRawIfcAnnotationElementData> elements, Entity dbObj)
        {
            Impl.DoExtract(elements, dbObj);
        }

        public override void DoXClip(List<ThRawIfcAnnotationElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            Impl.DoXClip(elements, blockReference, matrix);
        }
    }
}
