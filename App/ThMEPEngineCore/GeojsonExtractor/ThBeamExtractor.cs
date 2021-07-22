using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.IO;

namespace ThMEPEngineCore.GeojsonExtractor
{
    public class ThBeamExtractor : ThExtractorBase, IPrint,IGroup
    {
        public List<Polyline> Beams { get; set; }
        public ThBeamExtractor()
        {
            Beams = new List<Polyline>();
            UseDb3Engine = true;
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            throw new NotImplementedException();
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Beams.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public void Print(Database database)
        {
            throw new NotImplementedException();
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            if (GroupSwitch)
            {
                Beams.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
            }
        }
    }
}
