using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.HydrantConnectPipe.Engine
{
    public class ThFireHydrantPipeExtractionEngine : ThDistributionElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var waterWellVisitor = new ThHydrantPipeExtractionVisitor();
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(waterWellVisitor);
            extractor.Extract(database); //从块和外参里提取元素
            Results.AddRange(waterWellVisitor.Results);
        }

        public override void ExtractFromMS(Database database)
        {
            var waterWellVisitor = new ThHydrantPipeExtractionVisitor();
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(waterWellVisitor);
            extractor.ExtractFromMS(database);//从本图里提取元素
            Results.AddRange(waterWellVisitor.Results);
        }
    }
    public class ThHydrantPipeRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public List<ThRawIfcDistributionElementData> Datas { get; set; }
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var extractionEngine = new ThFireHydrantPipeExtractionEngine();
            extractionEngine.Extract(database);
            Recognize(extractionEngine.Results, polygon);
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var extractionEngine = new ThFireHydrantPipeExtractionEngine();
            extractionEngine.ExtractFromMS(database);
            Recognize(extractionEngine.Results, polygon);
        }
        public override void Recognize(List<ThRawIfcDistributionElementData> datas, Point3dCollection polygon)
        {
            var dbObjs = datas.Select(o => o.Geometry).ToCollection();
            if (polygon.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
            }
            Datas = datas.Where(o => dbObjs.Contains(o.Geometry)).ToList();
            Elements.AddRange(Datas.Select(o => o.Geometry).Cast<Entity>().Select(x => new ThIfcDistributionFlowElement() { Outline = x }));
        }
    }
}
