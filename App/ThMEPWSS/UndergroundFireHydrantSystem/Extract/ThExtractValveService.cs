using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Extract
{
    public class ThExtractValveService//提取普通阀门
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
                dbObjs.Cast<Entity>()
                    .Where(e => e.IsTCHValve())
                    .ForEach(e => objs.Add(e));
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
            return blkName.Contains("蝶阀") || (blkName.Contains("VALVE") && blkName.Contains("316"));
        }
    }
}
