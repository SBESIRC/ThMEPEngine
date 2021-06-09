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
                LayerFilter = ThArchitectureWallLayerManager.CurveXrefLayers(database),
            };
            var pcArchWallVisitor = new ThDB3ArchWallExtractionVisitor()
            {
                LayerFilter = ThPCArchitectureWallLayerManager.CurveXrefLayers(database),
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(archWallVisitor);
            extractor.Accept(pcArchWallVisitor);
            extractor.Extract(database);
            Results = new List<ThRawIfcBuildingElementData>();
            Results.AddRange(archWallVisitor.Results);
            Results.AddRange(pcArchWallVisitor.Results);
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
            curves = ThArchitectureWallSimplifier.MakeValid(curves);
            if (curves.Count > 0)
            {
                var results = ThArchitectureWallSimplifier.Normalize(curves);
                results = ThArchitectureWallSimplifier.Simplify(results);
                results = ThArchitectureWallSimplifier.BuildArea(results);
                results.Cast<Entity>().ForEach(o => Elements.Add(ThIfcWall.Create(o)));
            }
        }
    }
}
