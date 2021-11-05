using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.SystemDiagram.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.SystemDiagram.Engine
{
    public class ThRequiredElementExtractionEngine : ThDistributionElementExtractionEngine, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public override void Extract(Database database)
        {
            var visitor = new ThRequiredElementVisitor();
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);    // 提取外参中的块            
            Results=visitor.Results;
        }

        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThRequiredElementVisitor();
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);           
            extractor.ExtractFromMS(database);// 提取ModelSpace下的块
            Results=visitor.Results;
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new NotSupportedException();
        }
    }
    public class ThRequiredElementRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThRequiredElementExtractionEngine();
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
            var engine = new ThRequiredElementExtractionEngine();
            engine.ExtractFromMS(database);
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
        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new NotSupportedException();
        }
    }
}
