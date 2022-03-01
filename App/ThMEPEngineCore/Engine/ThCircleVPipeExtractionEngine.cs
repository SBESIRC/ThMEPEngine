using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThCircleVPipeExtractionEngine : ThFlowSegmentExtractionEngine
    {
        public List<string> LayerFilter { get; set; } = new List<string>();
        public List<double> Radius { get; set; } = new List<double>();

        public override void Extract(Database database)
        {
            throw new System.NotImplementedException();
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new System.NotImplementedException();
        }

        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThCircleVPipeExtractionVisitor()
            {
                LayerFilter = LayerFilter,
                Radius = Radius,
            };
            var extractor = new ThFlowSegmentExtractor();
            extractor.Accept(visitor);
            extractor.ExtractFromMS(database);
            Results.AddRange(visitor.Results);
        }

    }
}
