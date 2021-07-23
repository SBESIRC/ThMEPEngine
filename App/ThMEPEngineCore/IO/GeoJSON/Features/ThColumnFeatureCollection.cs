using ThMEPEngineCore.Model;
using NetTopologySuite.Features;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO.GeoJSON
{
    public class ThColumnFeatureCollection
    {
        public static FeatureCollection Construct(List<ThIfcColumn> beams)
        {
            var features = new FeatureCollection();
            beams.ForEach(o => features.Add(ThColumnFeature.Construct(o)));
            return features;
        }
    }
}
