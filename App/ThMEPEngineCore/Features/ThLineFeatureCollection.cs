using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using NetTopologySuite.Features;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;

namespace ThMEPEngineCore.Features
{
    public class ThLineFeatureCollection
    {
        public static FeatureCollection Construct(List<Line> lines)
        {
            var features = new FeatureCollection();
            lines.ForEach(o => features.Add(ThLineFeature.Construct(o)));
            return features;
        }
    }
}
