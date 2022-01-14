using NFox.Cad;
using System.Linq;
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
    public class ThSprinklerWindowExtractor : ThWindowExtractor, ITransformer
    {
        private List<ThStoreyInfo> StoreyInfos { get; set; }
        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }
        public List<ThRawIfcBuildingElementData> Db3ExtractResults { get; set; }
        public ThSprinklerWindowExtractor()
        {
            StoreyInfos = new List<ThStoreyInfo>();
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            var db3Windows = ExtractDb3Window(pts).FilterSmallArea(SmallAreaTolerance);
            Windows = db3Windows.Cast<Polyline>().ToList();
        }

        private DBObjectCollection ExtractDb3Window(Point3dCollection pts)
        {
            var db3Windows = new DBObjectCollection();
            var windowEngine = new ThDB3WindowRecognitionEngine();
            var newPts = Transformer.Transform(pts);
            windowEngine.Recognize(Db3ExtractResults, newPts);
            db3Windows = windowEngine.Elements.Select(o => o.Outline as Polyline).ToCollection();
            return db3Windows;
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Windows.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                var parentId = BuildString(GroupOwner, o);
                if (string.IsNullOrEmpty(parentId))
                {
                    var storeyInfo = Query(o);
                    parentId = storeyInfo.Id;
                }
                geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, parentId);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public ThStoreyInfo Query(Entity entity)
        {
            var results = StoreyInfos.Where(o => o.Boundary.EntityContains(entity));
            return results.Count() > 0 ? results.First() : new ThStoreyInfo();
        }

        public void Transform()
        {
            Transformer.Transform(Windows.ToCollection());
        }

        public void Reset()
        {
            Transformer.Reset(Windows.ToCollection());
        }
    }
}
