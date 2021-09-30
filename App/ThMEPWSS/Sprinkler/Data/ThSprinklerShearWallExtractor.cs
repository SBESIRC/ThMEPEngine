using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPWSS.Sprinkler.Data
{
    public class ThSprinklerShearWallExtractor : ThShearwallExtractor, ITransformer
    {
        private List<ThStoreyInfo> StoreyInfos { get; set; }
        public List<ThRawIfcBuildingElementData> Db3ExtractResults { get; set; }
        public List<ThRawIfcBuildingElementData> NonDb3ExtractResults { get; set; }
        public ThSprinklerShearWallExtractor()
        {
            StoreyInfos = new List<ThStoreyInfo>();
            Db3ExtractResults = new List<ThRawIfcBuildingElementData>();
            NonDb3ExtractResults = new List<ThRawIfcBuildingElementData>();
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            var db3Walls = ExtractDb3Wall(pts);
            var nonDb3Walls = ExtractNonDb3Wall(pts);
            var xRefWalls = new DBObjectCollection();
            db3Walls.Cast<Entity>().ForEach(e => xRefWalls.Add(e));
            nonDb3Walls.Cast<Entity>().ForEach(e => xRefWalls.Add(e));
            xRefWalls = xRefWalls.UnionPolygons();
            xRefWalls = xRefWalls.FilterSmallArea(SmallAreaTolerance);
            Walls = xRefWalls.Cast<Entity>().ToList();
        }

        private DBObjectCollection ExtractDb3Wall(Point3dCollection pts)
        {
            var wallEngine = new ThDB3ShearWallRecognitionEngine();
            var newPts = Transformer.Transform(pts);
            wallEngine.Recognize(Db3ExtractResults, newPts);
            return wallEngine.Elements.Select(o => o.Outline as Polyline).ToCollection();
        }

        private DBObjectCollection ExtractNonDb3Wall(Point3dCollection pts)
        {
            var wallEngine = new ThShearWallRecognitionEngine();
            var newPts = Transformer.Transform(pts);
            wallEngine.Recognize(NonDb3ExtractResults, newPts);
            return wallEngine.Elements
                .Select(o => o.Outline as Polyline)
                .ToCollection();
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Walls.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                var parentId = BuildString(GroupOwner, o);
                if (string.IsNullOrEmpty(parentId))
                {
                    var storeyInfo = Query(o);
                    if (storeyInfo != null)
                    {
                        parentId = storeyInfo.Id;
                    }
                }
                geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, parentId);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public ThStoreyInfo Query(Entity entity)
        {
            var results = StoreyInfos.Where(o => o.Boundary.IsContains(entity));
            return results.Count() > 0 ? results.First() : new ThStoreyInfo();
        }

        public void Transform()
        {
            Transformer.Transform(Walls.ToCollection());
        }

        public void Reset()
        {
            Transformer.Reset(Walls.ToCollection());
        }
        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }
    }
}
