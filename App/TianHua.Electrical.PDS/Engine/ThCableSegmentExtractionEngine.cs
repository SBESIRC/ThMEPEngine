﻿using System;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThMEPEngineCore.Engine;
using TianHua.Electrical.PDS.Service;

namespace TianHua.Electrical.PDS.Engine
{
    public class ThCableSegmentExtractionEngine : ThFlowSegmentExtractionEngine
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
            var visitor = new ThCableSegmentExtractionVisitor
            {
                LayerFilter = ThPDSLayerService.CableLayers(),
            };
            var extractor = new ThFlowSegmentExtractor();
            extractor.Accept(visitor);
            extractor.ExtractFromMS(database);
            Results = visitor.Results;
        }
    }
}
