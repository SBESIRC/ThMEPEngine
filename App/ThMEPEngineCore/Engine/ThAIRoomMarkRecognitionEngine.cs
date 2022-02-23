using System;
using NFox.Cad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThAIRoomMarkExtractionEngine : ThAnnotationElementExtractionEngine
    {
        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThAIRoomMarkExtractionVisitor
            {
                LayerFilter = ThRoomMarkLayerManager.TextModelSpaceLayers(database),
            };
            var extractor = new ThAnnotationElementExtractor();
            extractor.Accept(visitor);
            extractor.ExtractFromMS(database);
            Results = visitor.Results;
        }

        public override void Extract(Database database)
        {
            var visitor = new ThAIRoomMarkExtractionVisitor
            {
                LayerFilter = ThRoomMarkLayerManager.AIRoomMarkXRefLayers(database),
            };
            var extractor = new ThAnnotationElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results = visitor.Results;
        }
    }
    public class ThAIRoomMarkRecognitionEngine : ThAnnotationElementRecognitionEngine
    {
        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var engine = new ThAIRoomMarkExtractionEngine();
            engine.ExtractFromMS(database);
            Recognize(engine.Results, polygon);
        }

        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThAIRoomMarkExtractionEngine();
            engine.Extract(database);
            Recognize(engine.Results, polygon);
        }       

        public override void Recognize(List<ThRawIfcAnnotationElementData> datas, Point3dCollection polygon)
        {
            var objs = datas.Select(o => o.Geometry).ToCollection();
            var filterDatas = new List<ThRawIfcAnnotationElementData>();
            if (polygon.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var pline = new Polyline()
                {
                    Closed = true,
                };
                pline.CreatePolyline(polygon);
                var filterObjs = spatialIndex.SelectCrossingPolygon(pline);
                filterDatas = datas.Where(o => filterObjs.Contains(o.Geometry)).ToList();
            }
            else
            {
                filterDatas = datas;
            }
            filterDatas.ForEach(o => Elements.Add(ThIfcTextNote.Create(GetContent(o.Data), o.Geometry as Polyline)));
        }
        private string GetContent(object data)
        {
            if (data is DBText dbText)
            {
                return dbText.TextString;
            }
            else if (data is MText mText)
            {
                return mText.Text;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
