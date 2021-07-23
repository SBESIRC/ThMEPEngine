using System.Collections.Generic;
using NetTopologySuite.Features;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.IO.GeoJSON
{
    public class ThBeamFeatureCollection
    {
        public static FeatureCollection Construct(List<ThIfcBeam> beams)
        {
            var features = new FeatureCollection();
            beams.ForEach(o => features.Add(ThBeamFeature.Construct(o)));
            return features;
        }
    }
}
