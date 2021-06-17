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

namespace ThMEPElectrical.SystemDiagram.Engine
{
    public class ThControlCircuitExtractionEngine : ThEntityCommonExtractionEngine
    {
        public List<string> LayerFilter { get; set; }

        public ThControlCircuitExtractionEngine()
        {
            LayerFilter = new List<string>();
        }
        public override void Extract(Database database)
        {
            throw new NotImplementedException();
        }

        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThControlCircuitVisitor()
            {
                LayerFilter = this.LayerFilter,
            };
            var extractor = new ThEntityCommonExtractor();
            extractor.Accept(visitor);
            extractor.ExtractFromMS(database);    // 提取MS
            Results = visitor.Results;
        }
    }

    public class ThControlCircuitRecognitionEngine : ThEntityCommonRecognitionEngine
    {
        public List<string> LayerFilter { get; set; }
        //ThAutoFireAlarmSystemRecognitionEngine BlockReferenceEngine;
        public ThControlCircuitRecognitionEngine()
        {
            //BlockReferenceEngine = new ThAutoFireAlarmSystemRecognitionEngine();
            LayerFilter = new List<string>();
        }

        public override void Recognize(Database database, Point3dCollection polygon)
        {
            //BlockReferenceEngine.Recognize(database, polygon);
            throw new NotImplementedException();
        }
        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            //BlockReferenceEngine.RecognizeMS(database, polygon);
            //BlockReferenceEngine.Elements.ForEach(data => Elements.Add(new ThEntityData() { Geometry = data.Outline }));

            var engine = new ThControlCircuitExtractionEngine()
            {
                LayerFilter = this.LayerFilter,
            };
            engine.ExtractFromMS(database);
            Recognize(engine.Results, polygon);
        }
        public override void Recognize(List<ThEntityData> datas, Point3dCollection polygon)
        {
            var originDatas = datas;
            if (polygon.Count > 0)
            {
                var dbObjs = datas.Select(o => o.Geometry).ToCollection();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                originDatas = originDatas.Where(o => dbObjs.Contains(o.Geometry)).ToList();
            }
            // 通过获取的OriginData 分类
            Elements.AddRange(originDatas);
        }
    }
}
