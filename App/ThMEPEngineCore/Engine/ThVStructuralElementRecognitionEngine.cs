using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Engine
{
    public class ThVStructuralElementExtractionEngine : ThBuildingElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = new ThVStructuralElementExtractionVisitor();
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

    public class ThVStructuralElementRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThVStructuralElementExtractionEngine();
            engine.Extract(database);
            Recognize(engine.Results, polygon);
        }

        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            var curves = new DBObjectCollection();
            var objs = datas.Select(o => o.Geometry).ToCollection();
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
            return ThVStructuralElementSimplifier.Tessellate(curves)
                .Cast<Polyline>()
                .Select(o => ThIfcColumn.Create(o) as ThIfcBuildingElement)
                .ToList();
        }

        private DBObjectCollection PreprocessLinealElements(DBObjectCollection curves)
        {
            var results = ThVStructuralElementSimplifier.Tessellate(curves);
            results = ThVStructuralElementSimplifier.MakeValid(curves);
            results = ThVStructuralElementSimplifier.Normalize(results);
            results = ThVStructuralElementSimplifier.Simplify(results);
            return results;
        }

        private List<ThIfcBuildingElement> RecognizeLinealElements(DBObjectCollection curves)
        {
            var walls = new DBObjectCollection();
            var columns = new DBObjectCollection();
            var elements = new List<ThIfcBuildingElement>();
            ThVStructuralElementSimplifier.Classify(PreprocessLinealElements(curves), columns, walls);
            elements.AddRange(walls.Cast<Entity>().Select(o => ThIfcWall.Create(o) as ThIfcBuildingElement));
            elements.AddRange(columns.Cast<Curve>().Select(o => ThIfcColumn.Create(o) as ThIfcBuildingElement));
            return elements;
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new System.NotImplementedException();
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new System.NotImplementedException();
        }
    }
}