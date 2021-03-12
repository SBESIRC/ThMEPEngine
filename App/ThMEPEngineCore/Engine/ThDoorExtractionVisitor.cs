using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThDoorExtractionVisitor : ThBuildingElementExtractionVisitor
    {
        private ThDoorMarkExtractionVisitor MarkVisitor { get; set; }
        private ThDoorStoneExtractionVisitor StoneVisitor { get; set; }

        public ThDoorExtractionVisitor()
        {
            MarkVisitor = new ThDoorMarkExtractionVisitor();
            StoneVisitor = new ThDoorStoneExtractionVisitor();
        }

        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            MarkVisitor.DoExtract(elements, dbObj, matrix);
            StoneVisitor.DoExtract(elements, dbObj, matrix);
        }

        public override void DoXClip(List<ThRawIfcBuildingElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            MarkVisitor.DoXClip(elements, blockReference, matrix);
            StoneVisitor.DoXClip(elements, blockReference, matrix);
        }
    }
}
