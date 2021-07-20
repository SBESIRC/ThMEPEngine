using System;
using System.Linq;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    class ThHoleExtractor : ThExtractorBase, IExtract, IBuildGeometry, IPrint, IGroup
    {
        public List<Polyline> Hole { get; private set; }

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
                if (GroupSwitch)
                {
                    geometry.Properties.Add(GroupIdPropertyName, BuildString(GroupOwner, o));
                }
                if (Group2Switch)
                {
                    geometry.Properties.Add(Group2IdPropertyName, BuildString(Group2Owner, o));
                }
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

        public override void Group2(Dictionary<Entity, string> groupId)
        {
            if (Group2Switch)
            {
                Hole.ForEach(o => Group2Owner.Add(o, FindCurveGroupIds(groupId, o)));
            }
        }
        public void Print(Database database)
        {
            Hole.Cast<Entity>().ToList().CreateGroup(database, ColorIndex);
        }
    }
}
