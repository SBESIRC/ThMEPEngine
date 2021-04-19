using System;
using System.Collections.Generic;
using Linq2Acad;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Temp
{
    public class ThEquipmentExtractor :ThExtractorBase,IExtract,IPrint, IBuildGeometry,IGroup
    {
        private List<DBPoint> Extinguishers { get; set; }
        private List<DBPoint> Hydrants { get; set; }
        public ThEquipmentExtractor()
        {
            Extinguishers = new List<DBPoint>();
            Hydrants = new List<DBPoint>();
            Category = "Equipment";
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            var extinguisherService = new ThExtractExtinguisherService();
            extinguisherService.Extract(database, pts);
            Extinguishers = extinguisherService.Extinguishers;

            var hydrantService = new ThExtractHydrantService();
            hydrantService.Extract(database, pts);
            Hydrants = hydrantService.Hydrants;
        }

        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Extinguishers.ForEach(e =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                geometry.Properties.Add(NamePropertyName, "灭火器");
                geometry.Boundary = e;
                geos.Add(geometry);
            });

            Hydrants.ForEach(e =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                geometry.Properties.Add(NamePropertyName, "消火栓");
                geometry.Boundary = e;
                geos.Add(geometry);
            });
            return geos;
        }

        public void Print(Database database)
        {
            using (var db =AcadDatabase.Use(database))
            {
                var equipIds = new ObjectIdList();
                Extinguishers.ForEach(e =>
                {
                    var circle = new Circle(e.Position,Vector3d.ZAxis,5.0);
                    circle.ColorIndex = ColorIndex;
                    circle.SetDatabaseDefaults();
                    equipIds.Add(db.ModelSpace.Add(circle));
                });

                Hydrants.ForEach(e =>
                {
                    var circle = new Circle(e.Position, Vector3d.ZAxis, 5.0);
                    circle.ColorIndex = ColorIndex;
                    circle.SetDatabaseDefaults();
                    equipIds.Add(db.ModelSpace.Add(circle));
                });

                if (equipIds.Count > 0)
                {
                    GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), equipIds);
                }
            }
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            throw new NotImplementedException();
        }
    }
}
