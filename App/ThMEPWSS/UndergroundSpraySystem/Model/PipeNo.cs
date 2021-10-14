using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;

namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    public class PipeNo
    {
        public DBObjectCollection DBObjs { get; set; }
        public PipeNo()
        {
            DBObjs = new DBObjectCollection();
        }
        public void Extract(Database database, SprayIn sprayIn)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var pipeNumber = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => IsTargetLayer(o.Layer))
                   .ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(pipeNumber.ToCollection());
                foreach(var polygon in sprayIn.FloorRectDic.Values)
                {
                    var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                    dbObjs.Cast<Entity>()
                        .Where(e => IsTCHText(e))
                        .ForEach(e => ExplodeTCHText(e, DBObjs));
                }
            }
        }
        private bool IsTargetLayer(string layer)
        {
            return layer.ToUpper() == "W-FRPT-HYDT-EQPM";
        }
        private bool IsTCHText(Entity entity)
        {
            string dxfName = entity.GetRXClass().DxfName.ToUpper();
            return dxfName.StartsWith("TCH") && dxfName.Contains("PIPETEXT");
        }
        private void ExplodeTCHText(Entity entity, DBObjectCollection DBObjs)
        {
            var dbObjs = new DBObjectCollection();
            entity.Explode(dbObjs);
            dbObjs.Cast<Entity>()
                .Where(e => e.IsTCHText())
                .Where(e => (e.ExplodeTCHText()[0] as DBText).TextString.Contains("ZP"))
                .ForEach(e => DBObjs.Add(e.ExplodeTCHText()[0]));
        }
    }
}
