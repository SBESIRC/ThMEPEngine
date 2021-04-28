using System;
using System.Linq;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThRainshedExtractor : ThExtractorBase, IExtract,IPrint, IBuildGeometry,IGroup
    {
        public List<Entity> Rainsheds { get; private set; }
        public ThRainshedExtractor()
        {
            Rainsheds = new List<Entity>();
            Category = "雨棚";
            ElementLayer = "雨棚";
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            var instance = new ThExtractRainshedService()
            {
                ElementLayer = this.ElementLayer,
                Types = this.Types
            };
            instance.Extract(database, pts);
            Rainsheds = instance.Rainsheds.Select(o => o).ToList();
        }

        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Rainsheds.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                var eles = IEleQuery.Query(o);
                if (eles.Count > 0)
                {
                    geometry.Properties.Add(ElevationPropertyName, eles);
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
                Rainsheds.ForEach(o =>
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
