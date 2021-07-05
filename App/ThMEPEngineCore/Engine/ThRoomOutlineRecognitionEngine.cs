using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

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
            if (curves.Count > 0)
            {
                var transformer = new ThMEPOriginTransformer(curves);
                transformer.Transform(curves);
                curves = ThRoomOutlineSimplifier.MakeValid(curves);
                curves = ThRoomOutlineSimplifier.Simplify(curves);
                transformer.Reset(curves);
                Elements.AddRange(curves.Cast<Polyline>().Select(o => ThIfcRoom.Create(o)));
            }
        }

        public override void RecognizeMS(Database database, ObjectIdCollection dbObjs)
        {
            throw new NotImplementedException();
        }

    }
}
