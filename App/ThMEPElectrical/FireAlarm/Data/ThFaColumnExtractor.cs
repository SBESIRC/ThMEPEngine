using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPElectrical.FireAlarm.Service;
using NFox.Cad;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.IO;
using ThMEPElectrical.FireAlarm.Interface;
using ThMEPEngineCore.Algorithm;
using Dreambuild.AutoCAD;

namespace FireAlarm.Data
{
    public class ThFaColumnExtractor :ThColumnExtractor, IGroup, ISetStorey, ITransformer
    {
        private List<ThStoreyInfo> StoreyInfos { get; set; }
        public List<ThRawIfcBuildingElementData> Db3ExtractResults { get; set; }  //From Db3
        public List<ThRawIfcBuildingElementData> NonDb3ExtractResults { get; set; } // From Non-Db3
        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }

        public ThFaColumnExtractor()
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

            var localColumns = ExtractMsColumn(database, pts);

            ThCleanEntityService clean = new ThCleanEntityService();
            localColumns = localColumns.FilterSmallArea(SmallAreaTolerance)
                .Cast<Polyline>()
                .Select(o => clean.Clean(o))
                .Cast<Entity>()
                .ToCollection();
            //对Clean的结果进一步过虑
            localColumns = localColumns.FilterSmallArea(SmallAreaTolerance);

            //处理重叠
            var conflictService = new ThHandleConflictService(
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
            Db3ExtractResults.ForEach(o => Transformer.Transform(o.Geometry));
            var columnEngine = new ThDB3ColumnRecognitionEngine();
            var newPts = new Point3dCollection();
            pts.Cast<Point3d>().ForEach(p =>
            {
                var pt = new Point3d(p.X, p.Y, p.Z);
                Transformer.Transform(ref pt);
                newPts.Add(pt);
            });
            columnEngine.Recognize(Db3ExtractResults, newPts);
            return columnEngine.Elements.Select(o => o.Outline as Polyline).ToCollection();
        }
        private DBObjectCollection ExtractNonDb3Column(Point3dCollection pts)
        {
            NonDb3ExtractResults.ForEach(o => Transformer.Transform(o.Geometry));
            var columnEngine = new ThColumnRecognitionEngine();
            var newPts = new Point3dCollection();
            pts.Cast<Point3d>().ForEach(p =>
            {
                var pt = new Point3d(p.X, p.Y, p.Z);
                Transformer.Transform(ref pt);
                newPts.Add(pt);
            });
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
            localColumns = instance.Polys.ToCollection();

            return localColumns;
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

        public void Set(List<ThStoreyInfo> storeyInfos)
        {
            StoreyInfos = storeyInfos;
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            Columns.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
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
