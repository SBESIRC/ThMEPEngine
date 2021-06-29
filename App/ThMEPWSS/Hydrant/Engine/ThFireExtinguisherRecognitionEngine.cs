using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Hydrant.Engine
{
    public class ThFireExtinguisherExtractionEngine : ThDistributionElementExtractionEngine
    {
        private ThFireExtinguisherExtractionVisitor Visitor { get; set; }
        public ThFireExtinguisherExtractionEngine(ThFireExtinguisherExtractionVisitor visitor)
        {
            Visitor = visitor;
        }
        public override void Extract(Database database)
        {
            var service = new ThBlockElementExtractor()
            {
                FindExternalReference = false,
            };
            service.Accept(Visitor);
            service.Extract(database);
            Results.AddRange(Visitor.Results);
        }

        public override void ExtractFromMS(Database database)
        {
            var service = new ThBlockElementExtractor()
            {
                FindExternalReference = false,
            };
            service.Accept(Visitor);
            service.ExtractFromMS(database);
            Results.AddRange(Visitor.Results);
        }
    }
    public class ThFireExtinguisherRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        private ThFireExtinguisherExtractionVisitor Visitor { get; set; }
        public ThFireExtinguisherRecognitionEngine(ThFireExtinguisherExtractionVisitor visitor)
        {
            Visitor = visitor;
        }
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var extractEngine = new ThFireExtinguisherExtractionEngine(Visitor);
            extractEngine.Extract(database);
            Recognize(extractEngine.Results, polygon);
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var extractEngine = new ThFireExtinguisherExtractionEngine(Visitor);
            extractEngine.ExtractFromMS(database);
            Recognize(extractEngine.Results, polygon);
        }
        public override void Recognize(List<ThRawIfcDistributionElementData> datas, Point3dCollection polygon)
        {
            var originDatas = datas;
            if (polygon.Count > 0)
            {
                var dbObjs = datas.Select(o => o.Geometry).ToCollection();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                originDatas = originDatas.Where(o => dbObjs.Contains(o.Geometry)).ToList();
            }
            Elements.AddRange(originDatas.Select(x => new ThIfcDistributionFlowElement() { Outline = x.Geometry }));
        }
    }
}
