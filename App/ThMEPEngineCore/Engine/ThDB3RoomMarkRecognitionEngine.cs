using System;
using NFox.Cad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;

namespace ThMEPEngineCore.Engine
{
    public class ThDB3RoomMarkExtractionEngine : ThAnnotationElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = new ThDB3RoomMarkExtractionVisitor
            {
                LayerFilter = ThRoomMarkLayerManager.TextXrefLayers(database),
            };
            var extractor = new ThAnnotationElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results = visitor.Results;
        }

        public override void ExtractFromMS(Database database)
        {
            throw new NotSupportedException();
        }
    }

    public class ThDB3RoomMarkRecognitionEngine : ThAnnotationElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThDB3RoomMarkExtractionEngine();
            engine.Extract(database);
            Recognize(engine.Results, polygon);
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new NotSupportedException();
        }

        public override void Recognize(List<ThRawIfcAnnotationElementData> datas, Point3dCollection polygon)
        {
            //过滤DBText没有算出OBB的情况，防止丢进去引起NTS报错
            var objs = datas.Where(o => (o.Geometry as Polyline).NumberOfVertices > 0).Select(o => o.Geometry).ToCollection();
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
