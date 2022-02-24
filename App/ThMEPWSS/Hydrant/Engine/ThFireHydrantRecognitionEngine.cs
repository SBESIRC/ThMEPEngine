using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPWSS.Model;

namespace ThMEPWSS.Hydrant.Engine
{
    public class ThFireHydrantExtractionEngine : ThDistributionElementExtractionEngine
    {
        private ThFireHydrantExtractionVisitor Visitor { get; set; }
        public ThFireHydrantExtractionEngine(ThFireHydrantExtractionVisitor visitor)
        {
            Visitor = visitor;
        }
        public override void Extract(Database database)
        {
            var service = new ThDistributionElementExtractor();
            service.Accept(Visitor);
            service.Extract(database);
            Results.AddRange(Visitor.Results);
        }

        public override void ExtractFromMS(Database database)
        {
            var service = new ThDistributionElementExtractor();
            service.Accept(Visitor);
            service.ExtractFromMS(database);
            Results.AddRange(Visitor.Results);
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new NotSupportedException();
        }
    }
    public class ThFireHydrantRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        private ThFireHydrantExtractionVisitor Visitor { get; set; }
        public ThFireHydrantRecognitionEngine(ThFireHydrantExtractionVisitor visitor)
        {
            Visitor = visitor;
        }
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            Visitor.Results = new List<ThRawIfcDistributionElementData>();
            var extractEngine = new ThFireHydrantExtractionEngine(Visitor);
            extractEngine.Extract(database);
            Recognize(extractEngine.Results, polygon);
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            Visitor.Results = new List<ThRawIfcDistributionElementData>();
            var extractEngine = new ThFireHydrantExtractionEngine(Visitor);
            extractEngine.ExtractFromMS(database);
            Recognize(extractEngine.Results, polygon);
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new NotSupportedException();
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
            Elements.AddRange(originDatas.Select(x => new ThFireHydrant() { Outline = x.Geometry }));
        }
        public void Recognize(DBObjectCollection datas, Point3dCollection polygon)
        {
            var originDatas = new List<Entity>();
            if (polygon.Count > 0)
            {
                var dbObjs = datas;
                var spatialIndex = new ThCADCoreNTSSpatialIndex(datas);
                dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                originDatas = dbObjs.Cast<Entity>().ToList();
            }
            else
            {
                originDatas = datas.Cast<Entity>().ToList();
            }
            Elements.AddRange(originDatas.Select(x => new ThFireHydrant() { Outline = x.GeometricExtents.ToRectangle() }));
        }
    }
}
