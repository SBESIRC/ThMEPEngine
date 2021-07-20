using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp 
{
    public class ThFireproofShutterExtractor : ThExtractorBase, IExtract, IBuildGeometry, IPrint, IGroup
    {
        public List<Polyline> FireproofShutter { get; private set; }

        public ThFireproofShutterExtractor()
        {
            FireproofShutter = new List<Polyline>();
            Category = BuiltInCategory.FireproofShutter.ToString();
        }
        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            FireproofShutter.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                geometry.Properties.Add(ParentIdPropertyName, BuildString(GroupOwner, o));
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            if (UseDb3Engine)
            {
                //
            }
            else
            {
                var extractService = new ThExtractPolylineService()
                {
                    ElementLayer = this.ElementLayer,
                };
                extractService.Extract(database, pts);
                FireproofShutter = extractService.Polys;
            }
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            if (GroupSwitch)
            {
                FireproofShutter.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
            }
        }

        public void Print(Database database)
        {
            FireproofShutter.Cast<Entity>().ToList().CreateGroup(database, ColorIndex);
        }
    }
}
