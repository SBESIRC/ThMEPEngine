﻿using NFox.Cad;
using DotNetARX;
using System.Linq;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
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
    public class ThSprinklerHoleExtractor : ThExtractorBase, IPrint, ITransformer
    {
        public Dictionary<Polyline, List<string>> HoleDic { get; private set; }
        private List<ThStoreyInfo> StoreyInfos { get; set; }

        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }

        public ThSprinklerHoleExtractor()
        {
            Category = BuiltInCategory.Hole.ToString();
            StoreyInfos = new List<ThStoreyInfo>();
            HoleDic = new Dictionary<Polyline, List<string>>();
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            HoleDic.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                var parentId = BuildString(GroupOwner, o.Key);
                if (string.IsNullOrEmpty(parentId))
                {
                    var storeyInfo = Query(o.Key);
                    parentId = storeyInfo.Id;
                }
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, string.Join(",", o.Value.ToArray()));
                geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, parentId);
                geometry.Boundary = o.Key;
                geos.Add(geometry);
            });
            return geos;
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            var extractService = new ThExtractPolylineService()
            {
                ElementLayer = this.ElementLayer,
            };
            extractService.Extract(database, pts);

            extractService.Polys.ForEach(o => Transformer.Transform(o));
            var holes = extractService.Polys.ToList();
            holes = holes.Select(o => ThSprinklerHandleNonClosedPolylineService.Handle(o)).ToList();
            var clean = new ThSprinklerCleanEntityService();
            holes = holes
                .Where(o => o.Area >= SmallAreaTolerance)
                .Select(o => clean.Clean(o))
                .Cast<Polyline>()
                .ToList();
            //对Clean的结果进一步过虑
            holes = holes.ToCollection().FilterSmallArea(1.0).Cast<Polyline>().ToList();

            var textService = new ThExtractTextService()
            {
                ElementLayer = "AI-房间名称",
            };
            textService.Extract(database, pts);
            textService.Texts.ForEach(o => Transformer.Transform(o));
            // 获取洞包括的文字
            var textInfoService = new ThSprinklerQueryHoleTextInfoService();
            HoleDic = textInfoService.Query(holes, textService.Texts);
        }

        public void Print(Database database)
        {
            HoleDic.Select(o => o.Key).Cast<Entity>().ToList().CreateGroup(database, ColorIndex);
        }

        public ThStoreyInfo Query(Entity entity)
        {
            var results = StoreyInfos.Where(o => o.Boundary.EntityContains(entity));
            return results.Count() > 0 ? results.First() : new ThStoreyInfo();
        }

        public void Transform()
        {
            Transformer.Transform(HoleDic.Keys.ToCollection());
        }

        public void Reset()
        {
            Transformer.Reset(HoleDic.Keys.ToCollection());
        }
    }
}
