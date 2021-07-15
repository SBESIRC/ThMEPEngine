using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    class ThRailingExtractor : ThExtractorBase, IExtract, IBuildGeometry, IPrint, IGroup
    {
        public List<Polyline> Railing { get; private set; }
        public ThRailingExtractor()
        {
            Railing = new List<Polyline>();
            Category = BuiltInCategory.Railing.ToString();
            ElementLayer = "栏杆";
        }
        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Railing.ForEach(o =>
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
                var railingEngine = new ThRailingRecognitionEngine();
                railingEngine.Recognize(database, pts);
                Railing = railingEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            }
            else
            {
                var extractService = new ThExtractPolylineService()
                {
                    ElementLayer = this.ElementLayer,
                };
                extractService.Extract(database, pts);
                Railing = extractService.Polys;
            }
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            if (GroupSwitch)
            {
                Railing.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
            }
        }

        public override void Group2(Dictionary<Entity, string> groupId)
        {
            if (Group2Switch)
            {
                Railing.ForEach(o => Group2Owner.Add(o, FindCurveGroupIds(groupId, o)));
            }
        }

        public void Print(Database database)
        {
            Railing.Cast<Entity>().ToList().CreateGroup(database, ColorIndex);
        }
    }
}
