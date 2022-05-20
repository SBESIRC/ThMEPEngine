using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using ThMEPEngineCore.Engine;

namespace ThMEPWSS.Sprinkler.Data
{
    public class ThWaterFlowIndicatorEngine : ThDistributionElementExtractionEngine
    {
        public List<string> NameFilter { get; set; }

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
            var visitor = new ThWaterFlowIndicatorExtractionVisitor()
            {
                NameFilter = NameFilter,
            };
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.ExtractFromMS(database);
            Results = visitor.Results;
        }
    }
}
