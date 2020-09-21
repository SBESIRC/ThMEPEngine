using System;
using NetTopologySuite.IO;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.IO
{
    public class ThBeamGeoJsonWriter : GeoJsonWriter
    {
        public string Write(ThIfcLineBeam lineBeam)
        {
            throw new NotSupportedException();
        }
    }
}
