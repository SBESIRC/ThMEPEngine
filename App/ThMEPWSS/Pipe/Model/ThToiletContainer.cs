using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPWSS.Pipe.Model
{
    public class ThToiletContainer
    {
        public ThIfcSpace Toilet { get; set; }
        public ThIfcSpace DrainageWell { get; set; }
        public ThIfcClosestool Closestool { get; set; }
        public List<ThIfcFloorDrain> FloorDrains { get; set; }
    }
}
