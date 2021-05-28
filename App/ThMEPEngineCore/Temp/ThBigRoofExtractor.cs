using System;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThBigRoofExtractor : ThExtractorBase, IExtract,IPrint, IBuildGeometry,IGroup
    {
        public List<Entity> BigRoofs { get; private set; }
        public ThBigRoofExtractor()
        {
            BigRoofs = new List<Entity>();
            Category = "BigRoof";
            ElementLayer = "大屋面";
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            var instance = new ThExtractBigRoofService()
            {
                ElementLayer = this.ElementLayer,
                Types = this.Types
            };
            instance.Extract(database, pts);
            BigRoofs = instance.BigRoofs.Select(o => o).ToList();
        }

        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            BigRoofs.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                geometry.Properties.Add(ElevationPropertyName, new List<double> { 48.0 });
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
                BigRoofs.ForEach(o =>
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
        public void Group(Dictionary<Entity, string> groupId)
        {
            //
        }
    }
}
