using System;
using NetTopologySuite.IO;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Features;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO
{
    public class ThBeamGeoJsonWriter : GeoJsonWriter
    {
        public string Write(List<ThIfcBeam> beams)
        {
            return base.Write(ThBeamFeatureCollection.Construct(beams));
        }
    }
}
