using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using NetTopologySuite.Features;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Features
{
    public class ThLineFeature
    {
        public static Feature Construct(Line line)
        {
            if (line != null)
            {
                using (var ov = new ThCADCoreNTSFixedPrecision())
                {
                    var geometry = line.Normalize().ToNTSLineString();
                    return new Feature()
                    {
                        Geometry = geometry,
                        //BoundingBox = geometry.EnvelopeInternal,
                    };
                }
            }
            return null;
        }
    }
}
