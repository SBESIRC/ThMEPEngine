using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.IO;

namespace ThMEPEngineCore.GeojsonExtractor
{
    public class ThRailingExtractor : ThExtractorBase, IPrint, IGroup
    {
        public List<Polyline> Railing { get; protected set; }
        public ThRailingExtractor()
        {
            Railing = new List<Polyline>();
            Category = BuiltInCategory.Railing.ToString();
            ElementLayer = "栏杆";
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Railing.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                if(GroupSwitch)
                {
                    geometry.Properties.Add(ThExtractorPropertyNameManager.GroupIdPropertyName, BuildString(GroupOwner,o));
                }
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            if (UseDb3Engine)
            {
                var railingEngine = new ThDB3RailingRecognitionEngine();
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
            //Railing.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
            foreach (var o in Railing)
            {
                if (GroupOwner.ContainsKey(o) == false)
                {
                    GroupOwner.Add(o, FindCurveGroupIds(groupId, o));
                }
                else
                {
                    GroupOwner[o] = FindCurveGroupIds(groupId, o);
                }
            }
        }

        public void Print(Database database)
        {
            Railing.Cast<Entity>().ToList().CreateGroup(database, ColorIndex);
        }
        public override List<Entity> GetEntities()
        {
            return Railing.Cast<Entity>().ToList();
        }
    }
}
