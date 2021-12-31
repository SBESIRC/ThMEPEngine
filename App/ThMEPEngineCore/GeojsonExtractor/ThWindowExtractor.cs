using System.Linq;
using ThMEPEngineCore.IO;
using System.Collections.Generic;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.GeojsonExtractor.Service;

namespace ThMEPEngineCore.GeojsonExtractor
{
    public class ThWindowExtractor : ThExtractorBase, IGroup,IPrint
    {
        public List<Polyline> Windows { get; protected set; }
        public ThWindowExtractor()
        {
            Windows = new List<Polyline>();
            Category = BuiltInCategory.Window.ToString();
        }
        public override void Extract(Database database, Point3dCollection pts)
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
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Windows.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                if (GroupSwitch)
                {
                    geometry.Properties.Add(ThExtractorPropertyNameManager.GroupIdPropertyName, BuildString(GroupOwner, o));
                }
                geometry.Boundary = o;
                geos.Add(geometry);
            });

            return geos;
        }       

        public void Group(Dictionary<Entity, string> groupId)
        {
            //Windows.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
            foreach (var o in Windows)
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
            Windows.Cast<Entity>().ToList().CreateGroup(database, ColorIndex);
        }

        public override List<Entity> GetEntities()
        {
            return Windows.Cast<Entity>().ToList();
        }
    }
}
