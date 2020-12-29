using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service
{
    public abstract class ThNumberService
    {
        protected ThLightGraphService LightGraph { get; set; }
        protected ThLightArrangeParameter ArrangeParameter { get; set; }
        protected ThNumberService()
        {           
        }
        protected ThNumberService(
            ThLightGraphService lightGraph,
            ThLightArrangeParameter arrangeParameter)
        {
            LightGraph = lightGraph;
            ArrangeParameter = arrangeParameter;
        }
        protected abstract void Number();
    }
}
