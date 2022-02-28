using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Engine
{
    public class ThFireCompartmentOutlineExtractionEngine : ThSpatialElementExtractionEngine
    {
        public List<string> LayerFilter { get; set; }

        public ThFireCompartmentOutlineExtractionEngine()
        {
            LayerFilter = new List<string>();
        }

        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThFireCompartmentOutlineExtractionVisitor()
            {
                LayerFilter = this.LayerFilter,
            };
            var extractor = new ThSpatialElementExtractor();
            extractor.Accept(visitor);
            extractor.ExtractFromMS(database);
            Results = visitor.Results;
        }

        public override void ExtractFromMS(Database database, ObjectIdCollection dbObjs)
        {
            var visitor = new ThFireCompartmentOutlineExtractionVisitor()
            {
                LayerFilter = this.LayerFilter,
            };
            var extractor = new ThSpatialElementExtractor();
            extractor.Accept(visitor);
            extractor.ExtractFromMS(database, dbObjs);
            Results = visitor.Results;
        }

        public override void Extract(Database database)
        {
            var visitor = new ThAIRoomOutlineExtractionVisitor()
            {
                LayerFilter = this.LayerFilter,
            };
            var extractor = new ThSpatialElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results = visitor.Results;
        }
    }


    public class ThFireCompartmentOutlineRecognitionEngine : ThSpatialElementRecognitionEngine
    {
        public List<string> LayerFilter { get; set; }

        public ThFireCompartmentOutlineRecognitionEngine()
        {
            LayerFilter = new List<string>();
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var engine = new ThFireCompartmentOutlineExtractionEngine()
            {
                LayerFilter = LayerFilter,
            };
            engine.ExtractFromMS(database);
            Recognize(engine.Results, polygon);
        }

        public override void RecognizeMS(Database database, ObjectIdCollection dbObjs)
        {
            var engine = new ThFireCompartmentOutlineExtractionEngine()
            {
                LayerFilter = LayerFilter,
            };
            engine.ExtractFromMS(database, dbObjs);
            Recognize(engine.Results, new Point3dCollection());
        }

        public override void Recognize(List<ThRawIfcSpatialElementData> datas, Point3dCollection polygon)
        {
            var results = new List<ThRawIfcSpatialElementData>();
            var objs = datas.Select(o => o.Geometry).ToCollection();
            if (polygon.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var pline = new Polyline()
                {
                    Closed = true,
                };
                pline.CreatePolyline(polygon);
                var filterObjs = spatialIndex.SelectCrossingPolygon(pline);
                results = datas.Where(o => filterObjs.Contains(o.Geometry as Curve)).ToList();
            }
            else
            {
                results = datas;
            }
            results.ForEach(o =>
            {
                if (o.Geometry is Polyline polyline && polyline.Area > 0.0)
                {
                    var room = ThIfcRoom.Create(polyline);
                    Elements.Add(room);
                }
            });
        }

        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThFireCompartmentOutlineExtractionEngine()
            {
                LayerFilter = LayerFilter,
            };
            engine.Extract(database);
            Recognize(engine.Results, polygon);
        }
    }
}
