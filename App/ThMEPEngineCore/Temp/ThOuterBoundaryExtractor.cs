using System;
using DotNetARX;
using Linq2Acad;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThOuterBoundaryExtractor : ThExtractorBase , IExtract , IPrint, IBuildGeometry,IGroup
    {
        public List<Polyline> OuterBoundaries { get; private set; }        

        public ThOuterBoundaryExtractor()
        {
            OuterBoundaries = new List<Polyline>();
            Category = "OuterBoundary";
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            var instance = new ThExtractOuterBoundaryService()
            {
                ElementLayer= this.ElementLayer,
            };
            instance.Extract(database, pts);
            OuterBoundaries = instance.OuterBoundaries;
        }

        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            OuterBoundaries.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
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
                OuterBoundaries.ForEach(o =>
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
