using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPTCH.TCHArchDataConvert.THStructureEntity
{
    public class THStructureSlab : THStructureEntity
    {
        public double RelativeBG { get; set; } = 0;
    }

    public enum SlabType
    {
        Slab,
        Hole,
    }
}
