using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThAIWindowOutlineExtractionEngine : ThSpatialElementExtractionEngine
    {
        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThAIRoomOutlineExtractionVisitor()
            {
                LayerFilter = ThWindowLayerManager.CurveModelSpaceLayers(database),
            };
            var extractor = new ThSpatialElementExtractor();
            extractor.Accept(visitor);
            extractor.ExtractFromMS(database);
            Results = visitor.Results;
        }

        public override void Extract(Database database)
        {
            var visitor = new ThAIRoomOutlineExtractionVisitor()
            {
                LayerFilter = ThWindowLayerManager.CurveModelSpaceLayers(database),
            };
            var extractor = new ThSpatialElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results = visitor.Results;
        }

        public override void ExtractFromMS(Database database, ObjectIdCollection dbObjs)
        {
            throw new NotImplementedException();
        }
    }
    public class ThAIWindowOutlineRecognitionEngine : ThSpatialElementRecognitionEngine
    {
        public List<Polyline> windowsOutLines;
        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var engine = new ThAIWindowOutlineExtractionEngine();
            engine.ExtractFromMS(database);
            Recognize(engine.Results, polygon);
        }

        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThAIWindowOutlineExtractionEngine();
            engine.Extract(database);
            Recognize(engine.Results, polygon);
        }

        public override void Recognize(List<ThRawIfcSpatialElementData> datas, Point3dCollection polygon)
        {
            var curves = new DBObjectCollection();
            windowsOutLines = new List<Polyline>();
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
                var simplifer = new ThRoomOutlineSimplifier();               
                simplifer.MakeClosed(curves); // 封闭
                curves = simplifer.Normalize(curves); // 处理狭长线
                curves = simplifer.MakeValid(curves); // 处理自交
                curves = simplifer.Simplify(curves);  // 处理简化线
                curves = simplifer.Filter(curves);
                transformer.Reset(curves);
                foreach (Polyline pl in curves)
                    windowsOutLines.Add(pl);
            }
        }
        public override void RecognizeMS(Database database, ObjectIdCollection dbObjs)
        {
            throw new NotImplementedException();
        }
    }
}
