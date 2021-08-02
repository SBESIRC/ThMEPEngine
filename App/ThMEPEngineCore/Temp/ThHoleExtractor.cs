using System;
using System.Linq;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    class ThHoleExtractor : ThExtractorBase, IExtract, IBuildGeometry, IPrint, IGroup, ISetStorey
    {
        public List<Polyline> Hole { get; private set; }
        private List<StoreyInfo> StoreyInfos { get; set; }

        public ThHoleExtractor()
        {
            Hole = new List<Polyline>();
            Category = "Hole";
            ElementLayer = "洞";
        }
        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Hole.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                var parentId = BuildString(GroupOwner, o);
                if (string.IsNullOrEmpty(parentId))
                {
                    var storeyInfo = Query(o);
                    parentId = storeyInfo.Id;
                }
                geometry.Properties.Add(ParentIdPropertyName, parentId);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            if (UseDb3Engine)
            {
                throw new NotImplementedException();
            }
            else
            {
                var extractService = new ThExtractPolylineService()
                {
                    ElementLayer = this.ElementLayer,
                };
                extractService.Extract(database, pts);
                Hole = extractService.Polys;
            }
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            if (GroupSwitch)
            {
                Hole.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
            }
        }
        public void Print(Database database)
        {
            Hole.Cast<Entity>().ToList().CreateGroup(database, ColorIndex);
        }

        public void Set(List<StoreyInfo> storeyInfos)
        {
            StoreyInfos = storeyInfos;
        }

        public StoreyInfo Query(Entity entity)
        {
            var results = StoreyInfos.Where(o => o.Boundary.IsContains(entity));
            return results.Count() > 0 ? results.First() : new StoreyInfo();
        }
    }
}
