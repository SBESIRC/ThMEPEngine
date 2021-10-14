using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    public class FlowIndicator
    {
        public DBObjectCollection DBObjs { get; set; }
        public FlowIndicator()
        {
            DBObjs = new DBObjectCollection();
        }
        public void Extract(Database database, Point3dCollection polygon)
        {
            var objs = new DBObjectCollection();
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = acadDatabase
                    .ModelSpace
                    .OfType<Entity>()
                    .Where(o => IsTargetLayer(o.Layer))
                    .Where(o => IsTarget(o))
                    .ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);

                dbObjs.Cast<Entity>()
                    .ForEach(e => DBObjs.Add(ExplodeValve(e)));
            }
        }
        private bool IsTargetLayer(string layer)
        {
            return layer.ToUpper() == "W-FRPT-HYDT-EQPM" ||
                   layer.ToUpper() == "W-FRPT-SPRL-EQPM";
        }
        private bool IsTarget(Entity entity)
        {
            if (entity is BlockReference blockReference)
            {
                var blkName = blockReference.GetEffectiveName().ToUpper();
                return blkName.Contains("水流指示器") ||
                    (blkName.Contains("VALVE") && blkName.Contains("531"));
            }
            else if (entity.IsTCHValve())
            {
                var objs = new DBObjectCollection();
                entity.Explode(objs);
                if (objs[0] is BlockReference bkr)
                {
                    var blkName = bkr.Name.ToUpper();
                    return blkName.Contains("水流指示器") ||
                           (blkName.Contains("VALVE") && blkName.Contains("531"));
                }
            }
            return false;
        }
        private BlockReference ExplodeValve(Entity entity)
        {
            if (entity is BlockReference bkr)
            {
                return bkr;
            }
            else
            {
                var objs = new DBObjectCollection();
                entity.Explode(objs);
                return (BlockReference)objs[0];
            }
        }

        public List<Point3dEx> CreatePts()
        {
            var pts = new List<Point3dEx>();
            foreach (var db in DBObjs)
            {
                if(db is BlockReference br)
                {
                    pts.Add(new Point3dEx(br.Position));
                }
            }
            return pts;
        }
    }
}
