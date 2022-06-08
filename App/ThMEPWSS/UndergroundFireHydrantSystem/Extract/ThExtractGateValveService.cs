using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore;
using ThMEPEngineCore.Algorithm;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Extract
{
    public class ThExtractGateValveService//提取闸阀
    {
        public DBObjectCollection Extract(Database database, Point3dCollection polygon)
        {
            var objs = new DBObjectCollection();
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => IsHYDTPipeLayer(o.Layer));
                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                // 阀块
                dbObjs.Cast<Entity>()
                    .Where(e => e is BlockReference)
                    .Where(e => IsValveBlock((BlockReference)e))
                    .ForEach(e => objs.Add(e));
                // 天正阀
                foreach (var obj in dbObjs)
                {
                    if ((obj as Entity).IsTCHValve())
                    {
                        var dbColl = new DBObjectCollection();
                        (obj as Entity).Explode(dbColl);
                        dbColl.Cast<Entity>()
                            .Where(e => e is BlockReference)
                            .Where(e => IsValve((e as BlockReference).Name))
                            .ForEach(e => objs.Add(e));
                    }
                }
                return objs;
            }
        }

        private bool IsHYDTPipeLayer(string layer)
        {
            return layer.ToUpper() == "W-FRPT-HYDT-EQPM";
        }

        private bool IsValveBlock(BlockReference blockReference)
        {
            var blkName = blockReference.GetEffectiveName().ToUpper();
            return blkName.Contains("截止阀") ||
                   blkName.Contains("闸阀") ||
                   blkName.Contains("296");
        }

        private bool IsValve(string valveName)
        {
            return valveName.Contains("截止阀") ||
                   valveName.Contains("296") ||
                   valveName.Contains("闸阀");
        }

        public List<Point3d> GetGateValveSite(DBObjectCollection objs)
        {
            var pts = new List<Point3d>();
            foreach (var db in objs)
            {
                var br = db as BlockReference;
                var pt1 = br.GeometricExtents.MaxPoint;
                var pt2 = br.GeometricExtents.MinPoint;
                var pt = General.GetMidPt(pt1, pt2);
                pts.Add(pt);
            }
#if DEBUG
            var layer = "闸阀标记";
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                if (!acad.Layers.Contains(layer))
                {
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, layer, 2);
                }
            }

            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                foreach (var pt in pts)
                {
                    var c = new Circle(pt, new Vector3d(0, 0, 1), 200);
                    c.LayerId = DbHelper.GetLayerId(layer);
                    acadDatabase.CurrentSpace.Add(c);
                }
            }
#endif
            return pts;
        }
    }
}
