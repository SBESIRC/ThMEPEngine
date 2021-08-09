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
    public class ThDoorOpeningExtractor :ThExtractorBase , IExtract , IPrint, IBuildGeometry,IGroup
    {
        public List<ThIfcDoor> Doors { get; protected set; }

        private const string SwitchPropertyName = "Switch";
        private const string UsePropertyName = "Use";

        public ThDoorOpeningExtractor()
        {
            Doors = new List<ThIfcDoor>();
            Category = "DoorOpening";
            ElementLayer = "门";
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            if (UseDb3Engine)
            {
                var doorEngine = new ThDB3DoorRecognitionEngine();
                doorEngine.Recognize(database, pts);
                Doors=doorEngine.Elements.Cast<ThIfcDoor>().ToList();
            }
            else
            {
                var instance = new ThExtractDoorOpeningService()
                {
                    ElementLayer = this.ElementLayer,
                };
                instance.Extract(database, pts);
                Doors = instance.Openings.Select(o => new ThIfcDoor() { Outline = o }).ToList();
            }
        }

        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Doors.ForEach(o =>
            {                
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                if (GroupSwitch)
                {
                    geometry.Properties.Add(GroupIdPropertyName, BuildString(GroupOwner, o.Outline));
                }
                geometry.Boundary = o.Outline;

                geos.Add(geometry);
            });
            return geos;
        }

        public void Print(Database database)
        {
            using (var db =AcadDatabase.Use(database))
            {
                var doorIds = new ObjectIdList();
                Doors.ForEach(o =>
                {
                    o.Outline.ColorIndex = ColorIndex;
                    o.Outline.SetDatabaseDefaults();
                    doorIds.Add(db.ModelSpace.Add(o.Outline));                    
                });
                if (doorIds.Count > 0)
                {
                    GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), doorIds);
                }
            }
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            if (GroupSwitch)
            {
                Doors.ForEach(o => GroupOwner.Add(o.Outline, FindCurveGroupIds(groupId, o.Outline)));
            }
        }
    }
}
