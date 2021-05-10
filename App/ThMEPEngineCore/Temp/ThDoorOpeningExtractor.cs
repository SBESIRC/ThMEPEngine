using System;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThDoorOpeningExtractor :ThExtractorBase , IExtract , IPrint, IBuildGeometry,IGroup
    {
        public List<Polyline> Openings { get; private set; }

        private const string SwitchPropertyName = "Switch";

        public ThDoorOpeningExtractor()
        {
            Openings = new List<Polyline>();
            Category = "DoorOpening";
            ElementLayer = "门";
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            var instance = new ThExtractDoorOpeningService()
            {
                ElementLayer= this.ElementLayer,
            };
            instance.Extract(database, pts);
            Openings = instance.Openings;
        }

        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Openings.ForEach(o =>
            {                
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                geometry.Properties.Add(SwitchPropertyName, "Open");
                geometry.Boundary = o;
                geos.Add(geometry);
            });

            return geos;
        }

        public void Print(Database database)
        {
            using (var db =AcadDatabase.Use(database))
            {
                var doorIds = new ObjectIdList();
                Openings.ForEach(o =>
                {
                    o.ColorIndex = ColorIndex;
                    o.SetDatabaseDefaults();
                    doorIds.Add(db.ModelSpace.Add(o));                    
                });
                if (doorIds.Count > 0)
                {
                    GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), doorIds);
                }
            }
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            throw new NotImplementedException();
        }
    }
}
