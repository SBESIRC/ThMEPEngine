using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;

namespace TianHua.Mep.UI.Data
{
    /// <summary>
    /// 1、提取DB3Column,Column,Shearwall,DB3Shearwall规则之外的Hatch，
    /// 2、Hatch是Solid的填充
    /// 3、不考虑图层规则
    /// </summary>
    public class ThOtherShearWallExtractionEngine : ThShearWallExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = new ThOtherShearWallExtractionVisitor()
            {
                BlackVisitors = CreateBlacks(database),
                LayerFilter = ThDbLayerManager.Layers(database).ToHashSet(),
            };
            var extractor = new ThBlockInnerElementExtractor(visitor);
            extractor.Extract(database);
            Results = visitor.Results;
        }

        public static List<ThBuildingElementExtractionVisitor> CreateBlacks(Database database)
        {
            var results = new List<ThBuildingElementExtractionVisitor>();
            results.Add(ThShearWallExtractionEngine.Create(database));
            results.Add(ThDB3ShearWallExtractionEngine.Create(database));            
            results.Add(ThColumnExtractionEngine.Create(database));
            results.Add(ThDB3ColumnExtractionEngine.Create(database));
            return results;
        }
    }

    public class ThOtherShearWallRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThOtherShearWallExtractionEngine();
            engine.Extract(database);
            Recognize(engine.Results, polygon);
        }
        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection frame)
        {
            var objs = datas.Select(o => o.Geometry).ToCollection();
            var polygons = new DBObjectCollection();           
            if (frame.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                polygons = spatialIndex.SelectCrossingPolygon(frame);
            }
            else
            {
                polygons = objs;
            }

            //
            var results = Postprocess(polygons);
            results.OfType<Entity>().ForEach(o => Elements.Add(ThIfcWall.Create(o)));

            objs = objs.Difference(results);
            objs.MDispose();
        }

        private DBObjectCollection Postprocess(DBObjectCollection objs)
        {
            var results = new DBObjectCollection();
            var transformer = new ThMEPEngineCore.Algorithm.ThMEPOriginTransformer(objs);
            transformer.Transform(objs);
            var simplifer = new ThDB3ShearWallSimplifier();
            objs.OfType<Entity>().ForEach(e =>
            {
                if(e is MPolygon mPolygon)
                {
                    results = results.Union(Clean(mPolygon, simplifer));
                }
                else if(e is Polyline polyline)
                {
                    results = results.Union(Clean(polyline, simplifer));
                }
                else
                {
                    results.Add(e);
                }
            });
            transformer.Reset(results);
            return results;
        }

        private DBObjectCollection Clean(MPolygon polygon, ThBuildElementSimplifier simplifier)
        {
            var garbages = new DBObjectCollection();

            var objs = new DBObjectCollection();
            objs.Add(polygon.Shell());
            polygon.Holes().ForEach(o => objs.Add(o));
            garbages = garbages.Union(objs);

            var cleanPolys = new DBObjectCollection();
            objs.OfType<Polyline>().ForEach(o => cleanPolys = cleanPolys.Union(CleanPolyline(o, simplifier)));
            garbages = garbages.Union(cleanPolys);

            var results = cleanPolys.BuildArea();
            garbages = garbages.Union(results);
            results = simplifier.Filter(results);

            garbages = garbages.Difference(results);
            garbages.MDispose();

            return results;
        }

        private DBObjectCollection Clean(Polyline polyline, ThBuildElementSimplifier simplifier)
        {
            return CleanPolyline(polyline, simplifier);
        }

        private DBObjectCollection CleanPolyline(Polyline polyline,ThBuildElementSimplifier simplifier)
        {
            var results = new DBObjectCollection { polyline };
            simplifier.MakeClosed(results);

            // 以下会产生新对象
            var garbages = new DBObjectCollection();

            // make valid
            if (results.Count>0)
            {
                results = simplifier.MakeValid(results);
                garbages = garbages.Union(results);
                results = simplifier.Filter(results);
            }

            // normalize
            if (results.Count>0)
            {
                results = simplifier.Normalize(results);
                garbages = garbages.Union(results);
                results = simplifier.Filter(results);
            }

            // simplify
            if(results.Count > 0)
            {
                results = simplifier.Simplify(results);
                garbages = garbages.Union(results);
                results = simplifier.Filter(results);
            }

            // dispose
            garbages = garbages.Difference(results);
            garbages.MDispose();

            return results;
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
