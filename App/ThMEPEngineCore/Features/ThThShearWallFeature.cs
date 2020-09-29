using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using NetTopologySuite.Features;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Features
{
    public class ThThShearWallFeature
    {
        public static Feature Construct(ThIfcWall wall)
        {
            var poly = wall.Outline as Polyline;
            if (poly != null)
            {
                var geometry = poly.ToNTSPolygon();
                return new Feature()
                {
                    Geometry = geometry,
                    BoundingBox = geometry.EnvelopeInternal,
                };
            }
            return null;
        }
    }
}
