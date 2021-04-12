using System;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThDrainageFacilityExtractor : ThExtractorBase,IExtract,IPrint, IBuildGeometry,IGroup
    {
        public List<Curve> DrainageFacilities { get; private set; }      
        public ThDrainageFacilityExtractor()
        {
            DrainageFacilities = new List<Curve>();
            Category = "DrainageFacility";
            ElementLayer = "排水设施";
        }
        public void Extract(Database database, Point3dCollection pts)
        {
            var instance = new ThExtractDrainageFacilityService()
            {
                ElementLayer = this.ElementLayer,
            };
            instance.Extract(database, pts);
            DrainageFacilities = instance.Facilities;
        }

        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            DrainageFacilities.ForEach(o =>
            {                
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                if(o is Line)
                {
                    geometry.Properties.Add(NamePropertyName, "排水沟");
                    geometry.Boundary = o;
                }
                else if (o is Polyline polyline)
                {
                    if(polyline.Closed)
                    {
                        geometry.Properties.Add(NamePropertyName, "集水井");
                    }
                    else
                    {
                        geometry.Properties.Add(NamePropertyName, "排水沟");
                    }
                    geometry.Boundary = o;
                }
                else if(o is Circle circle)
                {
                    geometry.Properties.Add(NamePropertyName, "地漏");
                    geometry.Boundary = ToRectangle(circle);
                }
                geos.Add(geometry);
            });
            return geos;
        }

        private Polyline ToRectangle(Circle circle)
        {
            var pt1 = new Point2d(circle.Center.X + circle.Radius, circle.Center.Y + circle.Radius);
            var pt2 = new Point2d(circle.Center.X - circle.Radius, circle.Center.Y + circle.Radius);
            var pt3 = new Point2d(circle.Center.X - circle.Radius, circle.Center.Y - circle.Radius);
            var pt4 = new Point2d(circle.Center.X + circle.Radius, circle.Center.Y - circle.Radius);

            var poly = new Polyline()
            {
                Closed = true
            };
            poly.AddVertexAt(0, pt1, 0, 0, 0);
            poly.AddVertexAt(1, pt2, 0, 0, 0);
            poly.AddVertexAt(2, pt3, 0, 0, 0);
            poly.AddVertexAt(3, pt4, 0, 0, 0);
            return poly;
        }

        public void Print(Database database)
        {
            using (var db = AcadDatabase.Use(database))
            {
                var drainageFacilityIds = new ObjectIdList();
                DrainageFacilities.ForEach(o =>
                {
                    o.ColorIndex = ColorIndex;
                    o.SetDatabaseDefaults();
                    drainageFacilityIds.Add(db.ModelSpace.Add(o));
                });
                if (drainageFacilityIds.Count > 0)
                {
                    GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), drainageFacilityIds);
                }
            }
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            throw new NotImplementedException();
        }
    }
}
