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
    public class ThFireHydrantExtractionEngine : ThDistributionElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var waterWellVisitor = new ThHydrantExtractionVisitor();
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(waterWellVisitor);
            extractor.Extract(database); //从块和外参里提取元素
            Results.AddRange(waterWellVisitor.Results);
        }

        public override void ExtractFromMS(Database database)
        {
            var waterWellVisitor = new ThHydrantExtractionVisitor();
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(waterWellVisitor);
            extractor.ExtractFromMS(database);
            Results.AddRange(waterWellVisitor.Results);
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new NotSupportedException();
        }
    }

    public class ThHydrantRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public List<ThRawIfcDistributionElementData> Datas { get; set; }
        public ThHydrantRecognitionEngine()
        {
            Datas = new List<ThRawIfcDistributionElementData>();
        }
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var extractionEngine = new ThFireHydrantExtractionEngine();
            extractionEngine.Extract(database);
            Recognize(extractionEngine.Results, polygon);
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var extractionEngine = new ThFireHydrantExtractionEngine();
            extractionEngine.ExtractFromMS(database);
            Recognize(extractionEngine.Results, polygon);
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new NotSupportedException();
        }

        public override void Recognize(List<ThRawIfcDistributionElementData> datas, Point3dCollection polygon)
        {
            //var dbObjs = datas.Select(o => o.Geometry).ToCollection();
            //if (polygon.Count > 0)
            //{
            //    var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            //    dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
            //}
            //Datas = datas.Where(o => dbObjs.Contains(o.Geometry)).ToList();
            //Elements.AddRange(Datas.Select(o => o.Geometry).Cast<Entity>().Select(x => new ThIfcDistributionFlowElement() { Outline = x }));

            var dbObjs = datas.Select(o => o.Geometry).ToCollection();
            if (polygon.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
            }
            datas = datas.Where(o => dbObjs.Contains(o.Geometry)).ToList();
            Datas.AddRange(datas);
            Elements.AddRange(datas.Select(o => o.Geometry).Cast<Entity>().Select(x => new ThIfcDistributionFlowElement() { Outline = x }));
        }
    }
}
