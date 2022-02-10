using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.AFAS.Model
{
    public class ThBeamDataParameter
    {
        public bool ReferBeam { get; set; } = true;
        public double WallThickness { get; set; } = 100;
        public double BufferDist { get; set; } = 500;
        
    }
}
