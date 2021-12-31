using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service.Arrange
{
    public class ThFirstwayArrangeService : ThArrangeService
    {
        public ThFirstwayArrangeService(
            ThRegionBorder regionBorder, 
            ThLightArrangeParameter arrangeParameter)
            :base(regionBorder, arrangeParameter)
        {
        }
        public override void Arrange()
        {
            throw new NotImplementedException();
        }
    }
}
