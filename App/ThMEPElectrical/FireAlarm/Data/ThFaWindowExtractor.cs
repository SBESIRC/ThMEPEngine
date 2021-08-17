﻿using NFox.Cad;
using System.Linq;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.FireAlarm.Service;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPElectrical.FireAlarm.Interface;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using Dreambuild.AutoCAD;

namespace FireAlarm.Data
{
    public class ThFaWindowExtractor : ThWindowExtractor, ISetStorey, ITransformer
    {
        private List<ThStoreyInfo> StoreyInfos { get; set; }
        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }
        public ThFaWindowExtractor()
        {
            StoreyInfos = new List<ThStoreyInfo>();
        }
        public override void Extract(Database database, Point3dCollection pts)
        {
            var db3Windows = ExtractDb3Window(database, pts);
            var localWindows = ExtractMsWindow(database, pts);

            ThCleanEntityService clean = new ThCleanEntityService();
            localWindows = localWindows.FilterSmallArea(SmallAreaTolerance)
                .Cast<Polyline>()
                .Select(o => clean.Clean(o))
                .Cast<Entity>()
                .ToCollection();
            //对Clean的结果进一步过虑
            localWindows = localWindows.FilterSmallArea(SmallAreaTolerance);

            //处理重叠
            var conflictService = new ThHandleConflictService(
                localWindows.Cast<Entity>().ToList(),
                db3Windows.Cast<Entity>().ToList());
            conflictService.Handle();
            var handleObjs = conflictService.Results.ToCollection().FilterSmallArea(SmallAreaTolerance);
            Windows = handleObjs.Cast<Polyline>().ToList();
        }
        private DBObjectCollection ExtractDb3Window(Database database, Point3dCollection pts)
        {
            var db3Windows = new DBObjectCollection();
            var db3WindowExtractionEngine = new ThDB3WindowExtractionEngine();
            db3WindowExtractionEngine.Extract(database);
            db3WindowExtractionEngine.Results.ForEach(o => Transformer.Transform(o.Geometry));

            var windowEngine = new ThDB3WindowRecognitionEngine();
            var newPts = new Point3dCollection();
            pts.Cast<Point3d>().ForEach(p =>
            {
                var pt = new Point3d(p.X, p.Y, p.Z);
                Transformer.Transform(ref pt);
                newPts.Add(pt);
            });
            windowEngine.Recognize(db3WindowExtractionEngine.Results, newPts);
            db3Windows = windowEngine.Elements.Select(o => o.Outline as Polyline).ToCollection();
            return db3Windows;
        }
        private DBObjectCollection ExtractMsWindow(Database database, Point3dCollection pts)
        {
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
            var results = StoreyInfos.Where(o => o.Boundary.IsContains(entity));
            return results.Count() > 0 ? results.First() : new ThStoreyInfo();
        }

        public void Set(List<ThStoreyInfo> storeyInfos)
        {
            StoreyInfos = storeyInfos;
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
