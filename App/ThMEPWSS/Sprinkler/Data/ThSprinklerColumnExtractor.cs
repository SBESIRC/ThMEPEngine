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
using ThMEPWSS.Sprinkler.Service;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPWSS.Sprinkler.Data
{
    public class ThSprinklerColumnExtractor : ThColumnExtractor, ITransformer
    {
        private List<ThStoreyInfo> StoreyInfos { get; set; }
        public List<ThRawIfcBuildingElementData> Db3ExtractResults { get; set; }  //From Db3
        public List<ThRawIfcBuildingElementData> NonDb3ExtractResults { get; set; } // From Non-Db3
        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }

        public ThSprinklerColumnExtractor()
        {
            StoreyInfos = new List<ThStoreyInfo>();
            Db3ExtractResults = new List<ThRawIfcBuildingElementData>();
            NonDb3ExtractResults = new List<ThRawIfcBuildingElementData>();
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            var db3Columns = ExtractDb3Column(pts);
            var nonDb3Columns = ExtractNonDb3Column(pts);
            var xRefColumns = new DBObjectCollection();
            db3Columns.Cast<Entity>().ForEach(e => xRefColumns.Add(e));
            nonDb3Columns.Cast<Entity>().ForEach(e => xRefColumns.Add(e));
            xRefColumns = xRefColumns.UnionPolygons();
            xRefColumns = xRefColumns.FilterSmallArea(SmallAreaTolerance);
            Columns = xRefColumns.Buffer(-2.5).Cast<Polyline>().ToList();

            var localColumns = ExtractMsColumn(database, pts);

            var clean = new ThSprinklerCleanEntityService();
            localColumns = localColumns.FilterSmallArea(SmallAreaTolerance)
                .Cast<Polyline>()
                .Select(o => clean.Clean(o))
                .Cast<Entity>()
                .ToCollection();
            //对Clean的结果进一步过虑
            localColumns = localColumns.FilterSmallArea(SmallAreaTolerance);

            //处理重叠
            var conflictService = new ThSprinklerHandleConflictService(
                localColumns.Cast<Entity>().ToList(),
                xRefColumns.Cast<Entity>().ToList());
            conflictService.Handle();
            var handleObjs = conflictService.Results.ToCollection().FilterSmallArea(SmallAreaTolerance);
            ThHandleContainsService handlecontain = new ThHandleContainsService();
            handleObjs = handlecontain.Handle(handleObjs.Cast<Entity>().ToList()).ToCollection();
            Columns = handleObjs.Cast<Polyline>().ToList();
        }

        private DBObjectCollection ExtractDb3Column(Point3dCollection pts)
        {
            var columnEngine = new ThDB3ColumnRecognitionEngine();
            var newPts = Transformer.Transform(pts);
            columnEngine.Recognize(Db3ExtractResults, newPts);
            return columnEngine.Elements.Select(o => o.Outline as Polyline).ToCollection();
        }

        private DBObjectCollection ExtractNonDb3Column(Point3dCollection pts)
        {
            var columnEngine = new ThColumnRecognitionEngine();
            var newPts = Transformer.Transform(pts);
            columnEngine.Recognize(NonDb3ExtractResults, newPts);
            return columnEngine.Elements
                .Select(o => o.Outline as Polyline)
                .ToCollection();
        }

        private DBObjectCollection ExtractMsColumn(Database database, Point3dCollection pts)
        {
            var localColumns = new DBObjectCollection();
            var instance = new ThExtractPolylineService()
            {
                ElementLayer = this.ElementLayer,
            };
            instance.Extract(database, pts);
            instance.Polys.ForEach(o => Transformer.Transform(o));
            return instance.Polys.ToCollection();
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Columns.ForEach(o =>
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
            var results = StoreyInfos.Where(o => o.Boundary.IsContains(entity));
            return results.Count() > 0 ? results.First() : new ThStoreyInfo();
        }

        public void Transform()
        {
            Transformer.Transform(Columns.ToCollection());
        }

        public void Reset()
        {
            Transformer.Reset(Columns.ToCollection());
        }
    }
}
