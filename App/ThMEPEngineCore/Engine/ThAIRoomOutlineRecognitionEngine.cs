﻿using System;
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
        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThAIRoomOutlineExtractionVisitor()
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
            var visitor = new ThAIRoomOutlineExtractionVisitor()
            {
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
        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var engine = new ThAIRoomOutlineExtractionEngine();
            engine.ExtractFromMS(database);
            Recognize(engine.Results, polygon);
        }

        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThAIRoomOutlineExtractionEngine();
            engine.Extract(database);
            Recognize(engine.Results, polygon);
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
                var roomSimplifer = new ThRoomOutlineSimplifier();
                roomSimplifer.MakeClosed(curves); // 封闭
                curves = roomSimplifer.Normalize(curves); // 处理狭长线
                curves = roomSimplifer.MakeValid(curves); // 处理自交
                curves = roomSimplifer.Simplify(curves);  // 处理简化线
                curves = roomSimplifer.Filter(curves);    // 过滤面积极小的线
                curves = roomSimplifer.OverKill(curves);  // 去重
                transformer.Reset(curves);
                Elements.AddRange(curves.OfType<Polyline>().Select(o => ThIfcRoom.Create(o)));
                var ellipseRoom = curves.OfType<Ellipse>().ToList().ToCollection();
                transformer.Reset(ellipseRoom);
                Elements.AddRange(ellipseRoom.OfType<Ellipse>().Select(o => ThIfcRoom.Create(o)));
            }
        }

        public override void RecognizeMS(Database database, ObjectIdCollection dbObjs)
        {
            throw new NotImplementedException();
        }

    }
}
