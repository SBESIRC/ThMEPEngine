using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;

namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    public class PipeDN
    {
        public DBObjectCollection DBObjs { get; set; }
        public PipeDN()
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
                   .Where(o => IsTargetLayer(o.Layer))
                   .ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                foreach(var polygon in sprayIn.FloorRectDic.Values)
                {
                    var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                    dbObjs.Cast<Entity>()
                        .Where(e => e is DBText)
                        .Where(e => (e as DBText).TextString.Contains("DN"))
                        .ForEach(e => DBObjs.Add(e));

                    dbObjs.Cast<Entity>()
                        .Where(e => e.IsTCHText())
                        .Where(e => (e.ExplodeTCHText()[0] as DBText).TextString.Contains("DN"))
                        .ForEach(e => DBObjs.Add(e.ExplodeTCHText()[0]));
                }
            }
        }
        private bool IsTargetLayer(string layer)
        {
            return layer.ToUpper().StartsWith("W-") && 
                   layer.ToUpper().Contains("-DIMS");
        }
    }
}
