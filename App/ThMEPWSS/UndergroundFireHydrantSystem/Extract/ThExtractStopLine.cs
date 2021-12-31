using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Extract
{
    public class ThExtractStopLine
    {
        public List<Point3dEx> Extract(Database database, Point3dCollection polygon)
        {
            var objs = new DBObjectCollection();
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => o.Layer == "W-FRPT-HYDT-EQPM");
                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);

                dbObjs.Cast<Entity>()
                      .Where(e => e.IsTCHEquipment())
                      .ForEach(e => objs.Add(Explode(e)));

                var pts = new List<Point3dEx>();
                objs.Cast<Entity>()
                    .ForEach(e => pts.Add(new Point3dEx((e as BlockReference).Position)));
                return pts;
            }
        }
        private DBObject Explode(Entity entity)
        {
            var dbObjs = new DBObjectCollection();
            entity.Explode(dbObjs);
            return dbObjs[0];
        }
    }
}
