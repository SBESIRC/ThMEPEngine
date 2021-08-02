using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThWindowExtractor : ThExtractorBase, IExtract, IBuildGeometry, IPrint, IGroup
    {
        public List<Polyline> Windows { get; private set; }
        public ThWindowExtractor()
        {
            Windows = new List<Polyline>();
            Category = BuiltInCategory.Window.ToString();
        }
        public void Extract(Database database, Point3dCollection pts)
        {
            if(UseDb3Engine)
            {
                var windowEngine = new ThDB3WindowRecognitionEngine();
                windowEngine.Recognize(database, pts);
                Windows = windowEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            }
            else
            {
                var extractService = new ThExtractPolylineService()
                { 
                    ElementLayer =this.ElementLayer,
                };
                extractService.Extract(database, pts);
                Windows = extractService.Polys;
            }
        }
        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Windows.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                if (GroupSwitch)
                {
                    geometry.Properties.Add(GroupIdPropertyName, BuildString(GroupOwner, o));
                }
                geometry.Boundary = o;
                geos.Add(geometry);
            });

            return geos;
        }       

        public void Group(Dictionary<Entity, string> groupId)
        {
            if(GroupSwitch)
            {
                Windows.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
            }
        }

        public void Print(Database database)
        {
            Windows.Cast<Entity>().ToList().CreateGroup(database, ColorIndex);
        }
    }
}
