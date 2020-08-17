using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Model
{
    public class ThBeamLink
    {
        public List<ThIfcElement> Start { get; set; } = new List<ThIfcElement>();
        public List<ThIfcElement> End { get; set; } = new List<ThIfcElement>();
        public List<ThIfcBeam> Beams { get; set; } = new List<ThIfcBeam>();
    }
}
