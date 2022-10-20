using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThDB3ArchWallExtractionEngine : ThBuildingElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var archWallVisitor = new ThDB3ArchWallExtractionVisitor()
            {
                LayerFilter = ThArchitectureWallLayerManager.CurveXrefLayers(database).ToHashSet(),
            };
            var pcArchWallVisitor = new ThDB3ArchWallExtractionVisitor()
            {
                LayerFilter = ThPCArchitectureWallLayerManager.CurveXrefLayers(database).ToHashSet(),
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(archWallVisitor);
            extractor.Accept(pcArchWallVisitor);
            extractor.Extract(database);
            Results = new List<ThRawIfcBuildingElementData>();
            Results.AddRange(archWallVisitor.Results);
            Results.AddRange(pcArchWallVisitor.Results);
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

    public class ThDB3ArchWallRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThDB3ArchWallExtractionEngine();
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
            if (curves.Count > 0)
            {
                // 处理Spikes
                var results = ThArchitectureWallSimplifier.Normalize(curves);
                // 处理Self-intersects
                results = ThArchitectureWallSimplifier.MakeValid(results);
                // 处理Duplicated Vertices
                results = ThArchitectureWallSimplifier.Simplify(results);
                results = ThArchitectureWallSimplifier.BuildArea(results);
                results = ThArchitectureWallSimplifier.Filter(results);
                results.Cast<Entity>().ForEach(o => Elements.Add(ThIfcWall.Create(o)));
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
    }
}
