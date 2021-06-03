using System;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThShearWallExtractor : ThExtractorBase, IExtract, IPrint, IBuildGeometry, IGroup
    {
        public List<Entity> Walls { get; private set; }
        private List<ThTempSpace> Spaces { get; set; }
        public ThShearWallExtractor()
        {
            Walls = new List<Entity>();
            Category = "ShearWall";
            UseDb3Engine = true;
            Spaces = new List<ThTempSpace>();
            ElementLayer = "剪力墙";
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            if (UseDb3Engine)
            {
                using (var engine = new ThShearWallRecognitionEngine())
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
                if(IsolateSwitch)
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
