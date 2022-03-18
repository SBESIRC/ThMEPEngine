﻿using System;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThMEPEngineCore.Engine;

namespace TianHua.Electrical.PDS.Engine
{
    public class ThPDSBlockExtractionEngine : ThDistributionElementExtractionEngine
    {
        public List<string> NameFilter { get; set; }
        public List<string> PropertyFilter { get; set; }
        public List<string> DistBoxKey { get; set; }

        public override void Extract(Database database)
        {
            var visitor = new ThPDSBlockExtractionVisitor()
            {
                NameFilter = NameFilter,
                PropertyFilter = PropertyFilter,
                DistBoxKey = DistBoxKey,
            };
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results = visitor.Results;
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new NotImplementedException();
        }

        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThPDSBlockExtractionVisitor()
            {
                NameFilter = NameFilter,
                PropertyFilter = PropertyFilter,
                DistBoxKey = DistBoxKey,
            };
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.ExtractFromMS(database);
            extractor.Extract(database);
            Results = visitor.Results;
        }
    }
}
