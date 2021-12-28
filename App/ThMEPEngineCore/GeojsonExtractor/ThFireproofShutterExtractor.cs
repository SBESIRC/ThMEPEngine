using System.Linq;
using ThMEPEngineCore.IO;
using System.Collections.Generic;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.GeojsonExtractor.Service;

namespace ThMEPEngineCore.GeojsonExtractor
{
    public class ThFireproofShutterExtractor : ThExtractorBase, IPrint, IGroup
    {
        public List<Polyline> FireproofShutter { get; protected set; }

        public ThFireproofShutterExtractor()
        {
            FireproofShutter = new List<Polyline>();
            Category = BuiltInCategory.FireproofShutter.ToString();
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            FireproofShutter.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, BuildString(GroupOwner, o));
                geometry.Boundary = o;
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
            FireproofShutter = extractService.Polys;
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            //FireproofShutter.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
            foreach (var o in FireproofShutter)
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
            FireproofShutter.Cast<Entity>().ToList().CreateGroup(database, ColorIndex);
        }
    }
}
