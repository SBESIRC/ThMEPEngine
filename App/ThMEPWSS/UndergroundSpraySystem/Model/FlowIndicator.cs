using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
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

        public string BlockName = "信号阀＋水流指示器";
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
                    .OfType<BlockReference>()
                    //.Where(o => IsTargetLayer(o.Layer))
                    .Where(o => IsTarget(o))
                    .ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);

                dbObjs.Cast<Entity>()
                    .ForEach(e => DBObjs.Add(ExplodeValve(e)));
            }
        }

        private bool IsTarget(Entity entity)
        {
            if (entity is BlockReference blockReference)
            {
                var blkName = blockReference.GetEffectiveName().ToUpper();
                return blkName.Equals(BlockName) ||
                    (blkName.Contains("VALVE") && blkName.Contains("531"));
            }
            else if (entity.IsTCHValve())
            {
                var objs = new DBObjectCollection();
                entity.Explode(objs);
                if (objs[0] is BlockReference bkr)
                {
                    var blkName = bkr.Name.ToUpper();
                    return blkName.Contains(BlockName) ||
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

        public List<Point3dEx> CreatePts(SprayIn sprayIn)
        {
            var pts = new List<Point3dEx>();
            foreach (var db in DBObjs)
            {
                if(db is BlockReference br)
                {
                    var bounds = br.Bounds;
                    var pt = new Point2d((bounds.Value.MaxPoint.X + bounds.Value.MinPoint.X) / 2,
                        (bounds.Value.MaxPoint.Y + bounds.Value.MinPoint.Y) / 2);
                    var newpt = pt.ToPoint3d();
                    pts.Add(new Point3dEx(newpt));
                    sprayIn.FlowTypeDic.Add(new Point3dEx(newpt), br.ObjectId.GetDynBlockValue("可见性"));

                }
            }
            return pts;
        }
        public DBObjectCollection CreatBlocks()
        {
            DBObjectCollection result = new DBObjectCollection();
            foreach (var db in DBObjs)
            {
                if (db is BlockReference br)
                {
                    result.Add((DBObject)db);
                }
            }
            return result;
        }
    }
}
