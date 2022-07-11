using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;

namespace ThMEPEngineCore.Engine
{
    public class ThDB3ColumnExtractionEngine : ThBuildingElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = Create(database);
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results = visitor.Results;
        }
        public static ThDB3ColumnExtractionVisitor Create(Database database)
        {
            return new ThDB3ColumnExtractionVisitor()
            {
                LayerFilter = ThDbLayerManager.Layers(database),
            };
        }
    }

    public class ThDB3ColumnRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThDB3ColumnExtractionEngine();
            engine.Extract(database);
            Recognize(engine.Results, polygon);
        }

        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            var curves = new DBObjectCollection();
            var objs = datas.Select(o => o.Geometry).ToCollection();
            //处理不完美的柱
            var simplifier = new ThPolygonalElementSimplifier();
            objs = simplifier.Simplify(objs);
            objs = simplifier.Normalize(objs);
            if (polygon.Count > 0)
            {
                ThCADCoreNTSSpatialIndex shearwallCurveSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                foreach (var filterObj in shearwallCurveSpatialIndex.SelectCrossingPolygon(polygon))
                {
                    curves.Add(filterObj as Curve);
                }
            }
            else
            {
                curves = objs;
            }

            // 识别圆形柱
            // 假定圆形轮廓一定是圆形柱的轮廓
            Elements.AddRange(RecognizeCircularElements(curves.Cast<Curve>().Where(o => o is Circle).ToCollection()));

            // 识别其他构件
            Elements.AddRange(RecognizeLinealElements(curves.Cast<Curve>().Where(o => o is Polyline).ToCollection()));
        }

        private List<ThIfcBuildingElement> RecognizeCircularElements(DBObjectCollection curves)
        {
            var simplifier = new ThDB3ColumnSimplifier();
            return simplifier.Tessellate(curves)
                .Cast<Polyline>()
                .Select(o => ThIfcColumn.Create(o) as ThIfcBuildingElement)
                .ToList();
        }

        private DBObjectCollection PreprocessLinealElements(DBObjectCollection curves)
        {
            var simplifier = new ThDB3ColumnSimplifier();
            var results = simplifier.Tessellate(curves);
            results = simplifier.MakeValid(curves);
            results = simplifier.Normalize(results);
            results = simplifier.Simplify(results);
            return results;
        }

        private List<ThIfcBuildingElement> RecognizeLinealElements(DBObjectCollection curves)
        {
            var columns = new DBObjectCollection();
            var elements = new List<ThIfcBuildingElement>();
            var results = PreprocessLinealElements(curves);
            elements.AddRange(results.Cast<Curve>().Select(o => ThIfcColumn.Create(o) as ThIfcBuildingElement));
            return elements;
        }
    }
}
