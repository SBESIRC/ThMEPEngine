using System;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Engine
{
    public abstract class ThLoopArrangementEngine : IDisposable
    {
        protected ThLightArrangeParameter ArrangeParameter { get; set; }
        public ThLoopArrangementEngine(
            ThLightArrangeParameter arrangeParameter)
        {
            ArrangeParameter = arrangeParameter;
        }
        public void Dispose()
        {
        }
        public abstract void Arrange(List<ThRegionLightEdge> lightRegions);
    }
}
