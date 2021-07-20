using System;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using System.Linq;

namespace ThMEPEngineCore.Temp
{
    public class ThArchitectureWallExtractor : ThExtractorBase, IExtract,IPrint, IBuildGeometry,IGroup
    {
        public List<Entity> Walls { get; private set; }
        private List<ThTempSpace> Spaces { get; set; }
        public ThArchitectureWallExtractor()
        {
            Walls = new List<Entity>();
            Category = "ArchitectureWall";
            UseDb3Engine = true;
            Spaces = new List<ThTempSpace>();
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            if(UseDb3Engine)
            {
                using (var engine = new ThDB3ArchWallRecognitionEngine())
                {
                    engine.Recognize(database, pts);
                    engine.Elements.ForEach(o => Walls.Add(o.Outline));
                }
            }
            else
            {
                var instance = new ThExtractPolylineService()
                {
                    ElementLayer = this.ElementLayer,
                };
                instance.Extract(database, pts);
                Walls = instance.Polys.Cast<Entity>().ToList();
            }
        }

        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Walls.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                if (IsolateSwitch)
                {
                    var isolate = IsIsolate(Spaces, o);
                    geometry.Properties.Add(IsolatePropertyName, isolate);
                }
                if (GroupSwitch)
                {
                    geometry.Properties.Add(GroupIdPropertyName, BuildString(GroupOwner, o));
                }
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public void Print(Database database)
        {
            using (var db=AcadDatabase.Use(database))
            {
                var wallIds = new ObjectIdList();
                Walls.ForEach(o =>
                {
                    o.ColorIndex = ColorIndex;
                    o.SetDatabaseDefaults();
                    wallIds.Add(db.ModelSpace.Add(o));
                });
                if (wallIds.Count > 0)
                {
                    GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), wallIds);
                }
            }
        }
        public void SetSpaces(List<ThTempSpace> spaces)
        {
            Spaces = spaces;
        }
        public void Group(Dictionary<Entity, string> groupId)
        {
            if(GroupSwitch)
            {
                Walls.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
            }
        }
    }
}
