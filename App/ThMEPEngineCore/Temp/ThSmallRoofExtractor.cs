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
    public class ThSmallRoofExtractor : ThExtractorBase, IExtract,IPrint, IBuildGeometry,IGroup
    {
        public List<Entity> SmallRoofs { get; private set; }
        public ThSmallRoofExtractor()
        {
            SmallRoofs = new List<Entity>();
            Category = "SmallRoof";
            ElementLayer = "屋面";
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            var instance = new ThExtractSmallRoofService()
            {
                ElementLayer = this.ElementLayer,
                Types = this.Types
            };
            instance.Extract(database, pts);
            SmallRoofs = instance.SmallRoofs.Select(o => o).ToList();
        }

        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            SmallRoofs.ForEach(o =>
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
                SmallRoofs.ForEach(o =>
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
