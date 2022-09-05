using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.Assistant;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Model;
using ThMEPWSS.Pipe.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Extract
{
    public class ThExtractHYDTPipeService//提取管道
    {
        public static ThTCHPipeInfo TCHPipeInfo;

        public ThExtractHYDTPipeService()
        {
            TCHPipeInfo = new ThTCHPipeInfo();
        }

        public DBObjectCollection Extract(Database database, Point3dCollection polygon)
        {
            using var acadDatabase = AcadDatabase.Use(database);
            var entities = acadDatabase.ModelSpace.OfType<Entity>().ToList();
            var tchPipe = entities.Where(e => IsHYDTPipeLayer(e.Layer)).Where(e => e.IsTCHPipe()).FirstOrDefault();
            if (tchPipe != null)
            {
                TCHPipeInfo.HasTCHPipe = true;
                TCHPipeInfo.System = SystemMap(tchPipe.Layer);
            }
            var lines = ThDrainageSystemServiceGeoCollector.GetLines(entities, IsHYDTPipeLayer);
            return GeoFac.CreateIntersectsSelector(lines.Select(x => x.ToLineString()).ToList())
                (polygon.ToRect().ToPolygon()).
                SelectMany(x => x.ToDbCollection().OfType<DBObject>()).ToCollection();
        }

        public static bool IsHYDTPipeLayer(string layer)
        {
            return layer.Contains("W-FRPT") && layer.Contains("HYDT") && layer.Contains("PIPE");
        }

        private string SystemMap(string layer)
        {
            return layer switch
            {
                "W-FRPT-HYDT-PIPE" => "消防",
                "W-FRPT-HYDT-PIPE-0" => "消防转输管",
                "W-FRPT-HYDT-PIPE-1" => "消火栓1区",
                "W-FRPT-HYDT-PIPE-2" => "消火栓2区",
                "W-FRPT-HYDT-PIPE-3" => "消火栓3区",
                "W-FRPT-HYDT-PIPE-预留1" => "消防预留管线1",
                "W-FRPT-HYDT-PIPE-预留2" => "消防预留管线2",
                "W-FRPT-HYDT-PIPE-XW" => "室外消火栓管",
                _ => "消防",
            };
        }
    }
}
