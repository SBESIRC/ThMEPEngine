#if ACAD2016
using ThCADCore.NTS;
using NetTopologySuite.IO;
using Autodesk.AutoCAD.DatabaseServices;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;
using CLI;

namespace ThMEPEngineCore.Algorithm
{
    public class ThMEPPolygonPartitionService
    {
        public static DBObjectCollection EarCut(AcPolygon polygon)
        {
            var reader = new WKTReader();
            var writer = new WKTWriter();
            var wkb = writer.Write(polygon.ToNTSPolygon());
            var repairer = new ThPolyPartitionMgd();
            var result = repairer.TriangulateEC(wkb);
            return reader.Read(result).ToDbCollection();
        }
    }
}

#endif
