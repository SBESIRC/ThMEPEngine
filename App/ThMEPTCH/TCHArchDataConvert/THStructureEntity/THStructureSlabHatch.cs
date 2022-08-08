using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPTCH.TCHArchDataConvert.THStructureEntity
{
    public class THStructureSlabHatch : THStructureEntity
    {
        public SlabType slabPLType { get; set; }
        public string PatternName { get; set; }
    }
}
