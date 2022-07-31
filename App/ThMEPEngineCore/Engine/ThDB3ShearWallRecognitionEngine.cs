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
    public class ThDB3ShearWallExtractionEngine : ThBuildingElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = Create(database);
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results = visitor.Results;
        }
        public static ThDB3ShearWallExtractionVisitor Create(Database database)
        {
            return new ThDB3ShearWallExtractionVisitor()
            {
                LayerFilter = ThDbLayerManager.Layers(database),
            };
        }

        public override void ExtractFromMS(Database database)
        {
            throw new System.NotImplementedException();
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new System.NotImplementedException();
        }
    }

    public class ThDB3ShearWallRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThDB3ShearWallExtractionEngine();
            engine.Extract(database);
            Recognize(engine.Results, polygon);
        }

        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            var curves = new DBObjectCollection();
            var objs = datas.Select(o => o.Geometry).ToCollection();
            //处理不完美的墙
            var simplifier = new ThShearWallSimplifier();
            simplifier.MakeClosed(objs);
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

            Elements.AddRange(RecognizeLinealElements(curves.Cast<Curve>().Where(o => o is Polyline).ToCollection()));
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new System.NotImplementedException();
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new System.NotImplementedException();
        }

        private DBObjectCollection PreprocessLinealElements(DBObjectCollection curves)
        {
            var simplifier = new ThDB3ShearWallSimplifier();
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
            elements.AddRange(results.Cast<Entity>().Select(o => ThIfcWall.Create(o) as ThIfcBuildingElement));
            return elements;
        }
    }
}
