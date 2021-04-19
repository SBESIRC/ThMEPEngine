using NFox.Cad;
using System.Linq;
using Dreambuild.AutoCAD;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.OverlayNG;
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
            return Clip(curves.ToMultiLineString(), inverted).ToDbCollection();
        }

        private Geometry Clip(Geometry other, bool inverted = false)
        {
            if (inverted)
            {
                return OverlayNGRobust.Overlay(other, Clipper, SpatialFunction.Difference);
            }
            else
            {
                return OverlayNGRobust.Overlay(Clipper, other, SpatialFunction.Intersection);
            }
        }
    }
}
