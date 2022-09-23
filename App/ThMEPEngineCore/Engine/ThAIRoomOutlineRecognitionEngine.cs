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
    public class ThAIRoomOutlineExtractionEngine : ThSpatialElementExtractionEngine
    {
        public bool IsSupportMPolygon { get; set; } = false;
        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThAIRoomOutlineExtractionVisitor()
            {
                IsSupportMPolygon = this.IsSupportMPolygon,
                LayerFilter = ThRoomLayerManager.CurveModelSpaceLayers(database),
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
                IsSupportMPolygon = this.IsSupportMPolygon,
                LayerFilter = ThRoomLayerManager.CurveModelSpaceLayers(database),
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

    public class ThAIRoomOutlineRecognitionEngine : ThSpatialElementRecognitionEngine
    {
        public bool IsSupportMPolygon { get; set; } = false;
        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var engine = new ThAIRoomOutlineExtractionEngine()
            { 
                IsSupportMPolygon =this.IsSupportMPolygon,
            };
            engine.ExtractFromMS(database);
            Recognize(engine.Results, polygon);
        }

        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThAIRoomOutlineExtractionEngine()
            {
                IsSupportMPolygon = this.IsSupportMPolygon,
            };
            engine.Extract(database);
            Recognize(engine.Results, polygon);
        }

        public override void Recognize(List<ThRawIfcSpatialElementData> datas, Point3dCollection polygon)
        {
            var polygons = new DBObjectCollection();
            var objs = datas.Select(o => o.Geometry).ToCollection();
            if (polygon.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                polygons = spatialIndex.SelectCrossingPolygon(polygon);
            }
            else
            {
                polygons = objs;
            }               
            if (polygons.Count > 0)
            {
                var transformer = new ThMEPOriginTransformer(polygons);
                transformer.Transform(polygons);
                var roomSimplifer = new ThRoomOutlineSimplifier();
                roomSimplifer.MakeClosed(polygons); // 封闭
                polygons = roomSimplifer.Normalize(polygons); // 处理狭长线
                polygons = roomSimplifer.MakeValid(polygons); // 处理自交
                polygons = roomSimplifer.Simplify(polygons);  // 处理简化线
                polygons = roomSimplifer.Filter(polygons);    // 过滤面积极小的线
                polygons = roomSimplifer.OverKill(polygons);  // 去重
                transformer.Reset(polygons);
                Elements.AddRange(polygons.OfType<Entity>().Select(o => ThIfcRoom.Create(o)));
            }
        }

        public override void RecognizeMS(Database database, ObjectIdCollection dbObjs)
        {
            throw new NotImplementedException();
        }
    }
}
