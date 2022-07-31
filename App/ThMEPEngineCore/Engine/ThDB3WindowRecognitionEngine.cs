using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThDB3WindowExtractionEngine : ThBuildingElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = new ThDB3WindowExtractionVisitor()
            {
                LayerFilter = ThDbLayerManager.Layers(database),
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results = visitor.Results;
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new System.NotImplementedException();
        }

        public override void ExtractFromMS(Database database)
        {
            throw new System.NotImplementedException();
        }
    }

    public class ThDB3WindowRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThDB3WindowExtractionEngine();
            engine.Extract(database);

            // 创建偏移矩阵
            var transformer = new ThMEPOriginTransformer(
                engine.Results.Select(o=>o.Geometry).ToCollection());

            // 移动
            var newPts = transformer.Transform(polygon);
            engine.Results.ForEach(e => transformer.Transform(e.Geometry));
            Recognize(engine.Results, newPts);

            // 还原
            Elements.ForEach(e => transformer.Reset(e.Outline));
        }

        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            var curves = new List<Curve>();
            var dbObjs = datas.Select(o => o.Geometry).ToCollection();
            if (polygon.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                foreach (var filterObj in spatialIndex.SelectCrossingPolygon(polygon))
                {
                    curves.Add(filterObj as Curve);
                }
            }
            else
            {
                curves = dbObjs.Cast<Curve>().ToList();
            }

            //创建窗户的外轮廓
            //后续，根据需要增加处理...
            if(curves.Count>0)
            {
                var results = curves.ToCollection();
                results = results.Outline();
                results = results.FilterSmallArea(1.0);
                foreach (Curve obj in results)
                {
                    var ent = Wash(obj); //清洗
                    if(ent is Polyline poly && poly.Area>1.0)
                    {
                        Elements.Add(ThIfcWindow.Create(poly));
                    }
                }
            }
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new System.NotImplementedException();
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new System.NotImplementedException();
        }

        private Entity Wash(Curve curve)
        {
            var objs = new DBObjectCollection() { curve };
            var simplifer = new ThWindowSimplifier();
            objs = simplifer.Simplify(objs);
            objs = simplifer.Normalize(objs);
            if (objs.Count > 0 && objs[0] is Polyline polyline)
            {
                return ThPolylineHandler.Handle(polyline);
            }
            return objs.Count > 0 ? objs[0] as Entity : new Polyline();
        }
    }
}

   

