using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Model
{
    public class ThBeamLink
    {
        public List<ThIfcBuildingElement> Start { get; set; } = new List<ThIfcBuildingElement>();
        public List<ThIfcBuildingElement> End { get; set; } = new List<ThIfcBuildingElement>();
        public List<ThIfcBeam> Beams { get; set; } = new List<ThIfcBeam>();
    }
}
