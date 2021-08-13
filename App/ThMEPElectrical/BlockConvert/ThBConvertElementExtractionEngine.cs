using System;
using ThMEPEngineCore.Engine;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertElementExtractionEngine : ThDistributionElementExtractionEngine
    {
        public List<string> NameFilter { get; set; }

        public override void Extract(Database database)
        {
            var visitor = new ThBConvertElementExtractionVisitor()
            {
                NameFilter = NameFilter,
            };
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);   
            Results = visitor.Results;
        }

        public override void ExtractFromMS(Database database)
        {
            throw new NotImplementedException();
        }
    }
}
