using NFox.Cad;
using System.Linq;
using Dreambuild.AutoCAD;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThCADCore.NTS
{
    public class ThCADCoreNTSGeometryClipper
    {
        private Polygon Clipper { get; set; }

        public ThCADCoreNTSGeometryClipper(AcPolygon polygon)
        {
            Clipper = polygon.ToNTSPolygon();
        }

        public static DBObjectCollection Clip(AcPolygon polygon, Curve curve, bool inverted = false)
        {
            var clipper = new ThCADCoreNTSGeometryClipper(polygon);
            return clipper.Clip(curve, inverted);
        }

        public static DBObjectCollection Clip(AcPolygon polygon, DBObjectCollection curves, bool inverted = false)
        {
            var clipper = new ThCADCoreNTSGeometryClipper(polygon);
            return clipper.Clip(curves, inverted);
        }

        public DBObjectCollection Clip(Curve curve, bool inverted = false)
        {
            return Clip(curve.ToNTSGeometry(), inverted).ToDbCollection();
        }

        public DBObjectCollection Clip(DBObjectCollection curves, bool inverted = false)
        {
            //return Clip(curves.ToMultiLineString(), inverted).ToDbCollection();
            //转ToMultiLineString，再Intersection，出现问题
            //错误提示“found non-noded intersection between LineString *** adn LineString ***”
            //暂时先一根根裁剪来解决问题
            return inverted ? Difference(curves) : Intersection(curves);
        }

        private DBObjectCollection Intersection(DBObjectCollection curves)
        {
            
            var results = new DBObjectCollection();
            foreach (Curve curve in curves)
            {
                var geo = Clipper.Intersection(curve.ToNTSGeometry());
                geo.ToDbCollection().Cast<Curve>().ForEach(o => results.Add(o));
            }
            return results;
        }

        private DBObjectCollection Difference(DBObjectCollection curves)
        {
            var results = new DBObjectCollection();
            foreach (Curve curve in curves)
            {
                var geo = curve.ToNTSGeometry().Difference(Clipper);
                geo.ToDbCollection().Cast<Curve>().ForEach(o => results.Add(o));
            }
            return results;
        }

        private Geometry Clip(Geometry other, bool inverted = false)
        {
            return inverted ? other.Difference(Clipper) : Clipper.Intersection(other);
        }
    }
}
