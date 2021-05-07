#if ACAD2016
using System.Linq;
using ThCADCore.NTS;
using NetTopologySuite.IO;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;
using CLI;

namespace ThMEPEngineCore.Algorithm
{
    public class ThMEPPolygonPartitionService
    {
        public static DBObjectCollection EarCut(AcPolygon shell, DBObjectCollection holes)
        {
            var reader = new WKTReader();
            var writer = new WKTWriter();
            var wkb = writer.Write(ToNTSPolygon(shell, holes));
            var repairer = new ThPolyPartitionMgd();
            var result = repairer.TriangulateEC(wkb);
            return reader.Read(result).ToDbCollection();
        }

        public static DBObjectCollection HMPartition(AcPolygon shell, DBObjectCollection holes)
        {
            var reader = new WKTReader();
            var writer = new WKTWriter();
            var wkb = writer.Write(ToNTSPolygon(shell, holes));
            var repairer = new ThPolyPartitionMgd();
            var result = repairer.HMPartition(wkb);
            return reader.Read(result).ToDbCollection();
        }

        private static Polygon ToNTSPolygon(AcPolygon shell, DBObjectCollection holes)
        {
            if (holes.Count == 0)
            {
                return ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon(
                    shell.ToNTSLineString() as LinearRing);
            }
            else
            {
                return ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon(
                    shell.ToNTSLineString() as LinearRing,
                    holes.ToMultiLineString().Geometries.Cast<LinearRing>().ToArray());
            }
        }
    }
}

#endif
