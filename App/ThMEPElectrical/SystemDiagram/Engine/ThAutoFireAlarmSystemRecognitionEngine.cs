using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.SystemDiagram.Engine
{
    public class ThAutoFireAlarmSystemExtractionEngine : ThDistributionElementExtractionEngine, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public override void Extract(Database database)
        {
            var visitor = new ThAutoFireAlarmSystemVisitor();
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);    // 提取外参中的块            
            Results = visitor.Results;
        }

        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThAutoFireAlarmSystemVisitor();
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.ExtractFromMS(database);// 提取ModelSpace下的块
            Results = visitor.Results;
        }
    }
    public class ThAutoFireAlarmSystemRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        private Dictionary<Entity, List<KeyValuePair<string, string>>> BlockAttInfo { get; set; }
        public ThAutoFireAlarmSystemRecognitionEngine()
        {
            BlockAttInfo = new Dictionary<Entity, List<KeyValuePair<string, string>>>();
        }
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThAutoFireAlarmSystemExtractionEngine();
            engine.Extract(database);
            var originDatas = engine.Results;
            if (polygon.Count > 0)
            {
                var dbObjs = engine.Results.Select(o => o.Geometry).ToCollection();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                originDatas = originDatas.Where(o => dbObjs.Contains(o.Geometry)).ToList();
            }
            originDatas.ForEach(o => BlockAttInfo.Add(o.Geometry, (o.Data as ElementInfo).AttNames));
            // 通过获取的OriginData 分类
            Elements.AddRange(originDatas.Select(x => new ThIfcDistributionFlowElement() { Outline = x.Geometry }));
        }
        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var engine = new ThAutoFireAlarmSystemExtractionEngine();
            engine.ExtractFromMS(database);
            var originDatas = engine.Results;
            if (polygon.Count > 0)
            {
                var dbObjs = engine.Results.Select(o => o.Geometry).ToCollection();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                originDatas = originDatas.Where(o => dbObjs.Contains(o.Geometry)).ToList();
            }
            originDatas.ForEach(o => BlockAttInfo.Add(o.Geometry, (o.Data as ElementInfo).AttNames));
            // 通过获取的OriginData 分类
            Elements.AddRange(originDatas.Select(x => new ThIfcDistributionFlowElement() { Outline = x.Geometry }));
        }
        public Dictionary<Entity, List<KeyValuePair<string, string>>> QueryAllOriginDatas()
        {
            return BlockAttInfo;
        }

        public List<KeyValuePair<string, string>> QueryOriginDatas(ThIfcDistributionFlowElement element)
        {
            return BlockAttInfo.First(o => o.Key.Equals(element.Outline)).Value;
        }
    }
}
