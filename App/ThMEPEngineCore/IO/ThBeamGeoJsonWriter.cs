using System;
using Newtonsoft.Json;
using NetTopologySuite.IO;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Features;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO
{
    public class ThBeamGeoJsonWriter : GeoJsonWriter
    {
        public void Write(List<ThIfcBeam> beams, JsonWriter writer)
        {
            Write(ThBeamFeatureCollection.Construct(beams), writer);
        }
    }
}
