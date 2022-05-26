using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPWSS.Assistant;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Pipe.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Extract
{
    public class ThExtractHYDTPipeService//提取管道
    {
        public ThExtractHYDTPipeService()
        {
        }

        public DBObjectCollection Extract(Database database, Point3dCollection polygon)
        {
            using var acadDatabase = AcadDatabase.Use(database);
            var lines = ThDrainageSystemServiceGeoCollector.GetLines(
                acadDatabase.ModelSpace.OfType<Entity>().ToList(),
                IsHYDTPipeLayer);
            return GeoFac.CreateIntersectsSelector(lines.Select(x => x.ToLineString()).ToList())
                (polygon.ToRect().ToPolygon()).
                SelectMany(x => x.ToDbCollection().OfType<DBObject>()).ToCollection();
        }

        public static bool IsHYDTPipeLayer(string layer)
        {
            return layer.Contains("W-FRPT") && layer.Contains("HYDT") && layer.Contains("PIPE");
        }
    }
}
