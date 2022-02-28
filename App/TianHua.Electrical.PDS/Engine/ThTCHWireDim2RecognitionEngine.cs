using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThMEPEngineCore.Engine;

namespace TianHua.Electrical.PDS.Engine
{
    public class ThTCHWireDim2ExtractionEngine : ThAnnotationElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            throw new NotImplementedException();
        }

        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThTCHWireDim2ExtractionVisitor();
            var extractor = new ThAnnotationElementExtractor();
            extractor.Accept(visitor);
            extractor.ExtractFromMS(database);
            Results = visitor.Results;
        }
    }

    public class ThTCHWireDim2RecognitionEngine : ThAnnotationElementRecognitionEngine
    {
        public List<Entity> Results { get; protected set; } = new List<Entity> ();

        public override void Recognize(Database database, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }

        public override void Recognize(List<ThRawIfcAnnotationElementData> datas, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var engine = new ThTCHWireDim2ExtractionEngine();
            engine.ExtractFromMS(database);
            Results = engine.Results.Select(o => o.Data as Entity).ToList();
        }
    }
}
