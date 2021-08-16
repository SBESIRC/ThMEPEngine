using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThDB3WindowExtractionEngine : ThBuildingElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            //V1.0窗户提取规则：图层:"AE-WIND",超链接中Category为窗
            var visitor = new ThDB3WindowExtractionVisitor()
            {
                LayerFilter = ThWindowLayerManager.CurveXrefLayers(database),
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results = visitor.Results;
        }
    }

    public class ThDB3WindowRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThDB3WindowExtractionEngine();
            engine.Extract(database);
            Recognize(engine.Results, polygon);
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
                foreach (Curve obj in results.Outline())
                {
                    Elements.Add(ThIfcWindow.Create(Wash(obj)));
                }
            }
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

   

