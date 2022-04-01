using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ThMEPWSS.DrainageADPrivate;

namespace ThMEPWSS.DrainageADPrivate.Model
{
    internal class ThDrainageADNode
    {
        public ThDrainageADNode Child { get; set; }
        public int isHot { get; set; }
        public ThDrainageADCommon.TerminalType Type { get; set; }
        public ThDrainageADNode Pair { get; set; }

    }
}
