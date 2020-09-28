using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Operation.Relate;
using NTSDimension = NetTopologySuite.Geometries.Dimension;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThCADCore.NTS
{
    public class ThCADCoreNTSRelate
    {
        private IntersectionMatrix Matrix { get; set; }

        public ThCADCoreNTSRelate(AcPolygon poly0, AcPolygon poly2)
        {
            Matrix = RelateOp.Relate(poly0.ToNTSPolygon(), poly2.ToNTSPolygon());
        }

        public bool IsCovers
        {
            get
            {
                return Matrix.IsCovers();
            }
        }

        public bool IsOverlaps
        {
            get
            {
                return Matrix.IsOverlaps(NTSDimension.Surface, NTSDimension.Surface);
            }
        }
    }
}
