using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Features;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Features
{
    public class ThBeamFeature
    {
        public Feature Impl { get; private set; }
        public ThBeamFeature(ThIfcLineBeam lineBeam)
        {
            Impl = new Feature();
        }
    }
}
