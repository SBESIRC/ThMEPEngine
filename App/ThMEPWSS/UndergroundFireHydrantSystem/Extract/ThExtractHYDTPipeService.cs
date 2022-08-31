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
            return "消防";
        }
    }
}
