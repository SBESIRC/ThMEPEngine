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
    public class ThWindowOutlineExtractionEngine : ThSpatialElementExtractionEngine
    {
        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThRoomOutlineExtractionVisitor()
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
            var visitor = new ThRoomOutlineExtractionVisitor()
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
    public class ThWindowOutlineRecognitionEngine : ThSpatialElementRecognitionEngine
    {
        public List<Polyline> windowsOutLines;
        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var engine = new ThWindowOutlineExtractionEngine();
            engine.ExtractFromMS(database);
            Recognize(engine.Results, polygon);
        }

        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThWindowOutlineExtractionEngine();
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
                var windowPolys = curves.OfType<Polyline>().ToList();
                var simplifer = new ThRoomOutlineSimplifier();               
                simplifer.Close(windowPolys); // 封闭
                var windowObjs = windowPolys.ToCollection();
                windowObjs = simplifer.Normalize(windowObjs); // 处理狭长线
                windowObjs = simplifer.MakeValid(windowObjs); // 处理自交
                windowObjs = simplifer.Simplify(windowObjs);  // 处理简化线
                windowObjs = simplifer.Filter(windowObjs);
                transformer.Reset(windowObjs);
                foreach (Polyline pl in windowObjs)
                    windowsOutLines.Add(pl);
            }
        }
        public override void RecognizeMS(Database database, ObjectIdCollection dbObjs)
        {
            throw new NotImplementedException();
        }
    }
}
