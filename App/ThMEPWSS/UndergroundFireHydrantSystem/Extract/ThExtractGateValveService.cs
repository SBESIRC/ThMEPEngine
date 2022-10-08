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
                        var blk = dbColl[0] as BlockReference;
                        if(IsValve(blk.Name))
                        {
                            objs.Add(blk);
                        }
                        //dbColl.Cast<Entity>()
                        //    .Where(e => e is BlockReference)
                        //    .Where(e => IsValve((e as BlockReference).Name))
                        //    .ForEach(e => objs.Add(e));
                    }
                }
                return objs;
            }
        }

        private bool IsHYDTPipeLayer(string layer)
        {
            return layer.Contains("EQPM");
        }

        private bool IsValveBlock(BlockReference blockReference)
        {
            var blkName = blockReference.GetEffectiveName().ToUpper();
            return blkName.Contains("截止阀") ||
                   blkName.Contains("闸阀") ||
                   blkName.Contains("296") ||
                   blkName.Contains("019");
        }

        private bool IsValve(string valveName)
        {
            return valveName.Contains("截止阀") ||
                   valveName.Contains("296") ||
                   valveName.Contains("闸阀")||
                   valveName.Contains("019");
        }

        public List<Point3d> GetGateValveSite(DBObjectCollection objs)
        {
            var pts = new List<Point3d>();
            foreach (var db in objs)
            {
                var br = db as BlockReference;
                
                //var pt1 = br.GeometricExtents.MaxPoint;
                //var pt2 = br.GeometricExtents.MinPoint;
                //var pt = General.GetMidPt(pt1, pt2);
                pts.Add(br.Position);
            }
            return pts;
        }
    }
}
