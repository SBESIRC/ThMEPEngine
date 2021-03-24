using System;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThParkingStallExtractor : ThExtractorBase,IExtract,IPrint, IBuildGeometry,IGroup
    {
        public List<ThIfcRoom> ParkingStalls { get; private set; }
        public ThParkingStallExtractor()
        {
            ParkingStalls = new List<ThIfcRoom>();
            Category = "ParkingStall";
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            using (var engine = new ThParkingStallRecognitionEngine())
            {
                engine.Recognize(database, pts);
                ParkingStalls = engine.Rooms;
            }
        }

        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            ParkingStalls.ForEach(o =>
            {                
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                geometry.Boundary = o.Boundary;
                geos.Add(geometry);
            });
            return geos;
        }

        public void Print(Database database)
        {
            using (var db =AcadDatabase.Use(database))
            {
                var parkingStallIds = new ObjectIdList();
                ParkingStalls.ForEach(o =>
                {
                    o.Boundary.ColorIndex = ColorIndex;
                    o.Boundary.SetDatabaseDefaults();
                    parkingStallIds.Add(db.ModelSpace.Add(o.Boundary));
                });
                if (parkingStallIds.Count > 0)
                {
                    GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), parkingStallIds);
                }
            }
        }

        public void Group(Dictionary<Polyline, string> groupId)
        {
            throw new NotImplementedException();
        }
    }
}
