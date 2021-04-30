#if ACAD2016
using ThCADCore.NTS;
using NetTopologySuite.IO;
using Autodesk.AutoCAD.DatabaseServices;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;
using CLI;

namespace ThMEPEngineCore.Algorithm
{
    public class ThMEPPolygonRepairService
    {
        public static DBObjectCollection Repair(AcPolygon polygon)
        {
            var reader = new WKBReader();
            var writer = new WKBWriter();
            var wkb = writer.Write(polygon.ToNTSPolygon());
            var repairer = new ThPolygonRepairerMgd();
            var result = repairer.MakeValid(wkb);
            return reader.Read(result).ToDbCollection();
        }
    }
}

#endif