using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Engine
{
    public class ThRoomOutlineExtractionEngine : ThSpatialElementExtractionEngine
    {
        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThRoomOutlineExtractionVisitor()
            {
                LayerFilter = ThRoomLayerManager.CurveModelSpaceLayers(database),
            };
            var extractor = new ThSpatialElementExtractor();
            extractor.Accept(visitor);
            extractor.ExtractFromMS(database);
            Results = visitor.Results;
        }

        public override void Extract(Database database)
        {
            throw new NotSupportedException();
        }

        public override void ExtractFromMS(Database database, ObjectIdCollection dbObjs)
        {
            throw new NotImplementedException();
        }
    }

    public class ThRoomOutlineRecognitionEngine : ThSpatialElementRecognitionEngine
    {
        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var engine = new ThRoomOutlineExtractionEngine();
            engine.ExtractFromMS(database);
            Recognize(engine.Results, polygon);
        }

        public override void Recognize(Database database, Point3dCollection polygon)
        {
            throw new NotSupportedException();
        }

        public override void Recognize(List<ThRawIfcSpatialElementData> datas, Point3dCollection polygon)
        {
            var curves = new DBObjectCollection();
            var objs = datas.Select(o => o.Geometry).ToCollection();
            if (polygon.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                curves = spatialIndex.SelectCrossingPolygon(polygon);
            }
            else
            {
                curves = objs;
            }
            curves = ThRoomOutlineSimplifier.MakeValid(curves);
            if (curves.Count > 0)
            {
                var results = ThRoomOutlineSimplifier.Simplify(curves);
                Elements.AddRange(results.Cast<Polyline>().Select(o => ThIfcRoom.Create(o)));
            }
        }

        public override void RecognizeMS(Database database, ObjectIdCollection dbObjs)
        {
            throw new NotImplementedException();
        }
    }
}
