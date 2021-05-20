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

namespace ThMEPElectrical.SystemDiagram.Engine
{
    public class ThAutoFireAlarmSystemExtractionEngine : ThDistributionElementExtractionEngine,IDisposable
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
            extractor.Extract(database);
            Results = visitor.Results;
        }

        public override void ExtractFromMS(Database database)
        {
            throw new NotImplementedException();
        }
    }
    public class ThAutoFireAlarmSystemRecognitionEngine : ThDistributionElementRecognitionEngine
    {
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

            // 通过获取的OriginData 分类
            Elements.AddRange(originDatas.Select(x => new ThIfcDistributionFlowElement() { Outline = x.Geometry }));
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }
    }
}
