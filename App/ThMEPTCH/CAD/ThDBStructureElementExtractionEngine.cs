using System;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.TCHArchDataConvert.THStructureEntity;

namespace ThMEPTCH.CAD
{
    public class ThDBStructureElementExtractionEngine : ThBuildingElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            throw new NotImplementedException();
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new NotImplementedException();
        }

        public override void ExtractFromMS(Database database)
        {
            //墙,柱,梁,板
            var visitors = new ThBuildingElementExtractionVisitor[]{
                new THDBWallExtractionVisitor(),
                new THDBColumnExtractionVisitor(),
                new THDBBeamExtractionVisitor(),
                new THDBMarkExtractionVisitor(),
                new THDBSlabPLExtractionVisitor(),
                new THDBSlabHatchExtractionVisitor(),
                new THDBSlabBTHExtractionVisitor(),
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitors);
            extractor.Extract(database);
            extractor.ExtractFromMS(database);
            Results = new List<ThRawIfcBuildingElementData>();
            visitors.ForEach(v => Results.AddRange(v.Results));

            //板
        }
    }
}
