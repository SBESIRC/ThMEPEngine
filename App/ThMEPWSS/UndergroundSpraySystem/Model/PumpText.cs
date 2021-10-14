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
    public class PumpText
    {
        public DBObjectCollection DBObjs { get; set; }
        public PumpText()
        {
            DBObjs = new DBObjectCollection();
        }
        public void Extract(Database database, SprayIn sprayIn)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => o is not DBPoint)
                   .Where(o => IsTargetLayer(o.Layer.ToUpper()))
                   .ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                foreach (var polygon in sprayIn.FloorRectDic.Values)
                {
                    var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                    dbObjs.Cast<Entity>()
                        .Where(e => IsTCHNote(e))
                        .ForEach(e => ExplodeTCHNote(e, DBObjs));
                    dbObjs.Cast<Entity>()
                        .Where(e => e is DBText)
                        .ForEach(e => DBObjs.Add(e));
                }
            }
        }
        public List<DBText> GetTexts()
        {
            var dbTexts = new List<DBText>();
            DBObjs.Cast<Entity>()
                .ForEach(e => dbTexts.Add((DBText)e));
            return dbTexts;
        }
        private bool IsTargetLayer(string layer)
        {
            return layer.StartsWith("W-") &&
                  (layer.Contains("-DIMS") ||
                   layer.Contains("-NOTE") ||
                   layer.Contains("-FRPT-HYDT-DIMS") ||
                   layer.Contains("-SHET-PROF"));
        }
        private bool IsTCHNote(Entity entity)
        {
            try
            {
                return entity.GetType().Name.Equals("ImpEntity");

            }
            catch
            {
                ;
            }
            return false;
        }
        private void ExplodeTCHNote(Entity entity, DBObjectCollection DBObjs)
        {
            try
            {
                var dbObjs = new DBObjectCollection();
                entity.Explode(dbObjs);
                dbObjs.Cast<Entity>()
                    .Where(e => e.IsTCHText())
                    .Where(e => !(e.ExplodeTCHText()[0] as DBText).TextString.StartsWith("DN"))
                    .ForEach(e => DBObjs.Add(e.ExplodeTCHText()[0]));
                dbObjs.Cast<Entity>()
                    .Where(e => e is DBText)
                    .Where(e => !(e as DBText).TextString.StartsWith("DN"))
                    .ForEach(e => DBObjs.Add(e));
            }
            catch
            {
                ;
            }
            
        }
    }
}
