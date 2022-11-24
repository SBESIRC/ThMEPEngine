using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;

namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    internal class AlarmValveTCH
    {
        public DBObjectCollection DBObjs { get; set; }

        public AlarmValveTCH()
        {
            DBObjs = new DBObjectCollection();
        }

        public List<Point3d> Extract(Database database, Point3dCollection polygon)
        {
            var objs = new DBObjectCollection();
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = acadDatabase
                    .ModelSpace
                    .OfType<Entity>()
                    .Where(o => IsTarget(o))
                    .ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);

                dbObjs.Cast<Entity>()
                    .ForEach(e => DBObjs.Add(ExplodeValve(e)));

                var pts = new List<Point3d>();
                foreach (var obj in DBObjs)
                {
                    pts.Add((obj as BlockReference).Position.ToPoint2D().ToPoint3d());
                }
                return pts;
            }
        }

        private bool IsTarget(Entity entity)
        {
            if (entity is BlockReference blockReference)
            {
                var blkName = blockReference.GetEffectiveName().ToUpper();
                return blkName.Contains("湿式报警阀平面") ||
                    (blkName.Contains("VALVE") && (blkName.Contains("520") || blkName.Contains("701")));
            }
            else if (entity.IsTCHValve())
            {
                var objs = new DBObjectCollection();
                entity.Explode(objs);
                if (objs[0] is BlockReference bkr)
                {
                    var blkName = bkr.Name.ToUpper();
                    return blkName.Contains("湿式报警阀平面") ||
                           (blkName.Contains("VALVE") && (blkName.Contains("520") || blkName.Contains("701")));
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
    }
}
