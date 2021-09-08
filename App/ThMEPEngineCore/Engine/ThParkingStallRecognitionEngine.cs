using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThParkingStallExtractionEngine : ThDistributionElementExtractionEngine
    {
        public ThParkingStallExtractionVisitor Visitor { get; set; }
        public ThParkingStallExtractionEngine()
        {
            Visitor = new ThParkingStallExtractionVisitor();
        }
        public override void Extract(Database database)
        {
            if (Visitor.LayerFilter.Count == 0)
            {
                Visitor.LayerFilter = ThParkingStallLayerManager.XrefLayers(database).ToHashSet();
            }
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(Visitor);
            extractor.Extract(database);
            Results.AddRange(Visitor.Results);
        }

        public override void ExtractFromMS(Database database)
        {
            if (Visitor.LayerFilter.Count == 0)
            {
                Visitor.LayerFilter = ThParkingStallLayerManager.XrefLayers(database).ToHashSet();
            }
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(Visitor);
            extractor.ExtractFromMS(database);
            Results.AddRange(Visitor.Results);
        }
    }
    public class ThParkingStallRecognitionEngine : ThSpatialElementRecognitionEngine
    {
        public ThParkingStallExtractionVisitor Visitor { get; set; }
        public ThParkingStallRecognitionEngine()
        {
            Visitor = new ThParkingStallExtractionVisitor();
        }
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            if (Visitor.LayerFilter.Count == 0)
            {
                Visitor.LayerFilter = ThParkingStallLayerManager.XrefLayers(database).ToHashSet();
            }
            var engine = new ThParkingStallExtractionEngine()
            {
                Visitor = this.Visitor,
            };
            engine.Extract(database);
            Recognize(Transfer(engine.Results), polygon);
        }
        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            if (Visitor.LayerFilter.Count == 0)
            {
                Visitor.LayerFilter = ThParkingStallLayerManager.XrefLayers(database).ToHashSet();
            }
            var engine = new ThParkingStallExtractionEngine()
            {
                Visitor = this.Visitor,
            };
            engine.ExtractFromMS(database);
            Recognize(Transfer(engine.Results), polygon);
        }

        public override void RecognizeMS(Database database, ObjectIdCollection dbObjs)
        {
            throw new System.NotImplementedException();
        }

        private List<ThRawIfcSpatialElementData> Transfer(List<ThRawIfcDistributionElementData> datas)
        {
            var results = new List<ThRawIfcSpatialElementData>();
            datas.ForEach(o =>
            {
                results.Add(new ThRawIfcSpatialElementData()
                {
                    Data=o.Data,
                    Geometry=o.Geometry
                });
            });
            return results;
        }

        public override void Recognize(List<ThRawIfcSpatialElementData> datas, Point3dCollection polygon)
        {
            var results = new List<ThRawIfcSpatialElementData>();
            var objs = datas.Select(o => o.Geometry).ToCollection();
            var center = polygon.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            transformer.Transform(objs);
            var newPts = transformer.Transform(polygon);
            if (newPts.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                objs = spatialIndex.SelectCrossingPolygon(newPts);                
            }
            transformer.Reset(objs);
            objs.Cast<Entity>().ForEach(o =>
            {
                if (o is Polyline polyline && polyline.Area > 0.0)
                {
                    Elements.Add(ThIfcParkingStall.Create(polyline));
                }
            });
        }
    }
}
